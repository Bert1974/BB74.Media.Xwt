using BaseLib.Media;
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
        public static long GetTime(long frame, FPS fps, long timebase)
        {
            return (long)((double)fps.Number.num * frame * timebase / fps.Number.den);
        }
        public static long GetFrame(long time, FPS fps, long timebase)
        {
            return (long)(double)((time * fps.Number.den) / (timebase * fps.Number.num));
        }
    }
}
