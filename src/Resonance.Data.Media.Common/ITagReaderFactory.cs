namespace Resonance.Data.Media.Common
{
    public interface ITagReaderFactory
    {
        ITagReader CreateTagReader(string path, bool readMediaPropertes = true, bool readCoverArt = true);
    }
}