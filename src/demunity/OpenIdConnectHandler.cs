
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using demunity.aws.Security;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Security;
using demunity.lib.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace demunity
{
    public class OpenIdConnectHandler
    {
        private readonly ILogWriter<OpenIdConnectHandler> logger;
        private readonly Lazy<Task<string>> authHostName;
        private readonly Lazy<CognitoSettings> cognitoSettings;
        private readonly IUsersService usersService;
        private readonly ISecretsProvider secretsProvider;
        private readonly ISystem system;

        public OpenIdConnectHandler(
            IUsersService usersService,
            ISecretsProvider secretsProvider,
            ISystem system,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            logger = logWriterFactory.CreateLogger<OpenIdConnectHandler>();
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            authHostName = new Lazy<Task<string>>(GetAuthHostName);
            cognitoSettings = new Lazy<CognitoSettings>(() => GetCognitoSettings().SafeGetResult());
        }
        public async Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            try
            {
                var redirectUrl = HttpUtility.UrlEncode($"https://{context.HttpContext.Request.Host}");
                context.ProtocolMessage.Scope = "openid";
                context.ProtocolMessage.ResponseType = "code";

                var hostname = await authHostName.Value;
                context.ProtocolMessage.IssuerAddress = $"https://{hostname}/logout?client_id={ClientId}&logout_uri={redirectUrl}&redirect_uri={redirectUrl}";

                context.Properties.Items.Remove(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Properties.Items.Remove(OpenIdConnectDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(OnRedirectToIdentityProviderForSignOut)}:\n{ex.ToString()}");
                throw;
            }
        }

        private string GetAwsRegion()
        {
            return system.Environment.GetVariable(Constants.EnvironmentVariables.AwsRegion);
        }


        private async Task OnTokenValidated(TokenValidatedContext ctx)
        {
            try
            {
                ClaimsIdentity identity = (ClaimsIdentity)ctx.Principal.Identity;
                var email = identity.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Email, StringComparison.OrdinalIgnoreCase))?.Value;
                if (email == null)
                {
                    logger.LogWarning($"No email in user information. Cannot create user. User information received:\n{JsonConvert.SerializeObject(identity)}");
                    return;
                }

                string name = identity.Claims.FirstOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value;
                if (email == null)
                {
                    logger.LogWarning($"No name in user information. Cannot create user. User information received:\n{JsonConvert.SerializeObject(identity)}");
                    return;
                }

                var existingUser = await usersService.FindUserByEmail(email);
                if (existingUser == UserModel.Null)
                {
                    // email unknown since before, create a user record
                    existingUser = await usersService.CreateUser(Guid.NewGuid(), name, email);
                }

                identity.AddClaim(new Claim(Constants.Security.UserIdClaim, existingUser.Id.Value.ToString()));
                identity.AddClaim(new Claim(Constants.Security.UserNameClaim, existingUser.Name));

                return;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(OnTokenValidated)}:\n{ex.ToString()}");
                throw;
            }
        }

        public string ClientId => cognitoSettings.Value.ClientId;
        public string ClientSecret => cognitoSettings.Value.ClientSecret;

        public string MetadataAddress => $"https://cognito-idp.{GetAwsRegion()}.amazonaws.com/{cognitoSettings.Value.UserPoolId}/.well-known/openid-configuration";

        private async Task<string> GetAuthHostName()
        {
            string authEndpointString = string.Empty;
            string response = string.Empty;
            try
            {

                using (var client = new HttpClient())
                {
                    response = await client.GetStringAsync(MetadataAddress);
                    var deserialized = JObject.Parse(response);
                    authEndpointString = deserialized["authorization_endpoint"].Value<string>();
                    return new Uri(authEndpointString).Host;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(GetAuthHostName)}\n{nameof(authEndpointString)} = '{authEndpointString}'\n\nMetadata:\n{response}\n\nException:\n{ex.ToString()}");
                throw;
            }
        }

        private Task<CognitoSettings> GetCognitoSettings()
        {
            return secretsProvider.GetCognitoSettings();
        }

        public void ConfigureEvents(OpenIdConnectEvents events)
        {
            events.OnRedirectToIdentityProviderForSignOut += OnRedirectToIdentityProviderForSignOut;
            events.OnTokenValidated += OnTokenValidated;
        }
    }
}
