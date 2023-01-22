using System.Diagnostics;
using System.Threading;
using SFML.Window;
using System.Collections.Generic;
using System;

namespace Engine;

public delegate void LoopEvent(float dt);

public static class EngineLoop
{
    public static LoopEvent? Update;
    public static LoopEvent? FixedUpdate;
    public static Thread updateThread;
    public static Thread fixedUpdateThread;

    static List<Action> updateActions = new List<Action>();
    static List<Action> fixedUpdateActions = new List<Action>();

    static bool running = true;

    static EngineLoop()
    {
        updateThread = new Thread(UpdateLoop);
        fixedUpdateThread = new Thread(FixedUpdateLoop);
        updateThread.Start();
        fixedUpdateThread.Start();
        fixedFrameRate = 40;
    }

    public static float MeasuredUpdateFPS { get; private set; }
    public static float MeasuredFixedUpdateFPS { get; private set; }

    public static float deltaTime { get; private set; } = 0;
    public static float maximumTimeStep { get; set; } = 0.1f;
    public static void UpdateLoop()
    {
        Context context = new Context();
        Stopwatch frameTimer = new Stopwatch();
        while (running)
        {
            frameTimer.Restart();
            Update?.Invoke(deltaTime);
            foreach (Action action in updateActions)
            {
                action();
            }
            updateActions.Clear();
            frameTimer.Stop();
            deltaTime = Math.Min(maximumTimeStep, (float)frameTimer.Elapsed.TotalSeconds);

            if(deltaTime > 0)
                MeasuredUpdateFPS = (MeasuredUpdateFPS * 0.95f) + (1.0f / deltaTime) * 0.05f;
        }
    }

    static float _fixedFrameRate;
    public static float fixedFrameRate { get => _fixedFrameRate; set {_fixedFrameRate = value; fixedDeltaTime = 1f / value;} }
    public static float fixedDeltaTime { get; private set; }

    public static void FixedUpdateLoop()
    {
        Context context = new Context();
        Stopwatch frameTimer = new Stopwatch();
        while (running)
        {
            frameTimer.Restart();
            FixedUpdate?.Invoke(fixedDeltaTime);
            foreach (Action action in fixedUpdateActions)
            {
                action();
            }
            fixedUpdateActions.Clear();
            frameTimer.Stop();

            float elapsed = (float)frameTimer.Elapsed.TotalSeconds;

            if(elapsed > 0)
                MeasuredFixedUpdateFPS = (MeasuredFixedUpdateFPS * 0.95f) + (1 / elapsed) * 0.05f;

            Thread.Sleep((int)MathF.Max(1000 * (float)(fixedDeltaTime - elapsed), 0));
        }
    }

    public static void RunInUpdateThread(Action action)
    {
        updateActions.Add(action);
    }

    public static void RunInFixedUpdateThread(Action action)
    {
        fixedUpdateActions.Add(action);
    }

    public static void Quit()
    {
        running = false;
    }
}