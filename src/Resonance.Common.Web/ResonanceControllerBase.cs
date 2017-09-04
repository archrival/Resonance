using Microsoft.AspNetCore.Mvc;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;

namespace Resonance.Common.Web
{
    public class ResonanceControllerBase : Controller
    {
        protected readonly IMediaLibrary MediaLibrary;
        protected readonly IMetadataRepository MetadataRepository;
        protected readonly ISettingsRepository SettingsRepository;

        public ResonanceControllerBase(IMediaLibrary mediaLibrary, IMetadataRepository metadataRepository, ISettingsRepository settingsRepository)
        {
            MediaLibrary = mediaLibrary;
            MetadataRepository = metadataRepository;
            SettingsRepository = settingsRepository;
        }
    }
}