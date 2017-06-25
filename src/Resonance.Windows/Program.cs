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

            bool isService = true;

            if (Debugger.IsAttached || args.Contains("--console"))
            {
                isService = false;
            }

            var host = IWebHostBuilderExtensions.GetWebHostBuilder()
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