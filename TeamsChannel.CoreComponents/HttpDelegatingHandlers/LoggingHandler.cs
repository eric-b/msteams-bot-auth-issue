using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsChannel.CoreComponents.HttpDelegatingHandlers
{
    public sealed class HttpContentLoggingHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public HttpContentLoggingHandler(ILoggerFactory loggerFactory)
        {
            // We need to inject ILoggerFactory instead of ILogger<T> because we are not in ASP.NET Core host.
            _logger = loggerFactory.CreateLogger<HttpContentLoggingHandler>();
        }

        static string GetHeadersAsString(HttpRequestMessage request)
        {
            return string.Join(Environment.NewLine, request.Headers.Select(t => $"{t.Key}: {string.Join(", ", t.Value)}"));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
                string requestContent = await request.Content.ReadAsStringAsync();
                _logger.LogInformation($"{request.Method} {request.RequestUri} ...{Environment.NewLine}Headers:{Environment.NewLine}{GetHeadersAsString(request)}{Environment.NewLine}{request.Content.Headers.ContentType}{Environment.NewLine}{requestContent}");
            }
            else
            {
                _logger.LogInformation($"{request.Method} {request.RequestUri} ...{Environment.NewLine}Headers:{Environment.NewLine}{GetHeadersAsString(request)}");
            }

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while sending request {request.Method} {request.RequestUri}");
                throw;
            }

            if (response.Content != null)
            {
                await response.Content.LoadIntoBufferAsync();
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"{request.Method} {request.RequestUri} : {response.StatusCode}{Environment.NewLine}{response.Content.Headers.ContentType}{Environment.NewLine}{responseContent}");
            }
            else
            {
                _logger.LogInformation($"{request.Method} {request.RequestUri} : {response.StatusCode}");
            }

            return response;
        }
    }
}
