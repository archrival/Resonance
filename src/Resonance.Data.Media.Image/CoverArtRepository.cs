using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using ImageSharp;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Resonance.Data.Media.Image
{
    public class CoverArtRepository : ICoverArtRepository
    {
        private readonly IMetadataRepositorySettings _metadataRepositorySettings;
        private readonly ITagReaderFactory _tagReaderFactory;

        public CoverArtRepository(IMetadataRepositorySettings metadataRepositorySettings, ITagReaderFactory tagReaderFactory)
        {
            _metadataRepositorySettings = metadataRepositorySettings;
            _tagReaderFactory = tagReaderFactory;
        }

        public async Task<CoverArt> GetCoverArt(Track track, int? size, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var coverArtDirectory = Path.Combine(Path.Combine(_metadataRepositorySettings.ResonancePath, "CoverArt"), size.HasValue ? size.Value.ToString() : "full");

            var coverArtPath = Path.Combine(coverArtDirectory, track.Id.ToString("n"));

            if (File.Exists(coverArtPath) && track.DateFileModified < File.GetLastWriteTimeUtc(coverArtPath))
            {
                var coverArtReturn = new CoverArt()
                {
                    CoverArtData = File.ReadAllBytes(coverArtPath),
                    CoverArtType = CoverArtType.Front,
                    MediaId = track.Id
                };

                coverArtReturn.Size = coverArtReturn.CoverArtData.Length;
                coverArtReturn.MimeType = MimeType.GetMimeType(coverArtReturn.CoverArtData, coverArtPath);

                return coverArtReturn;
            }

            var tagReader = _tagReaderFactory.Create(track.Path);

            var coverArt = tagReader.CoverArt.FirstOrDefault(ca => ca.CoverArtType == CoverArtType.Front || ca.CoverArtType == CoverArtType.Other);

            if (coverArt == null)
            {
                return null;
            }

            if (size.HasValue)
            {
                var bytes = coverArt.CoverArtData;

                using (var memoryStream = new MemoryStream(bytes))
                using (var imageMemoryStream = new MemoryStream())
                using (var image = ImageSharp.Image.Load(memoryStream))
                {
                    var height = (size.Value / image.Width) * image.Height;

                    image.Resize(size.Value, height).SaveAsPng(imageMemoryStream);

                    coverArt.CoverArtData = imageMemoryStream.ToArray();
                }
            }

            if (!Directory.Exists(coverArtDirectory))
            {
                Directory.CreateDirectory(coverArtDirectory);
            }

            File.WriteAllBytes(coverArtPath, coverArt.CoverArtData);

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, coverArtPath);

            return coverArt;
        }
    }
}