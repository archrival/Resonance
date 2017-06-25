using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface IRepositoryCacheItem<T>
    {
        Task<T> GetResultAsync(CancellationToken cancelToken, bool useCache = true);

        void Invalidate();

        void Remove();

        void SetTimeout(TimeSpan timeout);
    }
}