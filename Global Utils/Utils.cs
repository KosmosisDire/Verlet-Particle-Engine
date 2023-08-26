using System.Diagnostics;
using SFML.Graphics;

public static class Utils
{
    public static uint RGBAToInt(uint r, uint g, uint b, uint a)
    {
        return ( r << 0 ) | ( g << 8 ) | ( b << 16 ) | ( a << 24 );
    }

    public static uint RGBAToInt(byte r, byte g, byte b, byte a)
    {
        return (uint)( r << 0 ) | (uint)( g << 8 ) | (uint)( b << 16 ) | (uint)( a << 24 );
    }

    public static uint RGBAToInt(uint4 rgba)
    {
        return RGBAToInt(rgba.X, rgba.Y, rgba.Z, rgba.W);
    }

    public static uint4 IntToRgba(uint value)
    {
        var red =   ( value >>  0 ) & 255;
        var green = ( value >>  8 ) & 255;
        var blue =  ( value >> 16 ) & 255;
        var alpha = ( value >> 24 ) & 255;
        return new uint4(red, green, blue, alpha); 
    }

    public static uint4 ColorLerp(uint4 a, uint4 b, float t)
    {
        return new uint4(
            (uint)(a.X + (b.X - a.X) * t),
            (uint)(a.Y + (b.Y - a.Y) * t),
            (uint)(a.Z + (b.Z - a.Z) * t),
            (uint)(a.W + (b.W - a.W) * t));
    }

    public static uint ToUInt32(this Color c)
    {
        return RGBAToInt(c.R, c.G, c.B, c.A);
    }

    public static uint IntColorLerp(uint a, uint b, float t)
    {
        var aRgba = IntToRgba(a);
        var bRgba = IntToRgba(b);
        var _r = (uint)(aRgba.X + (float)(bRgba.X - aRgba.X) * t);
        var _g = (uint)(aRgba.Y + (float)(bRgba.Y - aRgba.Y) * t);
        var _b = (uint)(aRgba.Z + (float)(bRgba.Z - aRgba.Z) * t);
        var _a = (uint)(aRgba.W + (float)(bRgba.W - aRgba.W) * t);
        return RGBAToInt(_r, _g, _b, _a);
    }

    public static int RoundToMagnitude(float value, int magnitude)
    {
        return (int)(MathF.Round(value / magnitude) * magnitude);
    }

    public static int CeilToMagnitude(float value, int magnitude)
    {
        return (int)(MathF.Ceiling(value / magnitude) * magnitude);
    }

    public static int FloorToMagnitude(float value, int magnitude)
    {
        return (int)(MathF.Floor(value / magnitude) * magnitude);
    }

    public static float RoundToMagnitude(float value, float magnitude)
    {
        return (float)(Math.Round((decimal)value / (decimal)magnitude) * (decimal)magnitude);
    }

    public static float CeilToMagnitude(float value, float magnitude)
    {
        return (float)(Math.Ceiling((decimal)value / (decimal)magnitude) * (decimal)magnitude);
    }

    public static float FloorToMagnitude(float value, float magnitude)
    {
        return (float)(Math.Floor((decimal)value / (decimal)magnitude) * (decimal)magnitude);
    }


    public static unsafe int SingleToInt32Bits(float value)
    {
        return *(int*)(&value);
    }

    private const double Epsilon = 1e-10;
    public static bool IsZero(this float d)
    {
        return Math.Abs(d) < Epsilon;
    }

    public static float TestTiming(Action action, int iterations)
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        sw.Stop();
        return sw.ElapsedTicks / (float)iterations;
    }

    public static (float time1, float time2, float totalTime1, float totalTime2) CompareTimings(Action action1, Action action2, int iterations)
    {
        float timeTotal1 = 0;
        float timeTotal2 = 0;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            action1();
            sw.Stop();
            timeTotal1 += sw.ElapsedTicks;

            sw.Restart();
            action2();
            sw.Stop();
            timeTotal2 += sw.ElapsedTicks;
        }

        //convert to milliseconds
        timeTotal1 /= 10000;
        timeTotal2 /= 10000;

        return (timeTotal1 / iterations, timeTotal2 / iterations, timeTotal1, timeTotal2);
    }

    public static (float time1, float time2, float time3, float totalTime1, float totalTime2, float totalTime3) CompareTimings(Action action1, Action action2, Action action3, int iterations)
    {
        float timeTotal1 = 0;
        float timeTotal2 = 0;
        float timeTotal3 = 0;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            action1();
            sw.Stop();
            timeTotal1 += sw.ElapsedTicks;

            sw.Restart();
            action2();
            sw.Stop();
            timeTotal2 += sw.ElapsedTicks;

            sw.Restart();
            action3();
            sw.Stop();
            timeTotal3 += sw.ElapsedTicks;
        }

        //convert to milliseconds
        timeTotal1 /= 10000;
        timeTotal2 /= 10000;
        timeTotal3 /= 10000;

        return (timeTotal1 / iterations, timeTotal2 / iterations, timeTotal3 / iterations, timeTotal1, timeTotal2, timeTotal3);
    }

    // compare four timings
    public static (float time1, float time2, float time3, float time4, float totalTime1, float totalTime2, float totalTime3, float totalTime4) CompareTimings(Action action1, Action action2, Action action3, Action action4, int iterations)
    {
        float timeTotal1 = 0;
        float timeTotal2 = 0;
        float timeTotal3 = 0;
        float timeTotal4 = 0;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            action1();
            sw.Stop();
            timeTotal1 += sw.ElapsedTicks;

            sw.Restart();
            action2();
            sw.Stop();
            timeTotal2 += sw.ElapsedTicks;

            sw.Restart();
            action3();
            sw.Stop();
            timeTotal3 += sw.ElapsedTicks;

            sw.Restart();
            action4();
            sw.Stop();
            timeTotal4 += sw.ElapsedTicks;
        }

        //convert to milliseconds
        timeTotal1 /= 10000;
        timeTotal2 /= 10000;
        timeTotal3 /= 10000;
        timeTotal4 /= 10000;

        return (timeTotal1 / iterations, timeTotal2 / iterations, timeTotal3 / iterations, timeTotal4 / iterations, timeTotal1, timeTotal2, timeTotal3, timeTotal4);
    }
}

