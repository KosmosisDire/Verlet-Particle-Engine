using System.Diagnostics;
using SFML.Window;

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
}