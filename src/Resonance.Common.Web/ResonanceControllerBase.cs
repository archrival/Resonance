using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Resonance.Data.Media.LastFm;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Common.Web
{
    public class ResonanceControllerBase : Controller
    {
        protected static IOptions<MetadataRepositorySettings> Settings;
        private const string LastFmApiKey = "e30fe69883aea7850ec353c1ab42ac47";
        private const string LastFmApiPassword = "2ee73e4fd3d8813a3a90a725878d5a95";

        private static readonly Lazy<IMediaLibrary> MediaLibraryLazy = new Lazy<IMediaLibrary>(() => new MediaLibrary(MetadataRepositoryLazy.Value, new LastFmClient(LastFmApiKey, LastFmApiPassword), Settings.Value));

        private static readonly Lazy<IMetadataRepository> MetadataRepositoryLazy = new Lazy<IMetadataRepository>(() =>
        {
            var metadataRepositoryFactory = new MetadataRepositoryFactory();
            return metadataRepositoryFactory.Create(Settings.Value);
        });

        private static readonly Lazy<ISettingsRepository> SettingsRepositoryLazy = new Lazy<ISettingsRepository>(() => new SettingsRepository(MetadataRepositoryLazy.Value));

        public ResonanceControllerBase(IOptions<MetadataRepositorySettings> settings)
        {
            Settings = settings;
        }

        protected IMediaLibrary MediaLibrary => MediaLibraryLazy.Value;

        protected IMetadataRepository MetadataRepository => MetadataRepositoryLazy.Value;

        protected ISettingsRepository SettingsRepository => SettingsRepositoryLazy.Value;
    }
}