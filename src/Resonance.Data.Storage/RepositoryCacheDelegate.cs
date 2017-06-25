using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class RepositoryCacheDelegate<T> : IRepositoryCacheDelegate<T>
    {
        public Func<CancellationToken, Task<T>> Method { get; set; }

        public virtual async Task<T> GetResult(CancellationToken cancellationToken)
        {
            return await Method(cancellationToken);
        }
    }
}