using System.Collections.Generic;

namespace Resonance.Common
{
    public static class CollectionExtensions
    {
        public static TCollectionType AddValuesToCollection<T, TCollectionType>(TCollectionType collection, IEnumerable<T> values) where TCollectionType : ICollection<T>, new()
        {
            if (values == null)
            {
                return collection;
            }

            if (collection == null)
            {
                collection = new TCollectionType();
            }

            foreach (var value in values)
            {
                collection.Add(value);
            }

            return collection;
        }

        public static TCollectionType AddValueToCollection<T, TCollectionType>(TCollectionType collection, T value) where TCollectionType : ICollection<T>, new()
        {
            if (collection == null)
            {
                collection = new TCollectionType();
            }

            collection.Add(value);

            return collection;
        }
    }
}