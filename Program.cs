using ProtoGUI;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Engine;
using Engine.Rendering;
using ParticlePhysics;

public static class Application
{
    public static Canvas canvas;
    public static ParticleSystem particleSystem;
    public static Camera camera;

    static Particle lastParticle = null;

    static void FixedUpdate(float dt)
    {

        if(Keyboard.IsKeyPressed(Keyboard.Key.Space))
        {
            dt = dt / 10f;
        }

        var random = new Random();
        if(Mouse.IsButtonPressed(Mouse.Button.Left) && MouseGestures.overUI == false)
        {
            for(int i = 0; i < 5; i++)
            {
                var mousePositionNext = camera.ScreenToWorld((Vector2f)Mouse.GetPosition() + new Vector2f(random.NextSingle() * 200 - 100, random.NextSingle() * 200 - 100));
                var position = mousePositionNext;
                if(lastParticle != null)
                {
                    position = lastParticle.PositionF + (mousePositionNext - lastParticle.PositionF).Normalized() * particleSystem.particleRadius * 2;
                }

                var p = particleSystem.AddParticle(position);
                if(lastParticle != null)
                {
                    lastParticle.Link(p, particleSystem.particleRadius*2);
                }

                lastParticle = p;
            }
        }

        if(Mouse.IsButtonPressed(Mouse.Button.Right))
        {
            for(int i = 0; i < 1000; i++)
            {
                byte[] color = new byte[3];
                random.NextBytes(color);
                Color c = new Color(color[0], color[1], color[2]);

                var topLeft = particleSystem.AddParticle(camera.ScreenToWorld(Mouse.GetPosition()) + new Vector2f(random.NextSingle() * 600 - 300, random.NextSingle() * 600 - 300), c);
                var topRight = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(particleSystem.particleRadius * 2, 0), c);
                var bottomLeft = particleSystem.AddParticle(topLeft.PositionF + new Vector2f(0, particleSystem.particleRadius * 2), c);
                
                var diagonal = new Vector2f(particleSystem.particleRadius * 2, particleSystem.particleRadius * 2);
                var bottomRight = particleSystem.AddParticle(topLeft.PositionF + diagonal, c);
                
                topLeft.Link(topRight, particleSystem.particleRadius * 2);
                topRight.Link(bottomRight, particleSystem.particleRadius * 2);
                bottomRight.Link(bottomLeft, particleSystem.particleRadius * 2);
                bottomLeft.Link(topLeft, particleSystem.particleRadius * 2);

                var diagonalLength = diagonal.Length();
                topLeft.Link(bottomRight, diagonalLength);
                topRight.Link(bottomLeft, diagonalLength);
            }
        }

        particleSystem.SolveParticles(dt);
        canvas.DrawParticleSystem(particleSystem);


        Vector2f centerOfArea = (Vector2f)(particleSystem.grid.extents / 2);
        Vector2f mousePosition = camera.ScreenToWorld((Vector2f)Mouse.GetPosition());

        Particle hitParticle = particleSystem.Raycast(centerOfArea, mousePosition - centerOfArea, (mousePosition - centerOfArea).Length(), out var intersections, out var samples);

        for (int i = 0; i < intersections.Count(); i++)
        {
            var intersection = intersections[i];
            canvas.DrawCircleCPU(intersection, 2, Color.Red);
        }
        
        for (int i = 0; i < samples.Count(); i++)
        {
            var sample = samples[i];
            if(particleSystem.grid.gridValueCountsArray[particleSystem.grid.GetIndex(sample)] > 0)
                canvas.DrawCircleCPU(sample, 2, Color.Green);
        }

        if(hitParticle != null) canvas.DrawCircleCPU(hitParticle.PositionF, 25f, Color.Yellow);
        else
        {
            canvas.DrawCircleCPU(mousePosition, 25f, Color.Blue);
        }
        
    }

    static void Update(float dt)
    {
        camera.UpdatePanning(Mouse.Button.Middle);
        camera.UpdateZooming();

        // Update Living Cells
        // var partitions = Partitioner.Create(0, particleSystem.particles.Count);
        // Parallel.ForEach(partitions, (range, loopState) =>
        // {
        //     for(int i = range.Item1; i < range.Item2; i++)
        //     {
        //         (particleSystem.particles[i] as LivingCell)?.Update(dt);
        //     }
        // });
    }

    static void SetupGUI()
    {
        Panel infoPanel = new Panel(new Vector2f(10, 10), new Vector2f(100, 67.5f));
        infoPanel.topBarHeight = 15;

        var fixedFPSDisplay = new FloatDisplay("Fixed FPS: ", 15);
        fixedFPSDisplay.ConnectBeforeDraw(() => fixedFPSDisplay.value = EngineLoop.MeasuredFixedUpdateFPS);
        infoPanel.AddControl(fixedFPSDisplay);

        var updateFPSDisplay = new FloatDisplay("Update FPS: ", 15);
        updateFPSDisplay.ConnectBeforeDraw(() => updateFPSDisplay.value = EngineLoop.MeasuredUpdateFPS);
        infoPanel.AddControl(updateFPSDisplay);

        var particleCountDisplay = new FloatDisplay("Particles: ", 15);
        particleCountDisplay.ConnectBeforeDraw(() => particleCountDisplay.value = particleSystem.particleCount);
        particleCountDisplay.decimalPlaces = 0;
        infoPanel.AddControl(particleCountDisplay);

        Panel panel = new Panel(new Vector2f(10, 65), new Vector2f(canvas.width/6, canvas.height/2));
        panel.topBarHeight = 15;

        var radius = new Slider("Radius", 15, 3, 0.5f, 20);
        radius.ConnectValueChanged(() => particleSystem.SetRadius(radius.value));
        panel.AddControl(radius);

        var colorSmoothing = new Slider("Color Smoothing", 15, 0.2f, 0.00001f, 1);
        colorSmoothing.ConnectValueChanged(() => particleSystem.showThreshold = colorSmoothing.value);
        panel.AddControl(colorSmoothing);
        
        var gravity = new Vector2Slider("Gravity", 150, new Vector2f(0, 0), new Vector2f(-1500, -1500), new Vector2f(1500, 1500), 0.01f);
        gravity.OnValueChanged += (value) => particleSystem.gravity = value;
        gravity.verticalMargin = 10;
        panel.AddControl(gravity);

        var antiPressurePower = new Slider("Anti Pressure Power", 15, 0.2f, 0.01f, 2, 0.01f);
        antiPressurePower.ConnectValueChanged(() => particleSystem.antiPressurePower = antiPressurePower.value);
        panel.AddControl(antiPressurePower);

        panel.AddControl(new Label("Click to place strings of particles", 15));
        panel.AddControl(new Label("Right click to place a bunch of boxes made of particles.", 15));
    }
    
    static void Main(string[] args)
    {
        EngineLoop.RunInUpdateThread(() => 
        {
            canvas = new Canvas("Particle Sim", (int)(1920), (int)(1080));
            canvas.SetFillColor(new uint4(12, 9, 16, 255));
            camera = new Camera(new Vector2f (1920 * 0.5f, 1080 * 0.5f), canvas);
            particleSystem = new ParticleSystem(500000, canvas.width/4, canvas.width/4, 3f);

            SetupGUI();
            
            EngineLoop.fixedFrameRate = 40;
            EngineLoop.Update += Update;
            EngineLoop.FixedUpdate += FixedUpdate;
        });
    }
}