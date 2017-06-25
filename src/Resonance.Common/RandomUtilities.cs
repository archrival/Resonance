using System;
using System.Collections.Generic;

namespace Resonance.Common
{
    public static class RandomUtilities
    {
        public static HashSet<int> GetRandomUniqueIntegers(int min, int max, int count)
        {
            var random = new Random();
            var result = new HashSet<int>();

            if (max < count)
            {
                count = max;
            }

            for (int i = 0; i < count; i++)
            {
                result.Add(random.Next(min, max));
            }

            return result;
        }
    }
}