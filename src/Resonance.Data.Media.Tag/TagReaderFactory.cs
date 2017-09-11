using Resonance.Data.Media.Common;
using System;

namespace Resonance.Data.Media.Tag
{
    public class TagReaderFactory<TTagReader> : ITagReaderFactory where TTagReader : ITagReader, new()
    {
        public ITagReader CreateTagReader(string path)
        {
            var tagReader = (TTagReader)Activator.CreateInstance(typeof(TTagReader));
            tagReader.ReadTag(path);

            return tagReader;
        }
    }
}