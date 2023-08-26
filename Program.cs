using ProtoGUI;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Engine;
using Engine.Rendering;
using ParticlePhysics;

public static class Application
{
    public static readonly Loop update = EngineLoop.CreateLoop("Update", 60, false);
    public static readonly Loop physicsUpdate = EngineLoop.CreateLoop("Physics", 60, false);
    public static readonly Loop draw = EngineLoop.CreateLoop("Draw", 30, false);

    private static Screen? screen;
    private static ParticleSystem? particleSystem;
    private static Camera? camera;

    static Particle? lastParticle = null;

    private static Vector3f newColor = new(0.5f, 0.5f, 0.5f);
    private static Vector3f colorDirection = new(-0.04f, 0.03f, 0.02f);
    
    private static void PhysicsUpdate(float dt)
    {
        if(screen == null || particleSystem == null || camera == null) {
            throw new Exception("Screen, particleSystem or camera is null");
        }

        if(Keyboard.IsKeyPressed(Keyboard.Key.Space))
        {
            dt /= 10f;
        }

        float brushSize = particleSystem.boundsSize.X / 5f;

        var random = new Random();
        if(Mouse.IsButtonPressed(Mouse.Button.Left) && !GUIManager.IsMouseCapturedByUI())
        {
            var tempNewColor = newColor + colorDirection;
            if(tempNewColor.X > 1 || tempNewColor.X < 0) colorDirection.X *= -1;
            if(tempNewColor.Y > 1 || tempNewColor.Y < 0) colorDirection.Y *= -1;
            if(tempNewColor.Z > 1 || tempNewColor.Z < 0) colorDirection.Z *= -1;

            newColor += colorDirection;
            Color c = new Color((byte)(newColor.X * 255), (byte)(newColor.Y * 255), (byte)(newColor.Z * 255));

            for(int i = 0; i < 50; i++)
            {
                var mousePositionNext = screen.ScreenToWorld(screen.mousePosition + new Vector2f(random.NextSingle() * 10 - 5, random.NextSingle() * 10 - 5));
                var position = mousePositionNext;
                if(lastParticle != null)
                {
                    position = lastParticle.PositionF + (mousePositionNext - lastParticle.PositionF).Normalized() * particleSystem.particleRadius * 2;
                }

                var p = particleSystem.AddParticle(position, c);
                if(lastParticle != null)
                {
                    lastParticle.Link(p, particleSystem.particleRadius*2);
                }

                lastParticle = p;
            }
        }

        if(Mouse.IsButtonPressed(Mouse.Button.Right) && !GUIManager.IsMouseCapturedByUI())
        {
            var tempNewColor = newColor + colorDirection;
            if(tempNewColor.X > 1 || tempNewColor.X < 0) colorDirection.X *= -1;
            if(tempNewColor.Y > 1 || tempNewColor.Y < 0) colorDirection.Y *= -1;
            if(tempNewColor.Z > 1 || tempNewColor.Z < 0) colorDirection.Z *= -1;

            newColor += colorDirection;
            Color c = new Color((byte)(newColor.X * 255), (byte)(newColor.Y * 255), (byte)(newColor.Z * 255));

            float spacing = particleSystem.particleRadius * 12;
            for(int i = 0; i < MathF.Pow(brushSize / spacing, 2); i++)
            {
                // place particles on a grid centered on the mouse not randomly
                Vector2f position = screen.ScreenToWorld(screen.mousePosition) - new Vector2f(brushSize / 2, brushSize / 2) + new Vector2f((i % (brushSize / spacing)) * spacing, MathF.Floor(i / (brushSize / spacing)) * spacing);
                var topLeft = particleSystem.AddParticle(position, c);
                var topRight = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(particleSystem.particleRadius * 2, 0), c);
                var bottomLeft = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(0, particleSystem.particleRadius * 2), c);
                
                var diagonal = new Vector2f(particleSystem.particleRadius * 2, particleSystem.particleRadius * 2);
                var bottomRight = particleSystem.AddParticle(topLeft.PositionF + diagonal, c);
                
                topLeft.Link(topRight, particleSystem.particleRadius * 2);
                topRight.Link(bottomRight, particleSystem.particleRadius * 2);
                bottomRight.Link(bottomLeft, particleSystem.particleRadius * 2);
                bottomLeft.Link(topLeft, particleSystem.particleRadius * 2);

                var diagonalLength = diagonal.Magnitude();
                topLeft.Link(bottomRight, diagonalLength);
                topRight.Link(bottomLeft, diagonalLength); 
            }
        }

        if(Mouse.IsButtonPressed(Mouse.Button.XButton1))
        {
            foreach (var particle in particleSystem.particles.GetArray())
            {
                if(particle != null)
                    particle.SetColor(new uint4(0,50,200,255));
            }

            for (int i = 0; i < particleSystem.grid.cellCountLinear; i++)
            {
                var indicies = particleSystem.GetParticlesInGridPosition(i);

                foreach (int particleIndex in indicies)
                {
                    var p = particleSystem.particles[particleIndex];
                    if(p != null)
                        p.SetColor(new uint4(50,50,50,255));
                }
            }

            foreach (var particleIndex in particleSystem.particles.GetActiveArray())
            {
                var particle = particleSystem.particles[particleIndex];
                if(particle != null)
                    particle.SetColor(new uint4(0,200,0,255));
            }
            
        }

        particleSystem.SolveParticles(dt);
    }

    static void DrawUpdate(float dt)
    {
        if(screen == null || particleSystem == null || camera == null) {
            throw new Exception("Screen, particleSystem or camera is null");
        }

        screen.Clear();

        screen.DrawParticleSystemBounds(particleSystem, new Color(52, 170, 134));
        screen.ApplyCPUDraw();

        screen.DrawParticleSystem(particleSystem);
        screen.ApplyGPUDraw();
    }

    static void MainUpdate(float dt)
    {
        if(screen == null || particleSystem == null || camera == null) {
            throw new Exception("Screen, particleSystem or camera is null");
        }

        camera.UpdatePanning(Mouse.Button.Middle);
        camera.UpdateZooming();
        

        // Vector2f centerOfArea = (Vector2f)(particleSystem.grid.extents / 2);
        // Vector2f mousePosition = camera.ScreenToWorld((Vector2f)screen.GetMousePosition());
        
        // // partitioner parallel raycasts
        // var partitioner = Partitioner.Create(0, 360, 360 / Environment.ProcessorCount);

        // Parallel.ForEach(partitioner, (range, loopState) => 
        // {
        //     for (float i = range.Item1; i < range.Item2; i+=0.5f)
        //     {
        //         var angle = i * Math.PI / 180;
        //         var direction = new Vector2f((float)Math.Cos(angle), (float)Math.Sin(angle));
        //         var distance = (mousePosition - centerOfArea).Magnitude();
        //         Particle hitParticle = particleSystem.Raycast(centerOfArea, direction, distance, out _);
                
        //         if(hitParticle != null) screen.DrawLineCPU(centerOfArea, hitParticle.PositionF, Color.Red);
        //         if(hitParticle != null) hitParticle.SetColor(new uint4((uint)new Random().Next(), (uint)new Random().Next(), (uint)new Random().Next(), 255));
        //     }
        // });
    }

    static void SetupGUI()
    {
        if(screen == null || particleSystem == null || camera == null) 
        {
            throw new Exception("Screen, particleSystem or camera is null");
        }
        
        Panel infoPanel = new Panel(new Vector2f(10, 10), 200, screen);

        new UpdatableControl<string>("Update FPS: ",        infoPanel, () => EngineLoop.GetLoop("Update")?.measuredFPS.ToString("N0") ?? "0");
        new UpdatableControl<string>("Physics FPS: ",       infoPanel, () => EngineLoop.GetLoop("Physics")?.measuredFPS.ToString("N0") ?? "0");
        new UpdatableControl<string>("Draw FPS: ",          infoPanel, () => EngineLoop.GetLoop("Draw")?.measuredFPS.ToString("N0") ?? "0");
        new Space(15, infoPanel);
        new UpdatableControl<string>("Grid Time: ",         infoPanel, () => particleSystem.gridTime.ToString("N0") + " ms  -  " + (particleSystem.gridTime / particleSystem.totalBuildTime * 100).ToString("N0") + "%");
        new UpdatableControl<string>("Link Time: ",         infoPanel, () => particleSystem.linkTime.ToString("N0") + " ms  -  " + (particleSystem.linkTime / particleSystem.totalBuildTime * 100).ToString("N0") + "%");
        new UpdatableControl<string>("Strain Time: ",       infoPanel, () => particleSystem.strainTime.ToString("N0") + " ms  -  " + (particleSystem.strainTime / particleSystem.totalBuildTime * 100).ToString("N0") + "%");
        new UpdatableControl<string>("Total Build Time: ",  infoPanel, () => particleSystem.totalBuildTime.ToString("N0") + " ms");
        new Space(15, infoPanel);
        new UpdatableControl<string>("Collision Time: ",    infoPanel, () => particleSystem.collisionTime.ToString("N0") + " ms  -  " + (particleSystem.collisionTime / particleSystem.totalSolveTime * 100).ToString("N0") + "%");
        new UpdatableControl<string>("Copy Time: ",         infoPanel, () => particleSystem.copyTime.ToString("N0") + " ms  -  " + (particleSystem.copyTime / particleSystem.totalSolveTime * 100).ToString("N0") + "%");
        new UpdatableControl<string>("Total Solve Time: ",  infoPanel, () => particleSystem.totalSolveTime.ToString("N0") + " ms");
        new Space(15, infoPanel);
        new UpdatableControl<string>("Particles: ",         infoPanel, () => particleSystem.ParticleCount.ToString("N0"));
        new UpdatableControl<string>("Links: ",             infoPanel, () => particleSystem.LinkCount.ToString("N0"));

        Panel controlPanel = new Panel(new Vector2f(10, 300), 600, screen);

        // var radius = new Slider("Radius", controlPanel, (value) => particleSystem.SetRadius(value), 50f, 10,100, 1f);
        new Slider("Anti Pressure Power", controlPanel, (value) => particleSystem.antiPressurePower = value, 0.1f, 0.01f, 2, 0.01f);
        new Slider("Iterations", controlPanel, (value) => particleSystem.iterations = (int)value, 12, 1, 20, 1);
        var gravity = new Vector2Slider("Gravity", controlPanel, (value) => particleSystem.gravity = value, new Vector2f(0, 0), new Vector2f(-100, -100), new Vector2f(100, 100), 1)
        {
            LineHeight = 150,
            margin = new(0, 10)
        };

        gravity.getDisplayValue = () => $"({gravity.Value.X:N0}, {gravity.Value.Y:N0})";

        new Label("Click to place strings of particles", controlPanel){margin = new(0, 20)};
        new Label("Right click to place a bunch of boxes made of particles.", controlPanel){margin = new(0, 20)};
    }


    
    static void Main(string[] args)
    {
        Context context = new Context();

        draw.RunActionSync(() => 
        {
            screen = new Screen("Particle Sim", draw, 1920, 1080, true);
            screen.SetFillColor(new uint4(28, 30, 38, 255));
            camera = new Camera(new Vector2f (25000, 25000), 60, screen);
        });

        update.RunActionSync(() => 
        {
            if(screen == null) return;

            particleSystem = new ParticleSystem(1000000, 2000000, 30000, 30000, 10);
            SetupGUI();
        });

        update.Connect(MainUpdate);
        physicsUpdate.Connect(PhysicsUpdate);
        draw.Connect(DrawUpdate);

        EngineLoop.RunAll();
    }
    
}