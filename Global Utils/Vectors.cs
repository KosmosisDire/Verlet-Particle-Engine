using ComputeSharp;
using SFML.System;

public static class Vector2fExtensions
{
    public static Vector2f Add(this Vector2f v1, Vector2f v2)
    {
        return new Vector2f(v1.X + v2.X, v1.Y + v2.Y);
    }

    public static Vector2f Subtract(this Vector2f v1, Vector2f v2)
    {
        return new Vector2f(v1.X - v2.X, v1.Y - v2.Y);
    }

    public static Vector2f Multiply(this Vector2f v, float scale)
    {
        return new Vector2f(v.X * scale, v.Y * scale);
    }

    public static Vector2f Multiply(this Vector2f v1, Vector2f v2)
    {
        return new Vector2f(v1.X * v2.X, v1.Y * v2.Y);
    }

    public static Vector2f Divide(this Vector2f v, float scale)
    {
        return new Vector2f(v.X / scale, v.Y / scale);
    }

    public static Vector2f Divide(this Vector2f v1, Vector2f v2)
    {
        return new Vector2f(v1.X / v2.X, v1.Y / v2.Y);
    }

    public static float Dot(this Vector2f v1, Vector2f v2)
    {
        return v1.X * v2.X + v1.Y * v2.Y;
    }

    
    public static float Cross(this Vector2f v, Vector2f w)
    {
        return v.X * w.Y - v.Y * w.X;
    }

    public static float Magnitude(this Vector2f v)
    {
        return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
    }
    
    public static float SquareMagnitude(this Vector2f v)
    {
        return v.X * v.X + v.Y * v.Y;
    }

    public static Vector2f Normalized(this Vector2f v)
    {
        return v / v.Magnitude();
    }

    public static Vector2f PerpendicularClockwise(this Vector2f v)
    {
        return new Vector2f(-v.Y, v.X);
    }

    public static Vector2f PerpendicularCounterClockwise(this Vector2f v)
    {
        return new Vector2f(v.Y, -v.X);
    }

    public static float Angle(this Vector2f v1, Vector2f v2)
    {
        return (float)Math.Acos(v1.Dot(v2) / (v1.Magnitude() * v2.Magnitude()));
    }

    public static Vector2f Rotate(this Vector2f v, float angle)
    {
        float cos = (float)Math.Cos(angle);
        float sin = (float)Math.Sin(angle);
        return new Vector2f(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    public static Vector2f Reflect(this Vector2f v, Vector2f normal)
    {
        return v - 2 * v.Dot(normal) * normal.Normalized();
    }

    public static Vector2f Project(this Vector2f v, Vector2f b)
    {
        return b.Multiply(v.Dot(b) / b.Dot(b));
    }
}

public static class VectorConversions
{
    public static Vector2f ToVector2f(this float2 v)
    {
        return new Vector2f(v.X, v.Y);
    }

    public static Vector2i ToVector2i(this float2 v)
    {
        return new Vector2i((int)v.X, (int)v.Y);
    }

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
}

