using System.Diagnostics;
using System.Threading;
using SFML.Window;
using System.Collections.Generic;
using System;

namespace Engine;


public class Loop
{
    public string name { get; private set; }
    Thread loopThread;
    public event EngineLoop.LoopEvent? OnLoop;
    List<Action> threadQueue = new List<Action>();

    private int _targetFPS;
    public int targetFPS 
    {
        get => _targetFPS;
        set
        {
            _targetFPS = value;
            _deltaTime = 1f / value;
        }
    }
    private float _deltaTime;
    public float deltaTime 
    {
        get => _deltaTime;
        set
        {
            _deltaTime = value;
            _targetFPS = (int)(1f / value);
        }
    }

    public float measuredFPS { get; private set; }

    public bool running { get; private set; } = false;
    private bool aborted = false;
    private int stepCountdown = 0;

    public Loop(string name, int targetFPS, bool start = true)
    {
        this.name = name;
        this.targetFPS = targetFPS;
        this.OnLoop = null;

        loopThread = new Thread(LoopThread);
        loopThread.Start();

        if(start) Run();
    }

    void LoopThread()
    {
        Console.WriteLine($"Loop '{name}' initialized");

        Context context = new Context();
        Stopwatch frameTimer = new Stopwatch();

        while (!aborted)
        {
            while (running)
            {
                frameTimer.Restart();

                ThreadStep();

                frameTimer.Stop();
                var frameTime = (float)frameTimer.ElapsedTicks / 10000 / 1000; // Convert ticks to seconds
                if (frameTime > 0)
                    measuredFPS = (measuredFPS * 0.95f) + 1.0f / frameTime * 0.05f;
                
                var waitTime = Math.Max(0, deltaTime - frameTime);
                Thread.Sleep((int)(waitTime * 1000));
            }

            if (stepCountdown > 0)
            {
                stepCountdown--;
                ThreadStep();
            }
            else
            {
                Thread.Sleep(10);
            }
        }

        Console.WriteLine($"Loop '{name}' finished");
    }

    void ThreadStep()
    {
        lock(threadQueue)
        {
            foreach (Action action in threadQueue)
            {
                action();
            }
        }
        threadQueue.Clear();
        OnLoop?.Invoke(1/measuredFPS);
    }

    public void Abort()
    {
        running = false;
        aborted = true;
    }

    public void Pause()
    {
        running = false;
    }

    public void Run()
    {
        running = true;
    }

    public void Step()
    {
        stepCountdown = 1;
    }

    public void Step(int steps)
    {
        stepCountdown = steps;
    }

    public void Connect(EngineLoop.LoopEvent onLoop)
    {
        OnLoop += onLoop;
    }

    public void RunAction(Action action)
    {
        lock(threadQueue)
        {
            threadQueue.Add(action);
        }
    }

    public void RunActionSync(Action action)
    {
        lock(threadQueue)
        {
            threadQueue.Add(action);
        }

        Step();

        while (threadQueue.Count > 0)
        {
            Thread.Sleep(1);
        }
    }
}


public static class EngineLoop
{
    public delegate void LoopEvent(float dt);



    static Dictionary<string, Loop> loops = new Dictionary<string, Loop>();

    public static Loop? GetLoop(string loopName)
    {
        if (loops.ContainsKey(loopName))
            return loops[loopName];
        return null;
    }

    public static void ConnectLoop(string loopName, LoopEvent onLoop)
    {
        if (loops.ContainsKey(loopName))
        {
            loops[loopName].Connect(onLoop);
        }
        else
        {
            throw new Exception($"Loop '{loopName}' does not exist");
        }
    }

    public static void RunActionOnLoop(string loopName, Action action)
    {
        if (loops.ContainsKey(loopName))
            loops[loopName].RunAction(action);
        else
        {
            throw new Exception($"Loop '{loopName}' does not exist");
        }
    }

    public static Loop CreateLoop(string loopName, int targetFPS, bool start = true)
    {
        Console.WriteLine($"Adding loop '{loopName}'");

        if (loops.ContainsKey(loopName))
        {
            throw new Exception($"Loop '{loopName}' already exists");
        }

        Loop loop = new Loop(loopName, targetFPS, start);
        loops.Add(loopName, loop);

        return loop;
    }

    public static void RemoveLoop(string loopName)
    {
        if (loops.ContainsKey(loopName))
        {
            loops[loopName].Abort();
            loops.Remove(loopName);
        }
    }

    public static void RemoveLoop(Loop loop)
    {
        RemoveLoop(loop.name);
    }

    public static void StopAllLoops()
    {
        foreach (Loop loop in loops.Values)
        {
            loop.Abort();
        }
    }

    public static void RunAll()
    {
        foreach (Loop loop in loops.Values)
        {
            loop.Run();
        }
    }

    public static void StepAll()
    {
        foreach (Loop loop in loops.Values)
        {
            loop.Step();
        }
    }

    public static void StepAll(int steps)
    {
        foreach (Loop loop in loops.Values)
        {
            loop.Step(steps);
        }
    }

    // public static LoopEvent? Update;
    // public static LoopEvent? FixedUpdate;
    // public static LoopEvent? Draw;
    // public static Thread updateThread;
    // public static Thread fixedUpdateThread;
    // public static Thread drawThread;

    // static List<Action> updateActions = new List<Action>();
    // static List<Action> fixedUpdateActions = new List<Action>();
    // static List<Action> drawActions = new List<Action>();

    // static bool running = true;

    // static EngineLoop()
    // {
    //     updateThread = new Thread(UpdateLoop);
    //     fixedUpdateThread = new Thread(FixedUpdateLoop);
    //     drawThread = new Thread(DrawLoop);
    //     updateThread.Start();
    //     fixedUpdateThread.Start();
    //     drawThread.Start();
    //     fixedFrameRate = 40;
    // }

    // public static float MeasuredUpdateFPS { get; private set; }
    // public static float MeasuredFixedUpdateFPS { get; private set; }
    // public static float MeasuredDrawFPS { get; private set; }

    // public static float deltaTime { get; private set; } = 0;
    // public static float maximumTimeStep { get; set; } = 0.1f;
    // public static void UpdateLoop()
    // {
    //     Context context = new Context();
    //     Stopwatch frameTimer = new Stopwatch();
    //     while (running)
    //     {
    //         frameTimer.Restart();
    //         Update?.Invoke(deltaTime);
    //         foreach (Action action in updateActions)
    //         {
    //             action();
    //         }
    //         updateActions.Clear();
    //         frameTimer.Stop();
    //         deltaTime = Math.Min(maximumTimeStep, (float)frameTimer.Elapsed.TotalSeconds);

    //         if(deltaTime > 0)
    //             MeasuredUpdateFPS = (MeasuredUpdateFPS * 0.95f) + (1.0f / deltaTime) * 0.05f;
    //     }
    // }

    // static float _fixedFrameRate;
    // public static float fixedFrameRate { get => _fixedFrameRate; set {_fixedFrameRate = value; fixedDeltaTime = 1f / value;} }
    // public static float fixedDeltaTime { get; private set; }

    // public static void FixedUpdateLoop()
    // {
    //     Context context = new Context();
    //     Stopwatch frameTimer = new Stopwatch();
    //     while (running)
    //     {
    //         frameTimer.Restart();
    //         FixedUpdate?.Invoke(fixedDeltaTime);
    //         foreach (Action action in fixedUpdateActions)
    //         {
    //             action();
    //         }
    //         fixedUpdateActions.Clear();
    //         frameTimer.Stop();

    //         float elapsed = (float)frameTimer.Elapsed.TotalSeconds;

    //         if(elapsed > 0)
    //             MeasuredFixedUpdateFPS = (MeasuredFixedUpdateFPS * 0.95f) + (1 / elapsed) * 0.05f;

    //         Thread.Sleep((int)MathF.Max(1000 * (float)(fixedDeltaTime - elapsed), 0));
    //     }
    // }

    // public static void DrawLoop()
    // {
    //     Context context = new Context();
    //     Stopwatch frameTimer = new Stopwatch();
    //     while (running)
    //     {
    //         frameTimer.Restart();
    //         Draw?.Invoke(0);
    //         foreach (Action action in drawActions)
    //         {
    //             action();
    //         }
    //         drawActions.Clear();
    //         frameTimer.Stop();

    //         float elapsed = (float)frameTimer.Elapsed.TotalSeconds;

    //         if(elapsed > 0)
    //             MeasuredDrawFPS = (MeasuredDrawFPS * 0.95f) + (1 / elapsed) * 0.05f;
    //     }
    // }

    // public static void RunInUpdateThread(Action action)
    // {
    //     updateActions.Add(action);
    // }

    // public static void RunInFixedUpdateThread(Action action)
    // {
    //     fixedUpdateActions.Add(action);
    // }

    // public static void RunInDrawThread(Action action)
    // {
    //     drawActions.Add(action);
    // }

    // public static void Quit()
    // {
    //     running = false;
    // }
}