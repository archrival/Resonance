using Microsoft.AspNetCore.Hosting;
using Resonance.Common.Web;

namespace Resonance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = IWebHostBuilderExtensions.GetWebHostBuilder()
                .UseStartup<Startup>()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}