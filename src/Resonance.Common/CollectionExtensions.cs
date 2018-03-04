using System.Collections.Generic;

namespace Resonance.Common
{
    public static class CollectionExtensions
    {
        public static CollectionType AddValuesToCollection<T, CollectionType>(CollectionType collection, IEnumerable<T> values) where CollectionType : ICollection<T>, new()
        {
            if (values == null)
            {
                return collection;
            }

            if (collection == null)
            {
                collection = new CollectionType();
            }

            foreach (var value in values)
            {
                collection.Add(value);
            }

            return collection;
        }

        public static CollectionType AddValueToCollection<T, CollectionType>(CollectionType collection, T value) where CollectionType : ICollection<T>, new()
        {
            if (collection == null)
            {
                collection = new CollectionType();
            }

            collection.Add(value);

            return collection;
        }
    }
}