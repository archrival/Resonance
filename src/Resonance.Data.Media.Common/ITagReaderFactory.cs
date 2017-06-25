namespace Resonance.Data.Media.Common
{
    public interface ITagReaderFactory
    {
        ITagReader Create<T>(string path) where T : ITagReader, new();
    }
}