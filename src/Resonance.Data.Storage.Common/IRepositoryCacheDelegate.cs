using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface IRepositoryCacheDelegate<T>
    {
        Task<T> GetResult(CancellationToken cancelToken);
    }
}
