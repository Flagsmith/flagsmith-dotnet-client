using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith.Interfaces
{
    public interface IRestClient
    {
        Task<string> Send(HttpMethod method, string url, string body, CancellationToken token);
    }
}
