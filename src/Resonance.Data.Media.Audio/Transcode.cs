using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Resonance.Data.Media.Audio
{
    public class Transcode
    {
        private static readonly string FfmpegBinary = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), @"..\ffmpeg"), "ffmpeg.exe");

        public Stream Convert(string file, string format, int bitrate, CancellationToken cancellationToken)
        {
            return StartProcess($" -i \"{file}\" -map 0:0 -b:a {bitrate}k -v 0 -f {format} -");
       }

        private static Stream StartProcess(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfmpegBinary,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            return process.StandardOutput.BaseStream;
        }
    }
}
