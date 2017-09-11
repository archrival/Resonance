using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Resonance.Common.Web;
using System.Diagnostics;
using System.Linq;

namespace Resonance.Windows
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var host = ResonanceWebHostBuilderExtensions.GetWebHostBuilder()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            if (isService)
            {
                host.RunAsService();
            }
            else
            {
                host.Run();
            }
        }
    }
}