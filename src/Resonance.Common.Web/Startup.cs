using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Resonance.Data.Media.Common;
using Resonance.Data.Media.Image;
using Resonance.Data.Media.LastFm;
using Resonance.Data.Media.Tag;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using System;
using System.Linq;
using System.Reflection;

namespace Resonance.Common.Web
{
    public class Startup
    {
        private const string CorsPolicyName = "CorsPolicy";
        private const string LastFmApiKey = "e30fe69883aea7850ec353c1ab42ac47";
        private const string LastFmApiPassword = "2ee73e4fd3d8813a3a90a725878d5a95";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        private IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddFile("resonance-{Date}.log");

            app.UseCors(CorsPolicyName);
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services
                .AddMvc()
                .AddXmlSerializerFormatters()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    opt.SerializerSettings.Formatting = Formatting.None;
                    opt.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    opt.SerializerSettings.StringEscapeHandling = StringEscapeHandling.Default;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyConstants.Administration, policy => policy.RequireRole(Enum.GetName(typeof(Role), Role.Administrator)));
                options.AddPolicy(PolicyConstants.ModifyUserSettings, policy => policy.RequireRole(Enum.GetName(typeof(Role), Role.Administrator), Enum.GetName(typeof(Role), Role.Settings)));
                options.AddPolicy(PolicyConstants.Scrobble, policy => policy.RequireRole(Enum.GetName(typeof(Role), Role.Administrator), Enum.GetName(typeof(Role), Role.Playback)));
                options.AddPolicy(PolicyConstants.Stream, policy => policy.RequireRole(Enum.GetName(typeof(Role), Role.Administrator), Enum.GetName(typeof(Role), Role.Playback)));
            });

            var config = Configuration.GetSection("MetadataRepository");
            services.Configure<MetadataRepositorySettings>(config);
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, builder => builder.AllowAnyOrigin());
            });

            var memoryCacheOptions = new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.FromMinutes(15)
            };

            var metadataRepositorySettings = config.Get<MetadataRepositorySettings>();

            services.AddSingleton<IMetadataRepositorySettings>(metadataRepositorySettings);
            services.AddSingleton<IMetadataRepositoryFactory, MetadataRepositoryFactory>();
            services.AddSingleton(s => s.GetService<IMetadataRepositoryFactory>().CreateMetadataRepository());
            services.AddSingleton<ITagReaderFactory, TagReaderFactory<TagLibTagReader>>();
            services.AddSingleton<ITagReader, TagLibTagReader>();
            services.AddSingleton<IMemoryCache>(new MemoryCache(memoryCacheOptions));
            services.AddSingleton<IMetadataRepositoryCache, MetadataRepositoryCache>();
            services.AddSingleton<ILastFmClient>(new LastFmClient(LastFmApiKey, LastFmApiPassword));
            services.AddSingleton<ISettingsRepository, SettingsRepository>();
            services.AddSingleton<ICoverArtRepository, CoverArtRepository>();
            services.AddSingleton<IMediaLibrary, MediaLibrary>();

            ConfigureControllerAssemblies(services);
        }

        private void ConfigureControllerAssemblies(IServiceCollection services)
        {
            var resonanceAssemblies = Configuration.GetSection("ResonanceAssemblies");

            foreach (var resonanceAssembly in resonanceAssemblies.GetChildren())
            {
                var assemblyNameValue = resonanceAssembly.GetValue<string>("AssemblyName");
                var typeNameValue = resonanceAssembly.GetValue<string>("TypeName");

                var assemblyName = new AssemblyName(assemblyNameValue);
                var assembly = Assembly.Load(assemblyName);

                var type = assembly.DefinedTypes.FirstOrDefault(a => a.Name == typeNameValue);

                if (assembly.CreateInstance(type.FullName) is IResonanceControllerAssembly instance)
                {
                    instance.ConfigureServices(services);
                }
            }
        }
    }
}