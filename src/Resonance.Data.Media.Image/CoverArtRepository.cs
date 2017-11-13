using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, object> ProcessingFiles = new ConcurrentDictionary<string, object>();

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
            await Task.CompletedTask;

            var fullTrackCoverPath = Path.Combine(_fullCoverArtPath, track.Id.ToString("n"));
            var coverArtPath = size.HasValue ? Path.Combine(_coverArtPath, size.Value.ToString()) : _fullCoverArtPath;
            var trackCoverArtPath = Path.Combine(coverArtPath, track.Id.ToString("n"));

            var coverArt = GetScaledCoverArt(track, trackCoverArtPath);

            if (coverArt != null)
            {
                return coverArt;
            }

            coverArt = GetScaledCoverArt(track, fullTrackCoverPath);

            if (coverArt == null)
            {
                var tagReader = _tagReaderFactory.CreateTagReader(track.Path, false, true);

                coverArt = tagReader.CoverArt.FirstOrDefault(ca => ca.CoverArtType == CoverArtType.Front || ca.CoverArtType == CoverArtType.Other);

                if (coverArt != null)
                {
                    WriteCoverArtToDisk(fullTrackCoverPath, coverArt.CoverArtData);
                }
            }

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
                using (var image = SixLabors.ImageSharp.Image.Load(memoryStream))
                {
                    var resizeOptions = new ResizeOptions { Size = new Size { Height = size.Value, Width = size.Value }, Mode = ResizeMode.Max };

                    var resizedImageData = image.Clone(ctx => ctx.Resize(resizeOptions));

                    // Save to PNG to retain quality at the expense of file size
                    resizedImageData.SaveAsPng(imageMemoryStream);

                    coverArt.CoverArtData = imageMemoryStream.ToArray();

                    WriteCoverArtToDisk(trackCoverArtPath, coverArt.CoverArtData);
                }
            }

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, trackCoverArtPath);

            return coverArt;
        }

        private static CoverArt GetScaledCoverArt(Track track, string trackCoverArtPath)
        {
            var lockObject = ProcessingFiles.GetOrAdd(trackCoverArtPath, new object());

            lock (lockObject)
            {
                if (!(File.Exists(trackCoverArtPath) && track.DateFileModified.ToUniversalTime() < File.GetLastWriteTimeUtc(trackCoverArtPath)))
                {
                    return null;
                }
            }

            ProcessingFiles.TryRemove(trackCoverArtPath, out lockObject);

            // Return the album art on disk if the file exists and is newer than the last modified date of the track

            var coverArtData = ReadCoverArtFromDisk(trackCoverArtPath);

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

        private static byte[] ReadCoverArtFromDisk(string path)
        {
            var lockObject = ProcessingFiles.GetOrAdd(path, new object());

            byte[] result;

            lock (lockObject)
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    result = new byte[stream.Length];

                    stream.Read(result, 0, (int)stream.Length);
                }
            }

            ProcessingFiles.TryRemove(path, out lockObject);

            return result;
        }

        private static void WriteCoverArtToDisk(string path, byte[] bytes)
        {
            var lockObject = ProcessingFiles.GetOrAdd(path, new object());

            lock (lockObject)
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }

                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            ProcessingFiles.TryRemove(path, out lockObject);
        }
    }
}