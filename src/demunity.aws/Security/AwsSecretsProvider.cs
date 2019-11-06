using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using demunity.lib.Logging;
using Newtonsoft.Json;

namespace demunity.aws.Security
{


    public class CognitoSettings
    {

        public CognitoSettings(
            string userPoolId,
            string clientId,
            string clientSecret)
        {
            UserPoolId = userPoolId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string UserPoolId { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
    }

    public interface ISecretsProvider
    {
        Task<CognitoSettings> GetCognitoSettings();
    }


    public class AwsSecretsProvider : ISecretsProvider
    {
        private readonly ILogWriter<AwsSecretsProvider> logWriter;

        public AwsSecretsProvider(ILogWriterFactory logWriterFactory)
        {
            logWriter = logWriterFactory.CreateLogger<AwsSecretsProvider>();
        }

        public async Task<CognitoSettings> GetCognitoSettings()
        {
            logWriter.LogInformation($"{nameof(GetCognitoSettings)}()");

            string region = "eu-west-1";


            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = "CognitoSettings",

                // VersionStage defaults to AWSCURRENT if unspecified.
                VersionStage = "AWSCURRENT"
            };

            GetSecretValueResponse response = null;
            Dictionary<string, string> values;

            try
            {
                response = await client.GetSecretValueAsync(request);
                values = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.SecretString);
            }
            catch (Exception ex)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                logWriter.LogError(ex, $"Error in {nameof(GetCognitoSettings)}:\n{ex.ToString()}");
                throw;
            }


            string userPoolId;
            string clientId;
            string clientSecret;

            string userPoolIdKey = Environment.GetEnvironmentVariable("COGNITO_USERPOOLID_SECRETNAME");
            if (!values.TryGetValue(userPoolIdKey, out userPoolId))
            {
                throw new Exception($"Expected value ('{userPoolIdKey}') not found.");
            }

            string clientIdKey = Environment.GetEnvironmentVariable("COGNITO_CLIENTID_SECRETNAME");
            if (!values.TryGetValue(clientIdKey, out clientId))
            {
                throw new Exception($"Expected value ('{clientIdKey}') not found.");
            }

            string clientSecretKey = Environment.GetEnvironmentVariable("COGNITO_CLIENTSECRET_SECRETNAME");
            if (!values.TryGetValue(clientSecretKey, out clientSecret))
            {
                throw new Exception($"Expected value ('{clientSecretKey}') not found.");
            }

            return new CognitoSettings(userPoolId, clientId, clientSecret);
        }
    }


}