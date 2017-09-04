using Microsoft.Extensions.DependencyInjection;
using Resonance.Common.Web;

namespace Resonance.SubsonicCompat
{
    public class SubsonicControllerAssembly : IResonanceControllerAssembly
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddApplicationPart(typeof(SubsonicControllerAssembly).Assembly);

            services.AddSingleton<SubsonicAsyncAuthorizationFilter>();
            services.AddSingleton<SubsonicAsyncResultFilter>();
        }
    }
}