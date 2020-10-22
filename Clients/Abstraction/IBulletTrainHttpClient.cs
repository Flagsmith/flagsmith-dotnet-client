using System.Threading.Tasks;

namespace BulletTrain
{
    public interface IBulletTrainHttpClient
    {
        Task<TResponse> GetAsync<TResponse>(string endpoint);
        Task<TResponse> PostAsync<TResponse>(string endpoint, object payload);
    }
}