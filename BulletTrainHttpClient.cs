using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BulletTrain
{
    public class BulletTrainHttpClient : IBulletTrainHttpClient
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly BulletTrainConfiguration _configuration;

        public BulletTrainHttpClient(BulletTrainConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<TResponse> GetAsync<TResponse>(Uri uri)
        {
            var response = await GetJsonAsync(HttpMethod.Get, uri);

            return JsonSerializer.Deserialize<TResponse>(response);
        }

        public async Task<TResponse> PostAsync<TResponse>(Uri uri, object payload)
        {
            var response = await GetJsonAsync(HttpMethod.Post, uri, JsonSerializer.Serialize(payload));

            return JsonSerializer.Deserialize<TResponse>(response);
        }

        private async Task<string> GetJsonAsync(HttpMethod method, Uri url, string body = null)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Headers =
                {
                    {"X-Environment-Key", _configuration.EnvironmentKey}
                }
            };
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}