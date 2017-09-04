using Microsoft.Extensions.DependencyInjection;

namespace Resonance.Common.Web
{
    public interface IResonanceControllerAssembly
    {
        void ConfigureServices(IServiceCollection services);
    }
}