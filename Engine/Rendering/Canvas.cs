using SFML.Graphics;
using SFML.Window;
using ComputeSharp;
using ProtoGUI;
using SFML.System;
using ParticlePhysics;
using Engine.Rendering.Internal;

namespace Engine.Rendering;

public class Canvas
{
    public int width;
    public int height;
    public float2 mousePosition;

    public readonly RenderWindow window;
    public Camera viewCamera;
    byte[] bitmapData;
    uint[] bitmapDataInt;
    uint[] blankData;
    ReadWriteBuffer<uint> bitmapBuffer;
    readonly Sprite renderSprite;

    public Canvas(string name, int width = 1920, int height = 1080)
    {
        this.width = width;
        this.height = height;
        window = new RenderWindow(new VideoMode((uint)width, (uint)height), name, Styles.Fullscreen);
        window.SetVerticalSyncEnabled(true);
        window.SetFramerateLimit(144);

        window.Closed += (obj, e) => { window.Close(); EngineLoop.Quit();};
        window.KeyPressed += (sender, e) =>
        {
            if(sender == null) return;
            Window window = (Window)sender;
            if (e.Code == Keyboard.Key.Escape)
            {
                EngineLoop.Quit();
                window.Close();
            }
        };
        window.MouseMoved += (sender, e) => mousePosition = new float2(e.X, e.Y);

        renderSprite = new Sprite(new Texture(new Image((uint)width, (uint)height)));

        bitmapData = new byte[width * height * 4];
        bitmapDataInt = new uint[width * height];
        blankData = new uint[width * height];

        bitmapBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(blankData);
        
        EngineLoop.Update += UpdateCanvas;
        
        GUIManager.window = window;
    }

    public void SetFillColor(uint4 color)
    {
        blankData = Enumerable.Repeat(Utils.RGBAToInt(color), blankData.Length).ToArray();
    }

    void UpdateCanvas(float dt)
    {
        window.DispatchEvents();
        window.Clear();
        ApplyBitmap();
        window.Draw(renderSprite);
        GUIManager.Update();
        window.Display();
    }

    public void ApplyBitmap()
    {
        Buffer.BlockCopy(bitmapDataInt, 0, bitmapData, 0, bitmapData.Length);
        renderSprite.Texture.Update(bitmapData);
    }

    public void SetScreenBytes(uint[] bytes)
    {
        bitmapDataInt = bytes;
    }

    public void ClearBitmapBuffer()
    {
        bitmapBuffer.CopyFrom(blankData);
    }

    public void ApplyBitmapBuffer()
    {
        bitmapBuffer.CopyTo(bitmapDataInt);
    }

    public void DrawCircles(ReadWriteBuffer<float2> positions, ReadOnlyBuffer<uint> colors, ReadOnlyBuffer<int> active, float radius)
    {
        GraphicsDevice.GetDefault().For(positions.Length, new DrawCirclesKernel(positions, colors, bitmapBuffer, active, new int2(width, height), viewCamera.RectBounds, radius, true));
    }

    public void DrawLines(ReadWriteBuffer<float2> positions, ReadOnlyBuffer<int4> links)
    {
        GraphicsDevice.GetDefault().For(links.Length, new DrawLinksKernel(links, positions, bitmapBuffer, new int2(width, height), viewCamera.RectBounds));
    }

    public void DrawPixelCPU(Vector2f pos, Color color)
    {
        int x = (int)pos.X;
        int y = (int)pos.Y;
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        bitmapDataInt[x + y * width] = Utils.RGBAToInt(color.R, color.G, color.B, color.A);
    }

    public void DrawLineCPU(Vector2f start, Vector2f end, Color color, int dottedInterval = 0)
    {
        //adjust for camera bounds
        start = viewCamera.WorldToScreen(start);
        end = viewCamera.WorldToScreen(end);

        Vector2f delta = end - start;
        float length = delta.Length();
        delta /= length;
        bool draw = true;
        for (int i = 0; i < length; i++)
        {
            if(draw)
            {
                Vector2f pos = start + delta * i;
                DrawPixelCPU(pos, color);
            }

            if(dottedInterval > 0)
            {
                if(i % dottedInterval == 0)
                    draw = !draw;
            }
        }
    }

    public void DrawCircleCPU(Vector2f pos, float radius, Color color)
    {
        // draws the outline of a circle using Bresenham's circle algorithm

        //adjust for camera bounds
        pos = viewCamera.WorldToScreen(pos);

        int x = (int)pos.X;
        int y = (int)pos.Y;
        int r = (int)radius;

        int d = 3 - 2 * r;

        int x1 = 0;
        int y1 = r;

        while (y1 >= x1)
        {
            DrawPixelCPU(new Vector2f(x + x1, y + y1), color);
            DrawPixelCPU(new Vector2f(x + y1, y + x1), color);
            DrawPixelCPU(new Vector2f(x - x1, y + y1), color);
            DrawPixelCPU(new Vector2f(x - y1, y + x1), color);
            DrawPixelCPU(new Vector2f(x + x1, y - y1), color);
            DrawPixelCPU(new Vector2f(x + y1, y - x1), color);
            DrawPixelCPU(new Vector2f(x - x1, y - y1), color);
            DrawPixelCPU(new Vector2f(x - y1, y - x1), color);

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

    public void DrawParticleSystem(ParticleSystem system, bool debugTime = false, bool drawBounds = true)
    {
        system.CopyColorsToGPU();
        
        ClearBitmapBuffer();
        DrawCircles(system.GetGPUPositions(), system.GetGPUColors(), system.GetGPUActive(), system.particleRadius);
        DrawLines(system.GetGPUPositions(), system.GetGPULinks());
        ApplyBitmapBuffer();
        
        if (drawBounds)
        {
            DrawLineCPU(new Vector2f(0, 0), new Vector2f(system.worldExtents.X, 0), Color.Blue, 10);
            DrawLineCPU(new Vector2f(system.worldExtents.X, 0), new Vector2f(system.worldExtents.X, system.worldExtents.Y), Color.Blue, 10);
            DrawLineCPU(new Vector2f(system.worldExtents.X, system.worldExtents.Y), new Vector2f(0, system.worldExtents.Y), Color.Blue, 10);
            DrawLineCPU(new Vector2f(0, system.worldExtents.Y), new Vector2f(0, 0), Color.Blue, 10);
        }
    }

}
