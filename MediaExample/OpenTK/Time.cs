using System;

namespace BaseLib
{
    public static class Time
    {
        public static long FromTicks(long ticks, long timeBase)
        {
            return Convert.ToInt64((double)ticks * timeBase / 10000000.0);
        }

        public static long ToTick(long time, long timeBase)
        {
            return Convert.ToInt64(time * 10000000.0 / timeBase);
        }
    }
}
