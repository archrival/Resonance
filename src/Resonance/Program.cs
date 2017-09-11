using Microsoft.AspNetCore.Hosting;
using Resonance.Common.Web;
using WebHostBuilderExtensions = Resonance.Common.Web.WebHostBuilderExtensions;

namespace Resonance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHostBuilderExtensions.GetWebHostBuilder()
                .UseStartup<Startup>()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}