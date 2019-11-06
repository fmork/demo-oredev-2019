
using System;
using demunity.aws.Data.DynamoDb;
using demunity.aws.Security;
using demunity.lib;
using demunity.lib.ErrorHandling;
using demunity.lib.Logging;
using demunity.lib.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace demunity
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            DependencyInjectionInitializer.SetupDependencyInjection(services);

            services.AddLogging(configure =>
            {
                configure.AddConfiguration(Configuration.GetSection("Logging"));
            });

            var serviceProvider = services.BuildServiceProvider();

            var secretsProvider = serviceProvider.GetService<ISecretsProvider>();
            var usersService = serviceProvider.GetService<IUsersService>();
            var system = serviceProvider.GetService<ISystem>();
            var logWriterFactory = serviceProvider.GetService<ILogWriterFactory>();

            var openidConnectHandler = new OpenIdConnectHandler(usersService, secretsProvider, system, logWriterFactory);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies
                // is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.ResponseType = "code";
                options.MetadataAddress = openidConnectHandler.MetadataAddress;
                options.ClientId = openidConnectHandler.ClientId;
                options.ClientSecret = openidConnectHandler.ClientSecret;
                options.GetClaimsFromUserInfoEndpoint = true;
                openidConnectHandler.ConfigureEvents(options.Events);
            });

            services.AddDataProtection()
                .Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(svcs =>
                {
                    var loggerFactory = svcs.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                    return new ConfigureOptions<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new DynamoDbDataProtectionXmlRepository(
                            loggerFactory,
                            svcs.GetService<IEnvironment>(),
                            svcs.GetService<IDynamoDbClientFactory>());
                    });
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });

                app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseMiddleware<ErrorLoggingMiddleware>();

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = (context) =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        MaxAge = TimeSpan.FromDays(7),
                        Public = true
                    };
                }
            });

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
