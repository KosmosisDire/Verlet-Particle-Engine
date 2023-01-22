using ComputeSharp;
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

    public static unsafe int SingleToInt32Bits(float value)
    {
        return *(int*)(&value);
    }

    private const double Epsilon = 1e-10;
    public static bool IsZero(this double d)
    {
        return Math.Abs(d) < Epsilon;
    }
}