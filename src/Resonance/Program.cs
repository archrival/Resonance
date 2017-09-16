using Microsoft.AspNetCore.Hosting;
using Resonance.Common.Web;

namespace Resonance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = ResonanceWebHostBuilderExtensions.GetWebHostBuilder()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
    }
}