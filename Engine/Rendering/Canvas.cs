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

    public Canvas(string name, Loop drawLoop, int width = 1920, int height = 1080, bool fullscreen = false)
    {
        this.width = width;
        this.height = height;
        window = new RenderWindow(new VideoMode((uint)width, (uint)height), name, fullscreen ? Styles.Fullscreen : Styles.Default);
        window.SetVerticalSyncEnabled(true);
        window.SetFramerateLimit(144);

        window.Closed += (obj, e) => { window.Close(); EngineLoop.StopAllLoops();};
        window.KeyPressed += (sender, e) =>
        {
            if(sender == null) return;
            Window window = (Window)sender;
            if (e.Code == Keyboard.Key.Escape)
            {
                EngineLoop.StopAllLoops();
                window.Close();
            }
        };
        window.MouseMoved += (sender, e) => mousePosition = Mouse.GetPosition(window).ToFloat2();

        renderSprite = new Sprite(new Texture(new Image((uint)width, (uint)height)));

        bitmapData = new byte[width * height * 4];
        bitmapDataInt = new uint[width * height];
        blankData = new uint[width * height];

        bitmapBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(blankData);
        
        drawLoop.Connect(UpdateCanvas);
        
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
        GUIManager.Update(dt);
        window.Display();
    }

    public Vector2f GetMousePosition()
    {
        return ((Vector2f)Mouse.GetPosition(window));
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

    public void Clear()
    {
        blankData.CopyTo(bitmapDataInt, 0);
        bitmapBuffer.CopyFrom(blankData);
    }

    public void ApplyGPUDraw()
    {
        bitmapBuffer.CopyTo(bitmapDataInt);
    }

    public void ApplyCPUDraw()
    {
        bitmapBuffer.CopyFrom(bitmapDataInt);
    }

    public void DrawCircles(ReadWriteBuffer<float2> positions, ReadOnlyBuffer<uint> colors, ReadOnlyBuffer<int> active, float radius)
    {
        GraphicsDevice.GetDefault().For(positions.Length, new DrawCirclesKernel(positions, colors, bitmapBuffer, active, new int2(width, height), viewCamera.RectBoundsWorld, radius, true));
    }

    public void DrawLines(ReadWriteBuffer<float2> positions, ReadOnlyBuffer<int4> links)
    {
        GraphicsDevice.GetDefault().For(links.Length, new DrawLinksKernel(links, positions, bitmapBuffer, new int2(width, height), viewCamera.RectBoundsWorld, 0xe4b28fFF));
    }

    public void DrawLines(Vector2f[] starts, Vector2f[] ends, Color color)
    {
        if(starts.Length != ends.Length) throw new Exception("Starts and ends must be the same length");

        // create positions and links buffers from arrays
        // positions contains all start and end points
        // links contains the indices of the start and end points on x and y. z and w are unused for this since we do not need active flags or target lengths

        float2[] positionsArray = new float2[starts.Length + ends.Length];
        int4[] linksArray = new int4[starts.Length];

        for (var i = 0; i < starts.Length; i++)
        {
            positionsArray[i] = starts[i].ToFloat2();
            positionsArray[i + starts.Length] = ends[i].ToFloat2();
            linksArray[i] = new int4(i, i + starts.Length, 0, 0);
        }

        var positions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(positionsArray);
        var links = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer(linksArray);

        GraphicsDevice.GetDefault().For(links.Length, new DrawLinksKernel(links, positions, bitmapBuffer, new int2(width, height), viewCamera.RectBoundsWorld, color.ToInteger()));
    
        positions.Dispose();
        links.Dispose();
    }

    public void DrawPixelCPU(Vector2f pos, Color color)
    {
        if (pos.X < 0 || pos.X >= width || pos.Y < 0 || pos.Y >= height) return;
        bitmapDataInt[(int)pos.X + (int)pos.Y * width] = Utils.RGBAToInt(color.R, color.G, color.B, color.A);
    }

    public void DrawLineCPU(Vector2f start, Vector2f end, Color color, int dottedInterval = 0)
    {
        var startScreen = viewCamera.WorldToScreen(start);
        var endScreen = viewCamera.WorldToScreen(end);

        var x1 = (int)startScreen.X;
        var y1 = (int)startScreen.Y;
        var x2 = (int)endScreen.X;
        var y2 = (int)endScreen.Y;

        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        int flatIndex = 0;
        while (true)
        {
            if (dottedInterval == 0 || (flatIndex / dottedInterval) % 2 == 0)
            {
                DrawPixelCPU(new Vector2f(x1, y1), color);
            }

            flatIndex++;

            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
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

    public void DrawParticleSystem(ParticleSystem particleSystem)
    {
        lock(particleSystem.colorsCPU) particleSystem.CopyColorsToGPU();

        var gpuPositions = particleSystem.GetGPUPositions();
        var gpuLinks = particleSystem.GetGPULinks();
        var gpuColors = particleSystem.GetGPUColors();
        var gpuActive = particleSystem.GetGPUActive();

        DrawLines(gpuPositions, gpuLinks);
        DrawCircles(gpuPositions, gpuColors, gpuActive, particleSystem.particleRadius);
    }

    public void DrawParticleSystemBounds(ParticleSystem particleSystem, Color color)
    {
        DrawLineCPU(new Vector2f(0, 0), new Vector2f(particleSystem.worldExtents.X, 0), color, 20);
        DrawLineCPU(new Vector2f(particleSystem.worldExtents.X, 0), new Vector2f(particleSystem.worldExtents.X, particleSystem.worldExtents.Y), color, 20);
        DrawLineCPU(new Vector2f(particleSystem.worldExtents.X, particleSystem.worldExtents.Y), new Vector2f(0, particleSystem.worldExtents.Y), color, 20);
        DrawLineCPU(new Vector2f(0, particleSystem.worldExtents.Y), new Vector2f(0, 0), color, 20);
    }

}
