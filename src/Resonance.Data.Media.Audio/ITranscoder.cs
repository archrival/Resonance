using System.IO;
using System.Threading;

namespace Resonance.Data.Media.Audio
{
    public interface ITranscoder
    {
        Stream TranscodeAudio(string file, string format, int bitrate, CancellationToken cancellationToken);
    }
}
