using ProtoGUI;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Engine;
using Engine.Rendering;
using ParticlePhysics;
using System.Collections.Concurrent;

public static class Application
{
    public static Canvas canvas;
    public static ParticleSystem particleSystem;
    public static Camera camera;

    static Particle lastParticle = null;

    private static Vector3f newColor = new(0.5f, 0.5f, 0.5f);
    private static Vector3f colorDirection = new(-0.04f, 0.03f, 0.02f);
    static void PhysicsUpdate(float dt)
    {
        if(Keyboard.IsKeyPressed(Keyboard.Key.Space))
        {
            dt = dt / 10f;
        }

        var random = new Random();
        if(Mouse.IsButtonPressed(Mouse.Button.Left) && !GUIManager.IsMouseCapturedByUI())
        {
            var tempNewColor = newColor + colorDirection;
            if(tempNewColor.X > 1 || tempNewColor.X < 0) colorDirection.X *= -1;
            if(tempNewColor.Y > 1 || tempNewColor.Y < 0) colorDirection.Y *= -1;
            if(tempNewColor.Z > 1 || tempNewColor.Z < 0) colorDirection.Z *= -1;

            newColor += colorDirection;
            Color c = new Color((byte)(newColor.X * 255), (byte)(newColor.Y * 255), (byte)(newColor.Z * 255));


            for(int i = 0; i < 5; i++)
            {
                var mousePositionNext = camera.ScreenToWorld((Vector2f)canvas.GetMousePosition() + new Vector2f(random.NextSingle() * 200 - 100, random.NextSingle() * 200 - 100));
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

            for(int i = 0; i < 1000; i++)
            {
                var topLeft = particleSystem.AddParticle(camera.ScreenToWorld(canvas.GetMousePosition()) + new Vector2f(random.NextSingle() * 400 - 200, random.NextSingle() * 400 - 200), c);
                // var topRight = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(particleSystem.particleRadius * 2, 0), c);
                // var bottomLeft = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(0, particleSystem.particleRadius * 2), c);
                
                // var diagonal = new Vector2f(particleSystem.particleRadius * 2, particleSystem.particleRadius * 2);
                // var bottomRight = particleSystem.AddParticle(topLeft.PositionF + diagonal, c);
                
                // topLeft.Link(topRight, particleSystem.particleRadius * 2);
                // topRight.Link(bottomRight, particleSystem.particleRadius * 2);
                // bottomRight.Link(bottomLeft, particleSystem.particleRadius * 2);
                // bottomLeft.Link(topLeft, particleSystem.particleRadius * 2);

                // var diagonalLength = diagonal.Magnitude();
                // topLeft.Link(bottomRight, diagonalLength);
                // topRight.Link(bottomLeft, diagonalLength);
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
        canvas.Clear();

        canvas.DrawParticleSystemBounds(particleSystem, new Color(52, 170, 134));
        canvas.ApplyCPUDraw();

        canvas.DrawParticleSystem(particleSystem);
        // particleSystem.grid.DrawGrid(canvas, new Color(28, 93, 73));
        // canvas.DrawLines(new Vector2f[] { new Vector2f(0, 0) }, new Vector2f[] { new Vector2f(100, 100) }, Color.Red);
        canvas.ApplyGPUDraw();
    }

    static void MainUpdate(float dt)
    {
        camera.UpdatePanning(Mouse.Button.Middle);
        camera.UpdateZooming();

        // Vector2f centerOfArea = (Vector2f)(particleSystem.grid.extents / 2);
        // Vector2f mousePosition = camera.ScreenToWorld((Vector2f)canvas.GetMousePosition());
        
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
                
        //         if(hitParticle != null) canvas.DrawLineCPU(centerOfArea, hitParticle.PositionF, Color.Red);
        //         if(hitParticle != null) hitParticle.SetColor(new uint4((uint)new Random().Next(), (uint)new Random().Next(), (uint)new Random().Next(), 255));
        //     }
        // });
    }

    static void SetupGUI()
    {
        Panel infoPanel = new Panel(new Vector2f(10, 10), new Vector2f(200, 157.5f))
        {
            topBarHeight = 15
        };

        var updateFPSDisplay = new UpdatableControl<string>("Update FPS: ", 15, () => EngineLoop.GetLoop("Update")?.measuredFPS.ToString("N0") ?? "0");
        infoPanel.AddControl(updateFPSDisplay);

        var fixedFPSDisplay = new UpdatableControl<string>("Physics FPS: ", 15, () => EngineLoop.GetLoop("Physics")?.measuredFPS.ToString("N0") ?? "0");
        infoPanel.AddControl(fixedFPSDisplay);

        var drawFPSDisplay = new UpdatableControl<string>("Draw FPS: ", 15, () => EngineLoop.GetLoop("Draw")?.measuredFPS.ToString("N0") ?? "0");
        infoPanel.AddControl(drawFPSDisplay);

        var gridTimeDisplay = new UpdatableControl<string>("Grid Time: ", 15, () => particleSystem.gridTime.ToString("N0") + " ms  -  " + (particleSystem.gridTime / particleSystem.totalUpdateTime * 100).ToString("N0") + "%");
        infoPanel.AddControl(gridTimeDisplay);

        var collisionTimeDisplay = new UpdatableControl<string>("Collision Time: ", 15, () => particleSystem.collisionTime.ToString("N0") + " ms  -  " + (particleSystem.collisionTime / particleSystem.totalUpdateTime * 100).ToString("N0") + "%");
        infoPanel.AddControl(collisionTimeDisplay);

        var linkTimeDisplay = new UpdatableControl<string>("Link Time: ", 15, () => particleSystem.linkTime.ToString("N0") + " ms  -  " + (particleSystem.linkTime / particleSystem.totalUpdateTime * 100).ToString("N0") + "%");
        infoPanel.AddControl(linkTimeDisplay);

        var copyTimeDisplay = new UpdatableControl<string>("Copy Time: ", 15, () => particleSystem.copyTime.ToString("N0") + " ms  -  " + (particleSystem.copyTime / particleSystem.totalUpdateTime * 100).ToString("N0") + "%");
        infoPanel.AddControl(copyTimeDisplay);

        var totalUpdateTimeDisplay = new UpdatableControl<string>("Total Update Time: ", 15, () => particleSystem.totalUpdateTime.ToString("N0") + " ms");
        infoPanel.AddControl(totalUpdateTimeDisplay);

        var particleCountDisplay = new UpdatableControl<string>("Particles: ", 15, () => particleSystem.particleCount.ToString("N0"));
        infoPanel.AddControl(particleCountDisplay);

        Panel panel = new Panel(new Vector2f(10, 300), new Vector2f(600, canvas.height / 2))
        {
            topBarHeight = 15
        };

        var radius = new Slider(label: "Radius", height: 15, defaultValue: 3, min: 0.5f, max: 20, step: 0.1f, (value) => particleSystem.SetRadius(value));
        panel.AddControl(radius);

        var antiPressurePower = new Slider("Anti Pressure Power", 15, 0.2f, 0.01f, 2, 0.01f, (value) => particleSystem.antiPressurePower = value);
        panel.AddControl(antiPressurePower);

        var iterations = new Slider("Iterations", 15, 5, 1, 20, 1, (value) => particleSystem.iterations = (int)value);
        panel.AddControl(iterations);

        var gravity = new Vector2Slider("Gravity", 150, new Vector2f(0, 0), new Vector2f(-300, -300), new Vector2f(300, 300), 1, (value) => particleSystem.gravity = value)
        {
            verticalMargin = 10
        };
        panel.AddControl(gravity);

        panel.AddControl(new Label("Click to place strings of particles", 15));
        panel.AddControl(new Label("Right click to place a bunch of boxes made of particles.", 15));
    }

    public static Loop update = EngineLoop.CreateLoop("Update", 60, false);
    public static Loop physicsUpdate = EngineLoop.CreateLoop("Physics", 60, false);
    public static Loop draw = EngineLoop.CreateLoop("Draw", 30, false);
    
    static void Main(string[] args)
    {
        Context context = new Context();

        draw.RunActionSync(() => 
        {
            canvas = new Canvas("Particle Sim", draw, 1920, 1080, true);
            canvas.SetFillColor(new uint4(28, 30, 38, 255));
            camera = new Camera(new Vector2f (1920 * 0.5f, 1080 * 0.5f), canvas);
        });

        update.RunActionSync(() => 
        {
            particleSystem = new ParticleSystem(1000000, canvas.width*1, canvas.width*1, 3f);
            SetupGUI();
        });

        update.Connect(MainUpdate);
        physicsUpdate.Connect(PhysicsUpdate);
        draw.Connect(DrawUpdate);

        EngineLoop.RunAll();
    }
    
}