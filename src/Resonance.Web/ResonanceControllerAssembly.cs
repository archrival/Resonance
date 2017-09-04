using Microsoft.Extensions.DependencyInjection;
using Resonance.Common.Web;

namespace Resonance.Web
{
    public class ResonanceControllerAssembly : IResonanceControllerAssembly
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddApplicationPart(typeof(ResonanceControllerAssembly).Assembly);
        }
    }
}