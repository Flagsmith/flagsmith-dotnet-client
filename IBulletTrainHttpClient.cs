using System;
using System.Threading.Tasks;

namespace BulletTrain
{
    public interface IBulletTrainHttpClient
    {
        Task<TResponse> GetAsync<TResponse>(Uri uri);
        Task<TResponse> PostAsync<TResponse>(Uri uri, object payload);
    }
}