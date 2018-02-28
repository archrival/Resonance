using System.Collections.Generic;

namespace Resonance.Common
{
    public static class CollectionExtensions
    {
        public static void AddValuesToCollection<T, CollectionType>(CollectionType collection, IEnumerable<T> values) where CollectionType : ICollection<T>, new()
        {
            if (values == null)
            {
                return;
            }

            if (collection == null)
            {
                collection = new CollectionType();
            }

            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        public static void AddValueToCollection<T, CollectionType>(CollectionType collection, T value) where CollectionType : ICollection<T>, new()
        {
            if (collection == null)
            {
                collection = new CollectionType();
            }

            collection.Add(value);
        }
    }
}