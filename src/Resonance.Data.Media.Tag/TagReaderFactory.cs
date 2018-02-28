using Resonance.Data.Media.Common;

namespace Resonance.Data.Media.Tag
{
    public class TagReaderFactory<TTagReader> : ITagReaderFactory where TTagReader : ITagReader, new()
    {
        public ITagReader CreateTagReader(string path, bool readMediaPropertes = true, bool readCoverArt = true)
        {
            var tagReader = new TTagReader();

            tagReader.ReadTag(path, readMediaPropertes, readCoverArt);

            return tagReader;
        }
    }
}