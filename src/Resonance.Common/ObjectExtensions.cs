using System.Linq;
using System.Reflection;

namespace Resonance.Common
{
    public static class ObjectExtensions
    {
        private const int HashFactor = 17;
        private const int HashSeed = 73;

        public static int GetHashCodeForObject<T>(this T graph, params object[] objects)
        {
            var hash = HashSeed;

            hash = hash * HashFactor + typeof(T).GetHashCode();

            return objects.Where(obj => obj != null).Aggregate(hash, (current, obj) => current * HashFactor + obj.GetHashCode());
        }

        public static bool PropertiesEqual<T>(this T left, T right, params string[] propertyNames) where T : class
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            var type = left.GetType();

            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property == null)
                {
                    return false;
                }

                var leftValue = property.GetValue(left);
                var rightValue = property.GetValue(right);

                if (leftValue != rightValue && (leftValue == null || !leftValue.Equals(rightValue)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}