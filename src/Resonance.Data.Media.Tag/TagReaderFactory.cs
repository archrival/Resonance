using Resonance.Data.Media.Common;
using System;

namespace Resonance.Data.Media.Tag
{
    public class TagReaderFactory : ITagReaderFactory
    {
        public ITagReader Create<T>(string path) where T : ITagReader, new()
        {
            var tagReader = (T)Activator.CreateInstance(typeof(T));
            tagReader.ReadTag(path);

            return tagReader;
        }
    }
}