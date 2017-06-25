using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Resonance.Common
{
    public static class CompressionExtensions
    {
        public static Stream CompressFiles(IEnumerable<string> files, CompressionLevel compressionLevel)
        {
            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    archive.CreateEntryFromFile(file, Path.GetFileName(file), compressionLevel);
                }
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}