using ComputeSharp;

namespace Engine.Rendering.Internal;



[AutoConstructor]
internal readonly partial struct DrawCirclesKernel : IComputeShader
{
    public readonly ReadWriteBuffer<float2> positions;
    public readonly ReadOnlyBuffer<uint> colors;
    public readonly ReadWriteBuffer<uint> bitmapDataInt;
    public readonly ReadOnlyBuffer<int> active;
    public readonly int2 resolution;
    public readonly Rect cameraRect;
    public readonly float radius;
    public readonly bool outlineOnly;
    

    public void SetColor(int2 coord, uint c)
    {
        if (Hlsl.All(coord >= 0) && Hlsl.All(coord < resolution))
        {
            bitmapDataInt[coord.X + coord.Y * resolution.X] = c;
        }
    }

    public void DrawCircleOutline(float2 pos, float radius, uint color)
    {
        // draws the outline of a circle using Bresenham's circle algorithm

        int x = (int)pos.X;
        int y = (int)pos.Y;
        int r = (int)radius;
        int d = 3 - 2 * r;

        int x1 = 0;
        int y1 = r;

        while (y1 >= x1)
        {
            SetColor(new int2(x + x1, y + y1), color);
            SetColor(new int2(x + y1, y + x1), color);
            SetColor(new int2(x - x1, y + y1), color);
            SetColor(new int2(x - y1, y + x1), color);
            SetColor(new int2(x + x1, y - y1), color);
            SetColor(new int2(x + y1, y - x1), color);
            SetColor(new int2(x - x1, y - y1), color);
            SetColor(new int2(x - y1, y - x1), color);

            if (d < 0)
                d += 4 * x1 + 6;
            else
            {
                d += 4 * (x1 - y1) + 10;
                y1--;
            }
            x1++;
        }

    }

    public void DrawCircleFromID(int id)
    {
        if(id >= positions.Length) return;

        if(active[id] == 0) return;

        float2 position = (positions[id] - cameraRect.topLeft) / (cameraRect.scale);

        if (Hlsl.Any(position < 0) || Hlsl.Any(position > resolution))
            return;
        
        float radiusAdjusted = radius / cameraRect.scale;
        float2 upper = position + radiusAdjusted + 1;
        float rsquared = radiusAdjusted * radiusAdjusted;
        int startY = (int)(position.Y - radiusAdjusted);

        if(outlineOnly)
        {
            DrawCircleOutline(position, radiusAdjusted, colors[id]);
            return;
        }

        for (int u = (int)(position.X - radiusAdjusted); u < upper.X; u++)
        {
            for (int v = startY; v < upper.Y; v++)
            {
                int2 coord = new int2(u, v);
                float2 dist = position - coord;
                if (Hlsl.Dot(dist, dist) < rsquared) 
                {
                    SetColor(coord, colors[id]);
                }
            }
        }
    }

    public void Execute()
    {
        for (int i = 0; i < 10; i++)
        {
            DrawCircleFromID(ThreadIds.X + positions.Length / 10 * i);
        }
    }
}
