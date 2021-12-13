using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;

namespace TeamsChannel.CoreComponents.Connector
{
    public sealed class TeamsConnectorClientProvider
    {
        static readonly string LastTokenCacheKey = $"{nameof(TeamsConnectorClientProvider)}.lastToken";

        public static class HttpClientNames
        {
            /// <summary>
            /// Having dedicated HttpClient for Login and for Service allows 
            /// to setup a different proxy for token requests.
            /// This is typically used for feature "Tenant restrictions":
            /// https://docs.microsoft.com/en-us/azure/active-directory/manage-apps/tenant-restrictions
            /// (outside scope of this application).
            /// </summary>
            public const string MicrosoftLogin = "MicrosoftLoginClient";

            /// <summary>
            /// Primary http client for Teams service except for token requests.
            /// </summary>
            public const string TeamsService = "TeamsConnectorClient";
        }

        private readonly TeamsConnectorClientProviderOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly bool _isProductionEnv;

        public Uri DefaultServiceUrl { get; }

        public TeamsConnectorClientProvider(TeamsConnectorClientProviderOptions options,
                                            IMemoryCache memoryCache,
                                            IHttpClientFactory httpClientFactory,
                                            ILoggerFactory loggerFactory,
                                            IHostEnvironment hostEnvironment)
        {
            ValidateOptions(options);
            if (hostEnvironment is null)
                throw new ArgumentNullException(nameof(hostEnvironment));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _isProductionEnv = hostEnvironment.IsProduction();

            DefaultServiceUrl = new Uri(options.DefaultServiceUrl!, UriKind.Absolute);
        }

        private static void ValidateOptions(TeamsConnectorClientProviderOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(options.DefaultServiceUrl))
                throw new ArgumentException($"{nameof(options.DefaultServiceUrl)} is required.", nameof(options));
            if (string.IsNullOrEmpty(options.ChannelAuthTenant))
                throw new ArgumentException($"{nameof(options.ChannelAuthTenant)} is required.", nameof(options));
            if (string.IsNullOrEmpty(options.MicrosoftAppId))
                throw new ArgumentException($"{nameof(options.MicrosoftAppId)} is required.", nameof(options));
            if (string.IsNullOrEmpty(options.MicrosoftAppPassword))
                throw new ArgumentException($"{nameof(options.MicrosoftAppPassword)} is required.", nameof(options));
        }

        public IConnectorClient CreateConnector(Uri serviceUrl)
        {
            if (serviceUrl is null)
                throw new ArgumentNullException(nameof(serviceUrl));

            if (!_isProductionEnv && serviceUrl.Scheme == "http" && serviceUrl.Host == "localhost")
            {
                // Support for Bot Framework Emulator
                HttpClient serviceClient = _httpClientFactory.CreateClient(HttpClientNames.TeamsService);
                return new ConnectorClient(serviceUrl, MicrosoftAppCredentials.Empty, serviceClient);
            }
            else
            {
                // Expected code path:
                HttpClient serviceClient = _httpClientFactory.CreateClient(HttpClientNames.TeamsService);
                HttpClient loginClient = _httpClientFactory.CreateClient(HttpClientNames.MicrosoftLogin);
                MicrosoftAppCredentials appCredentials = CreateMicrosoftAppCredentials(loginClient, _loggerFactory.CreateLogger<TeamsConnectorClientWrapper>());
                var connectorClient = new ConnectorClient(serviceUrl, appCredentials, serviceClient);
                return new TeamsConnectorClientWrapper(connectorClient, serviceClient, loginClient);
            }
        }

        private MicrosoftAppCredentials CreateMicrosoftAppCredentials(HttpClient loginClient, ILogger logger)
        {
            return new MicrosoftAppCredentials(_options.MicrosoftAppId,
                                               _options.MicrosoftAppPassword,
                                               _options.ChannelAuthTenant,
                                               loginClient,
                                               logger);
                                               //oAuthScope: AuthenticationConstants.ToChannelFromBotOAuthScope); // TODO: not sure for scope to use...
        }

        /// <summary>
        /// Used by health checks
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TryGetAccessTokenOrThrow()
        {
            if (_memoryCache.TryGetValue(LastTokenCacheKey, out _))
            {
                // to many health requests - if success, keep last state until token expires.
                return true;
            }

            using (HttpClient loginClient = _httpClientFactory.CreateClient(HttpClientNames.MicrosoftLogin))
            {
                MicrosoftAppCredentials appCredentials = CreateMicrosoftAppCredentials(loginClient, _loggerFactory.CreateLogger<TeamsConnectorClientProvider>());
                string accessToken = await appCredentials.GetTokenAsync().ConfigureAwait(continueOnCapturedContext: false);
                if (accessToken != null)
                {
                    var jwtToken = new JwtSecurityToken(accessToken);
                    _memoryCache.Set(LastTokenCacheKey, accessToken, jwtToken.ValidTo);
                }
                return accessToken != null;
            }
        }
    }
}
