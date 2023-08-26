using SFML.Graphics;
using SFML.Window;
using ComputeSharp;
using ProtoGUI;
using SFML.System;
using ParticlePhysics;
using Engine.Rendering.Internal;
using System.Collections.Concurrent;

namespace Engine.Rendering;

public class Screen
{
    public int width;
    public int height;
    public Vector2f Resolution => new(width, height);
    public Rect Bounds => new(new(0,0), new(width, height));
    public bool isMouseOnScreen;
    public Vector2f mousePosition;
    private Vector2f lastMousePosition;
    public Vector2f mouseDelta;
    public Vector2f mouseDeltaWorld;
    public float wheelDelta;

    public readonly RenderWindow window;

    readonly byte[] bitmapData;
    uint[] bitmapDataInt;
    uint[] blankData;
    readonly ReadWriteBuffer<uint> bitmapBuffer;
    readonly Sprite renderSprite;
    private Camera? activeCamera;
    public Camera? ActiveCamera 
    { 
        get => activeCamera; 
        set 
        {
            if(value == null) return;
            activeCamera = value;
            if(activeCamera.ViewingScreen != this) activeCamera.ViewingScreen = this;
        }
    }

    public Screen(string name, Loop drawLoop, int width = 1920, int height = 1080, bool fullscreen = false)
    {
        this.width = width;
        this.height = height;
        window = new RenderWindow(new VideoMode((uint)width, (uint)height), name, fullscreen ? Styles.Fullscreen : Styles.Default);
        window.SetVerticalSyncEnabled(true);
        window.SetFramerateLimit(144);

        window.Closed += (obj, e) => { window.Close(); EngineLoop.AbortAllLoops();};
        window.KeyPressed += (sender, e) =>
        {
            if(sender == null) return;
            Window window = (Window)sender;
            if (e.Code == Keyboard.Key.Escape)
            {
                EngineLoop.AbortAllLoops();
                window.Close();
            }
        };

        window.MouseWheelScrolled += (sender, e) => 
        {
            wheelDelta = e.Delta;
        };

        renderSprite = new Sprite(new Texture(new Image((uint)width, (uint)height)));

        bitmapData = new byte[width * height * 4];
        bitmapDataInt = new uint[width * height];
        blankData = new uint[width * height];

        bitmapBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(blankData);
        
        drawLoop.RunAction(() => drawLoop.Connect(UpdateScreen));
        
        GUIManager.window = window;
    }

    public Vector2f ScreenToWorld(Vector2f screenPos)
    {
        return (screenPos * activeCamera?.scale) + (activeCamera?.centerWorld - activeCamera?.WorldSize/2) ?? screenPos;
    }

    public Vector2f ScreenToWorld(Vector2i screenPos)
    {
        return ScreenToWorld((Vector2f)screenPos);
    }

    public Vector2f WorldToScreen(Vector2f worldPos)
    {
        return ((worldPos - (activeCamera?.centerWorld - activeCamera?.WorldSize/2)) / activeCamera?.scale) ?? worldPos;
    }

    public Vector2f WorldToScreen(Vector2i worldPos)
    {
        return WorldToScreen((Vector2f)worldPos);
    }

    public void SetFillColor(uint4 color)
    {
        blankData = Enumerable.Repeat(Utils.RGBAToInt(color), blankData.Length).ToArray();
    }

    void UpdateScreen(float dt)
    {
        wheelDelta = 0;
        window.DispatchEvents();

        mousePosition = window.MapPixelToCoords(Mouse.GetPosition(window));
        isMouseOnScreen = mousePosition.X >= 0 && mousePosition.X < width && mousePosition.Y >= 0 && mousePosition.Y < height;
        mouseDelta = mousePosition - lastMousePosition;
        mouseDeltaWorld = ScreenToWorld(mousePosition) - ScreenToWorld(lastMousePosition);
        lastMousePosition = mousePosition;


        window.Clear();
        ApplyBitmap();
        window.Draw(renderSprite);
        GUIManager.Update(dt);
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
        GraphicsDevice.GetDefault().For(positions.Length/10, new DrawCirclesKernel(positions, colors, bitmapBuffer, active, new int2(width, height), activeCamera?.RectBoundsWorld ?? Bounds, radius, true));
    }

    public void DrawLines(ReadWriteBuffer<float2> positions, ReadOnlyBuffer<int4> links)
    {
        GraphicsDevice.GetDefault().For(links.Length/10, new DrawLinksKernel(links, positions, bitmapBuffer, new int2(width, height), activeCamera?.RectBoundsWorld ?? Bounds, 0xe4b28fFF));
    }

    public void DrawLines(Vector2f[] starts, Vector2f[] ends, Color color)
    {
        if(starts.Length != ends.Length) throw new Exception("Starts and ends must be the same length");

        // create positions and links buffers from arrays
        // positions contains all start and end points
        // links contains the indices of the start and end points on x and y. z and w are unused for this since we do not need active flags or target lengths

        float2[] positionsArray = new float2[starts.Length + ends.Length];
        int4[] linksArray = new int4[starts.Length];

        for (int i = 0; i < starts.Length; i++)
        {
            positionsArray[i] = starts[i].ToFloat2();
            positionsArray[i + starts.Length] = ends[i].ToFloat2();
            linksArray[i] = new int4(i, i + starts.Length, 1, 1);
        }

        var positions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(positionsArray);
        var links = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer(linksArray);

        GraphicsDevice.GetDefault().For(links.Length, new DrawLinksKernel(links, positions, bitmapBuffer, new int2(width, height), activeCamera?.RectBoundsWorld ?? Bounds, color.ToInteger()));

        positions.Dispose();
        links.Dispose();
    }

    public void DrawLinesCPU(Vector2f[] starts, Vector2f[] ends, Color color, int dottedInterval = 0)
    {
        if(starts.Length != ends.Length) throw new Exception("Starts and ends must be the same length");

        var partitioner = Partitioner.Create(0, starts.Length, starts.Length/Environment.ProcessorCount);

        Parallel.ForEach(partitioner, (range, loopState) =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                DrawLineCPU(starts[i], ends[i], color, dottedInterval);
            }
        });
    }

    public void DrawPixelCPU(Vector2f pos, Color color)
    {
        if (pos.X < 0 || pos.X >= width || pos.Y < 0 || pos.Y >= height) return;
        bitmapDataInt[(int)pos.X + (int)pos.Y * width] = Utils.RGBAToInt(color.R, color.G, color.B, color.A);
    }

    public void DrawLineCPU(Vector2f start, Vector2f end, Color color, int dottedInterval = 0)
    {
        var startScreen = WorldToScreen(start);
        var endScreen = WorldToScreen(end);

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

    // draws the outline of a circle using Bresenham's circle algorithm
    public void DrawCircleCPU(Vector2f pos, float radius, Color color)
    {
        //adjust for camera bounds
        pos = WorldToScreen(pos);

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
        // var gpuLinks = particleSystem.GetGPULinks();
        var gpuColors = particleSystem.GetGPUColors();
        var gpuActive = particleSystem.GetGPUActive();

        // DrawLines(gpuPositions, gpuLinks);
        DrawCircles(gpuPositions, gpuColors, gpuActive, particleSystem.particleRadius);
    }

    public void DrawParticleSystemBounds(ParticleSystem particleSystem, Color color)
    {
        DrawLineCPU(new Vector2f(0, 0), new Vector2f(particleSystem.boundsSize.X, 0), color, 20);
        DrawLineCPU(new Vector2f(particleSystem.boundsSize.X, 0), new Vector2f(particleSystem.boundsSize.X, particleSystem.boundsSize.Y), color, 20);
        DrawLineCPU(new Vector2f(particleSystem.boundsSize.X, particleSystem.boundsSize.Y), new Vector2f(0, particleSystem.boundsSize.Y), color, 20);
        DrawLineCPU(new Vector2f(0, particleSystem.boundsSize.Y), new Vector2f(0, 0), color, 20);
    }

}
