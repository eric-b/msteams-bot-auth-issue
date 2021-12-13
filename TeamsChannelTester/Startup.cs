using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Serilog;
using TeamsChannel.CoreComponents.Connector;
using TeamsChannel.CoreComponents.HttpDelegatingHandlers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Net;
using System.Net.Http;

namespace TeamsChannelTester
{
    public sealed class Startup
    {
        IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            const string oasDocumentTitle = "Teams Channel Tester";

            AddHttpClients(services);

            services.AddSingleton<Components.TeamsActivityReceiver>();
            services.AddSingleton<TeamsConnectorClientProvider>();
            services.AddSingleton(s => s.GetRequiredService<IOptions<TeamsConnectorClientProviderOptions>>().Value);
            services.Configure<TeamsConnectorClientProviderOptions>(Configuration.GetSection("Teams"));

            services.AddMemoryCache();

            // AddSwaggerGenNewtonsoftSupport: explicit opt-in - needs to be placed after AddSwaggerGen()
            services.AddSwaggerGen();
            services.AddSwaggerGenNewtonsoftSupport();

            services.Configure<SwaggerGenOptions>(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = oasDocumentTitle, Version = "v1" });
            });

            services.Configure<SwaggerUIOptions>(options =>
            {
                options.SwaggerEndpoint("v1/swagger.json", oasDocumentTitle);
            });

            services
                .AddHttpContextAccessor()
                .AddControllers()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); });

            services.Configure<ExceptionHandlerOptions>(options => options.ExceptionHandlingPath = Controllers.ErrorController.ErrorLocalDevRouteTemplate);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseSerilogRequestLogging();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void AddHttpClients(IServiceCollection services)
        {
            services.AddHttpClient();

            IConfigurationSection teamsConfig = Configuration.GetSection("Teams");
            string msLoginProxyUrlStr = teamsConfig["LoginProxyUrl"];
            string teamsServiceProxyUrlStr = teamsConfig["ServiceProxyUrl"];

            services.AddTransient<HttpContentLoggingHandler>();

            IHttpClientBuilder MsLoginClientBuilder = services
                .AddHttpClient(TeamsConnectorClientProvider.HttpClientNames.MicrosoftLogin)
                .AddHttpMessageHandler<HttpContentLoggingHandler>();

            IHttpClientBuilder teamsServiceClientBuilder = services
                .AddHttpClient(TeamsConnectorClientProvider.HttpClientNames.TeamsService)
                .AddHttpMessageHandler<HttpContentLoggingHandler>();

            if (!string.IsNullOrEmpty(msLoginProxyUrlStr))
            {
                var proxyUrl = new Uri(msLoginProxyUrlStr, UriKind.Absolute);
                bool bypassOnLocal = !proxyUrl.IsLoopback; // if proxy is local, we assume we also want to debug local requests

                MsLoginClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyUrl, bypassOnLocal, BypassList: null, Credentials: CredentialCache.DefaultNetworkCredentials)
                });
            }

            if (!string.IsNullOrEmpty(teamsServiceProxyUrlStr))
            {
                var proxyUrl = new Uri(teamsServiceProxyUrlStr, UriKind.Absolute);
                bool bypassOnLocal = !proxyUrl.IsLoopback; // if proxy is local, we assume we also want to debug local requests

                teamsServiceClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyUrl, bypassOnLocal, BypassList: null, Credentials: CredentialCache.DefaultNetworkCredentials)
                });
            }
        }
    }
}
