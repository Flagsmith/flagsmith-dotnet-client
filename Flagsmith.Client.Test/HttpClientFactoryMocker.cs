using System.Net.Http;

namespace ClientTest
{
    public class HttpClientFactoryMocker : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public HttpClient CreateClient(string name)
        {
            return _client;
        }

        public HttpClientFactoryMocker(HttpClient client)
        {
            _client = client;
        }
    }
}
