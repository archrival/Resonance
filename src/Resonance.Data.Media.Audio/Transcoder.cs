using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Resonance.Data.Media.Audio
{
    public class Transcoder : ITranscoder
    {
        private readonly ITranscodeSettings _transcodeSettings;

        public Transcoder(ITranscodeSettings transcodeSettings)
        {
            _transcodeSettings = transcodeSettings;
        }

        public Stream TranscodeAudio(string file, string format, int bitrate, CancellationToken cancellationToken)
        {
            return StartProcess(string.Format(_transcodeSettings.Arguments, file, bitrate, format));
       }

        private Stream StartProcess(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _transcodeSettings.ApplicationPath,
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
