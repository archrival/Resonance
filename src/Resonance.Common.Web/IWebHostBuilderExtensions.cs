using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Resonance.Common.Web
{
    public class IWebHostBuilderExtensions
    {
        public static IWebHostBuilder GetWebHostBuilder()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var noDelay = true;
            var threadCount = 32;
            var allowSynchronousIO = false;
            var addServerHeader = true;
            var httpEnabled = false;
            var httpAddress = IPAddress.Any;
            var httpPort = 5000;
            var httpsEnabled = false;
            var httpsAddress = IPAddress.Any;
            var httpsPort = 5001;
            var httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions();

            var appSettings = configuration.GetSection("AppSettings");

            if (appSettings != null)
            {
                var kestrelSettings = appSettings.GetSection("KestrelSettings");

                if (kestrelSettings != null)
                {
                    threadCount = kestrelSettings.GetValue("ThreadCount", Environment.ProcessorCount);
                    noDelay = kestrelSettings.GetValue("NoDelay", true);
                    addServerHeader = kestrelSettings.GetValue("AddServerHeader", true);
                }

                var httpSettings = appSettings.GetSection("HttpSettings");

                if (httpSettings != null)
                {
                    var unsecuredSettings = httpSettings.GetSection("Unsecured");

                    if (unsecuredSettings != null)
                    {
                        httpEnabled = unsecuredSettings.GetValue("Enabled", false);

                        if (httpEnabled)
                        {
                            var listenerSettings = unsecuredSettings.GetSection("Listener");

                            if (listenerSettings != null)
                            {
                                var address = listenerSettings.GetValue("Address", "*");

                                if (address != "*")
                                {
                                    IPAddress.TryParse(address, out httpAddress);
                                }

                                httpPort = listenerSettings.GetValue("Port", 5000);
                            }
                        }
                    }

                    var securedSettings = httpSettings.GetSection("Secured");

                    if (securedSettings != null)
                    {
                        httpsEnabled = securedSettings.GetValue("Enabled", false);

                        if (httpsEnabled)
                        {
                            var listenerSettings = securedSettings.GetSection("Listener");

                            if (listenerSettings != null)
                            {
                                var address = listenerSettings.GetValue("Address", "*");

                                if (address != "*")
                                {
                                    IPAddress.TryParse(address, out httpsAddress);
                                }

                                httpsPort = listenerSettings.GetValue("Port", 5001);
                            }

                            var certificateFile = securedSettings.GetValue("CertificateFile", string.Empty);
                            var certificatePassword = securedSettings.GetValue("CertificatePassword", string.Empty);

                            httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions
                            {
                                ClientCertificateMode = ClientCertificateMode.NoCertificate,
                                ServerCertificate = new X509Certificate2(certificateFile, certificatePassword),
                                SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12
                            };
                        }
                    }
                }
            }

            var webHostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.AllowSynchronousIO = allowSynchronousIO;
                    options.ApplicationSchedulingMode = SchedulingMode.Inline;
                    options.AddServerHeader = addServerHeader;

                    if (httpEnabled)
                    {
                        options.Listen(httpAddress, httpPort);
                    }

                    if (httpsEnabled)
                    {
                        options.Listen(httpsAddress, httpsPort, listenOptions =>
                        {
                            listenOptions.NoDelay = noDelay;
                            listenOptions.UseHttps(httpsConnectionAdapterOptions);
                        });
                    }
                })
                .UseLibuv(options =>
                {
                    options.ThreadCount = threadCount;
                })
                .UseContentRoot(Directory.GetCurrentDirectory());

            return webHostBuilder;
        }
    }
}