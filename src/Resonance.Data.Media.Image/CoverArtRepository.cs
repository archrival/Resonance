using ImageSharp;
using ImageSharp.Processing;
using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using SixLabors.Primitives;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Media.Image
{
    public class CoverArtRepository : ICoverArtRepository
    {
        private const string Full = "full";
        private static object _lockObject;
        private readonly string _coverArtPath;
        private readonly IMetadataRepositorySettings _metadataRepositorySettings;
        private readonly ITagReaderFactory _tagReaderFactory;

        public CoverArtRepository(IMetadataRepositorySettings metadataRepositorySettings, ITagReaderFactory tagReaderFactory)
        {
            _metadataRepositorySettings = metadataRepositorySettings;
            _tagReaderFactory = tagReaderFactory;
            _coverArtPath = Path.Combine(_metadataRepositorySettings.ResonancePath, "CoverArt");
        }

        public async Task<CoverArt> GetCoverArt(Track track, int? size, CancellationToken cancellationToken)
        {
            var coverArtDirectory = Path.Combine(_coverArtPath, size.HasValue ? size.Value.ToString() : Full);
            var trackCoverArtPath = Path.Combine(coverArtDirectory, track.Id.ToString("n"));

            // Return the album art on disk if the file exists and is newer than the last modified date of the track
            if (File.Exists(trackCoverArtPath) && track.DateFileModified.ToUniversalTime() < File.GetLastWriteTimeUtc(trackCoverArtPath))
            {
                var coverArtData = await ReadCoverArtFromDiskAsync(trackCoverArtPath, cancellationToken);

                var coverArtReturn = new CoverArt()
                {
                    CoverArtData = coverArtData,
                    CoverArtType = CoverArtType.Front,
                    MediaId = track.Id
                };

                coverArtReturn.Size = coverArtData.Length;
                coverArtReturn.MimeType = MimeType.GetMimeType(coverArtData, trackCoverArtPath);

                return coverArtReturn;
            }

            var tagReader = _tagReaderFactory.CreateTagReader(track.Path);

            var coverArt = tagReader.CoverArt.FirstOrDefault(ca => ca.CoverArtType == CoverArtType.Front || ca.CoverArtType == CoverArtType.Other);

            if (coverArt == null)
            {
                return null;
            }

            // Resize the image if requested
            if (size.HasValue)
            {
                var bytes = coverArt.CoverArtData;

                using (var memoryStream = new MemoryStream(bytes))
                using (var imageMemoryStream = new MemoryStream())
                using (var image = ImageSharp.Image.Load(memoryStream))
                {
                    var resizeOptions = new ResizeOptions { Size = new Size { Height = size.Value, Width = size.Value }, Mode = ResizeMode.Max };

                    var resizedImageData = image.Resize(resizeOptions);

                    // Save to PNG to retain quality at the expense of file size
                    resizedImageData.SaveAsPng(imageMemoryStream);

                    coverArt.CoverArtData = imageMemoryStream.ToArray();
                }
            }

            if (!Directory.Exists(coverArtDirectory))
            {
                Directory.CreateDirectory(coverArtDirectory);
            }

            File.WriteAllBytes(trackCoverArtPath, coverArt.CoverArtData);

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, trackCoverArtPath);

            return coverArt;
        }

        private static async Task<byte[]> ReadCoverArtFromDiskAsync(string trackCoverArtPath, CancellationToken cancellationToken)
        {
            byte[] result;

            using (var stream = File.Open(trackCoverArtPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = new byte[stream.Length];

                await stream.ReadAsync(result, 0, (int)stream.Length, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}