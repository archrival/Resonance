using ImageSharp;
using ImageSharp.Processing;
using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using SixLabors.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Media.Image
{
    public class CoverArtRepository : ICoverArtRepository
    {
        private const string CoverArt = "CoverArt";
        private const string Full = "full";

        private static readonly HashSet<string> Processing = new HashSet<string>();
        private readonly string _coverArtPath;
        private readonly string _fullCoverArtPath;
        private readonly ITagReaderFactory _tagReaderFactory;

        public CoverArtRepository(IMetadataRepositorySettings metadataRepositorySettings, ITagReaderFactory tagReaderFactory)
        {
            _tagReaderFactory = tagReaderFactory;
            _coverArtPath = Path.Combine(metadataRepositorySettings.ResonancePath, CoverArt);
            _fullCoverArtPath = Path.Combine(_coverArtPath, Full);
        }

        public async Task<CoverArt> GetCoverArtAsync(Track track, int? size, CancellationToken cancellationToken)
        {
            var coverArtPath = size.HasValue ? Path.Combine(_coverArtPath, size.Value.ToString()) : _fullCoverArtPath;
            var trackCoverArtPath = Path.Combine(coverArtPath, track.Id.ToString("n"));

            // Return the album art on disk if the file exists and is newer than the last modified date of the track
            if (File.Exists(trackCoverArtPath) && track.DateFileModified.ToUniversalTime() < File.GetLastWriteTimeUtc(trackCoverArtPath))
            {
                var coverArtData = await ReadCoverArtFromDiskAsync(trackCoverArtPath, cancellationToken);

                var coverArtReturn = new CoverArt
                {
                    CoverArtData = coverArtData,
                    CoverArtType = CoverArtType.Front,
                    MediaId = track.Id,
                    Size = coverArtData.Length,
                    MimeType = MimeType.GetMimeType(coverArtData, trackCoverArtPath)
                };

                return coverArtReturn;
            }

            lock (Processing)
            {
                if (Processing.Contains(track.Path))
                {
                    return GetCoverArtAsync(track, size, cancellationToken).GetAwaiter().GetResult();
                }

                Processing.Add(track.Path);
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

            if (!Directory.Exists(coverArtPath))
            {
                Directory.CreateDirectory(coverArtPath);
            }

            await WriteCoverArtToDiskAsync(trackCoverArtPath, coverArt.CoverArtData, cancellationToken);

            lock (Processing)
            {
                Processing.Remove(track.Path);
            }

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, trackCoverArtPath);

            return coverArt;
        }

        private static async Task<byte[]> ReadCoverArtFromDiskAsync(string path, CancellationToken cancellationToken)
        {
            byte[] result;

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = new byte[stream.Length];

                await stream.ReadAsync(result, 0, (int)stream.Length, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private static async Task WriteCoverArtToDiskAsync(string path, byte[] bytes, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096, true))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}