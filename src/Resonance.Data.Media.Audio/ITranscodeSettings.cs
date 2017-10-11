namespace Resonance.Data.Media.Audio
{
    public interface ITranscodeSettings
    {
        string ApplicationPath { get; set; }
        string Arguments { get; set; }
    }
}
