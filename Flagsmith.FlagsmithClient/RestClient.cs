using Flagsmith.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class RestClient : IRestClient
    {
        private readonly ILogger<RestClient> _logger;
        private readonly IFlagsmithClientConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public RestClient(ILogger<RestClient> logger, IFlagsmithClientConfig config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> Send(HttpMethod method, string url, string body, CancellationToken token)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(_config.ApiUrl + _config.EnvironmentKey);

                var policy = HttpPolicies.GetRetryPolicyAwaitable(_config.Retries);
                using var response = await policy.ExecuteAsync(async () =>
                {
                    using var request = new HttpRequestMessage(method, url);
                    if (!string.IsNullOrEmpty(body))
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request, token);
                    return response.EnsureSuccessStatusCode();
                });

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                _logger?.LogError(e, "\nHTTP Request Exception Caught!");
                throw new FlagsmithAPIError("Unable to get valid response from Flagsmith API", e);
            }
            catch (TaskCanceledException e)
            {
                _logger?.LogError(e, "\nHTTP Request Exception Caught!");
                throw new FlagsmithAPIError("Request cancelled: Api server takes too long to respond", e);
            }
        }
    }
}
