using Microsoft.AspNetCore.Hosting;
using Resonance.Common.Web;
using System;
using System.Net;

namespace Resonance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 12;

            var host = ResonanceWebHostBuilderExtensions.GetWebHostBuilder()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
    }
}