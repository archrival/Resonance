namespace Resonance.Data.Media.Common
{
    public interface ITagReaderFactory
    {
        ITagReader Create(string path);
    }
}