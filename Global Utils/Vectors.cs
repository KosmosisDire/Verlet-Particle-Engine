using ComputeSharp;
using SFML.System;

public static class VectorExtentions
{

    public static float2 ToFloat2(this Vector2f v)
    {
        return new float2(v.X, v.Y);
    }

    public static float2 ToFloat2(this Vector2i v)
    {
        return new float2((float)(v.X), (float)(v.Y));
    }

    public static int2 ToInt2(this Vector2f v)
    {
        return new int2((int)(v.X), (int)(v.Y));
    }

    public static int2 ToInt2(this Vector2i v)
    {
        return new int2(v.X, v.Y);
    }

    public static Vector2f ToVector2f(this float2 v)
    {
        return new Vector2f(v.X, v.Y);
    }

    public static Vector2i ToVector2i(this float2 v)
    {
        return new Vector2i((int)v.X, (int)v.Y);
    }

    public static Vector2f MirrorX(this Vector2f v)
    {
        return new Vector2f(-v.X, v.Y);
    }

    public static Vector2f MirrorY(this Vector2f v)
    {
        return new Vector2f(v.X, -v.Y);
    }

    public static float Length(this Vector2f v)
    {
        return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
    }

    public static float LengthSquared(this Vector2f v)
    {
        return v.X * v.X + v.Y * v.Y;
    }

    public static Vector2f Normalized(this Vector2f v)
    {
        return v / v.Length();
    }

    public static double Cross(this Vector2f v, Vector2f w)
    {
        return v.X * w.Y - v.Y * w.X;
    }

    public static double Dot(this Vector2f v, Vector2f w)
    {
        return v.X * w.X + v.Y * w.Y;
    }

    public static Vector2f Multiply(this Vector2f v, double s)
    {
        return new Vector2f((float)(v.X * s), (float)(v.Y * s));
    }

    public static Vector2f PerpendicularClockwise(this Vector2f vector2)
    {
        return new Vector2f(vector2.Y, -vector2.X);
    }

    public static Vector2f PerpendicularCounterClockwise(this Vector2f vector2)
    {
        return new Vector2f(-vector2.Y, vector2.X);
    }



}












