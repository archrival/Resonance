using System.IO;
using System.Linq;

namespace Resonance.Common
{
    public static class MimeType
    {
        private static readonly byte[] BMP = { 66, 77 };
        private static readonly byte[] DOC = { 208, 207, 17, 224, 161, 177, 26, 225 };
        private static readonly byte[] EXE_DLL = { 77, 90 };
        private static readonly byte[] GIF = { 71, 73, 70, 56 };
        private static readonly byte[] ICO = { 0, 0, 1, 0 };
        private static readonly byte[] JPG = { 255, 216, 255 };
        private static readonly int maxByteCount = 16;
        private static readonly byte[] MP3 = { 255, 251, 48 };
        private static readonly byte[] OGG = { 79, 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] PDF = { 37, 80, 68, 70, 45, 49, 46 };
        private static readonly byte[] PNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
        private static readonly byte[] RAR = { 82, 97, 114, 33, 26, 7, 0 };
        private static readonly byte[] SWF = { 70, 87, 83 };
        private static readonly byte[] TIFF = { 73, 73, 42, 0 };
        private static readonly byte[] TORRENT = { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 };
        private static readonly byte[] TTF = { 0, 1, 0, 0, 0 };
        private static readonly byte[] WAV_AVI = { 82, 73, 70, 70 };
        private static readonly byte[] WMV_WMA = { 48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108 };
        private static readonly byte[] ZIP_DOCX = { 80, 75, 3, 4 };

        public static string GetDefaultMimeTypeForExtension(string extension)
        {
            var mime = "application/octet-stream";

            switch (extension.ToLowerInvariant())
            {
                case "bmp":
                    mime = "image/bmp";
                    break;

                case "doc":
                    mime = "application/msword";
                    break;

                case "exe":
                case "dll":
                    mime = "application/x-msdownload";
                    break;

                case "gif":
                    mime = "image/gif";
                    break;

                case "ico":
                    mime = "image/x-icon";
                    break;

                case "jpg":
                    mime = "image/jpeg";
                    break;

                case "mp3":
                case "mp2":
                    mime = "audio/mpeg";
                    break;

                case "ogg":
                    mime = "video/ogg";
                    break;

                case "ogx":
                    mime = "application/ogg";
                    break;

                case "oga":
                    mime = "audio/ogg";
                    break;

                case "pdf":
                    mime = "application/pdf";
                    break;

                case "png":
                    mime = "image/png";
                    break;

                case "rar":
                    mime = "application/x-rar-compressed";
                    break;

                case "swf":
                    mime = "application/x-shockwave-flash";
                    break;

                case "tif":
                case "tiff":
                    mime = "image/tiff";
                    break;

                case "torrent":
                    mime = "application/x-bittorrent";
                    break;

                case "ttf":
                    mime = "application/x-font-ttf";
                    break;

                case "wav":
                    mime = "audio/x-wav";
                    break;

                case "avi":
                    mime = "video/x-msvideo";
                    break;

                case "wma":
                    mime = "audio/x-ms-wma";
                    break;

                case "wmv":
                    mime = "video/x-ms-wmv";
                    break;

                case "zip":
                    mime = "application/x-zip-compressed";
                    break;

                case "docx":
                    mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
            }

            return mime;
        }

        public static string GetMimeType(Stream stream, string extension)
        {
            var bytes = new byte[maxByteCount];

            stream.Read(bytes, 0, maxByteCount);

            return GetMimeType(bytes, $"test.{extension}");
        }

        public static string GetMimeType(string fileName)
        {
            var bytes = new byte[maxByteCount];

            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Read(bytes, 0, maxByteCount);
            }

            return GetMimeType(bytes, fileName);
        }

        public static string GetMimeType(byte[] file, string fileName)
        {
            var mime = "application/octet-stream";

            var extension = string.Empty;

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string pathExtension = Path.GetExtension(fileName);
                extension = pathExtension.ToUpper();
            }

            if (file.Take(3).SequenceEqual(MP3))
            {
                mime = "audio/mpeg";
            }
            else if (file.Take(14).SequenceEqual(OGG))
            {
                if (extension == ".OGX")
                {
                    mime = "application/ogg";
                }
                else if (extension == ".OGA")
                {
                    mime = "audio/ogg";
                }
                else
                {
                    mime = "video/ogg";
                }
            }
            else if (file.Take(16).SequenceEqual(PNG))
            {
                mime = "image/png";
            }
            else if (file.Take(3).SequenceEqual(JPG))
            {
                mime = "image/jpeg";
            }
            else if (file.Take(2).SequenceEqual(BMP))
            {
                mime = "image/bmp";
            }
            else if (file.Take(8).SequenceEqual(DOC))
            {
                mime = "application/msword";
            }
            else if (file.Take(2).SequenceEqual(EXE_DLL))
            {
                mime = "application/x-msdownload"; //both use same mime type
            }
            else if (file.Take(4).SequenceEqual(GIF))
            {
                mime = "image/gif";
            }
            else if (file.Take(4).SequenceEqual(ICO))
            {
                mime = "image/x-icon";
            }
            else if (file.Take(7).SequenceEqual(PDF))
            {
                mime = "application/pdf";
            }
            else if (file.Take(7).SequenceEqual(RAR))
            {
                mime = "application/x-rar-compressed";
            }
            else if (file.Take(3).SequenceEqual(SWF))
            {
                mime = "application/x-shockwave-flash";
            }
            else if (file.Take(4).SequenceEqual(TIFF))
            {
                mime = "image/tiff";
            }
            else if (file.Take(11).SequenceEqual(TORRENT))
            {
                mime = "application/x-bittorrent";
            }
            else if (file.Take(5).SequenceEqual(TTF))
            {
                mime = "application/x-font-ttf";
            }
            else if (file.Take(4).SequenceEqual(WAV_AVI))
            {
                mime = extension == ".AVI" ? "video/x-msvideo" : "audio/x-wav";
            }
            else if (file.Take(16).SequenceEqual(WMV_WMA))
            {
                mime = extension == ".WMA" ? "audio/x-ms-wma" : "video/x-ms-wmv";
            }
            else if (file.Take(4).SequenceEqual(ZIP_DOCX))
            {
                mime = extension == ".DOCX" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/x-zip-compressed";
            }

            return mime;
        }
    }
}