using System;

namespace Resonance.Common
{
    public static class DynamicExtensions
    {
        public static DateTime GetDateTimeFromDynamic(dynamic value)
        {
            if (value == null)
            {
                return DateTime.MinValue;
            }

            if (value is DateTime)
            {
                return value;
            }

            return DateTime.Parse(value);
        }

        public static double GetDoubleFromDynamic(dynamic value)
        {
            if (value == null)
            {
                return 0.0;
            }

            if (value is double)
            {
                return value;
            }

            return Convert.ToDouble(value);
        }

        public static Guid GetGuidFromDynamic(dynamic value)
        {
            if (value == null)
            {
                return Guid.Empty;
            }

            if (value is Guid)
            {
                return value;
            }

            return new Guid(value);
        }

        public static int GetIntFromDynamic(dynamic value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is int)
            {
                return value;
            }

            if (value is string)
            {
                return int.Parse(value);
            }

            return Convert.ToInt32(value);
        }

        public static long GetLongFromDynamic(dynamic value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is long)
            {
                return value;
            }

            if (value is string)
            {
                return long.Parse(value);
            }

            return Convert.ToInt64(value);
        }
    }
}