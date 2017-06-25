using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
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

			var threadCount = 32;
			var noDelay = true;
			var addServerHeader = true;
			var httpEnabled = false;
			var httpsEnabled = false;
			var httpsConnectionFilterOptions = new HttpsConnectionFilterOptions();

			var urls = new List<string>();

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
							GetListenerUrls(urls, unsecuredSettings, "http", 5000);
						}
					}

					var securedSettings = httpSettings.GetSection("Secured");

					if (securedSettings != null)
					{
						httpsEnabled = securedSettings.GetValue("Enabled", false);

						if (httpsEnabled)
						{
							GetListenerUrls(urls, securedSettings, "https", 5001);

							var certificateFile = securedSettings.GetValue("CertificateFile", string.Empty);
							var certificatePassword = securedSettings.GetValue("CertificatePassword", string.Empty);

							if (!string.IsNullOrWhiteSpace(certificateFile) && !string.IsNullOrWhiteSpace(certificatePassword))
							{
								httpsConnectionFilterOptions = new HttpsConnectionFilterOptions
								{
									ClientCertificateMode = ClientCertificateMode.NoCertificate,
									ServerCertificate = new X509Certificate2(certificateFile, certificatePassword),
									SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12
								};
							}
						}
					}
				}
			}

			var webHostBuilder = new WebHostBuilder()
				.UseUrls(urls.ToArray())
				.UseKestrel(options =>
				{
					options.ThreadCount = threadCount;
					options.NoDelay = noDelay;
					options.AddServerHeader = addServerHeader;

					if (httpsEnabled)
					{
						options.UseHttps(httpsConnectionFilterOptions);
					}
				})
				.UseContentRoot(Directory.GetCurrentDirectory());

			return webHostBuilder;
		}

		private static void GetListenerUrls(List<string> urls, IConfigurationSection settings, string scheme, int defaultPort)
		{
			var listenerSettings = settings.GetSection("Listener");

			if (listenerSettings != null)
			{
				var address = listenerSettings.GetValue("Address", "*");
				var port = listenerSettings.GetValue("Port", defaultPort);

				UriBuilder uriBuilder = new UriBuilder()
				{
					Scheme = scheme,
					Host = address,
					Port = port
				};

				urls.Add(uriBuilder.ToString());
			}
		}
	}
}