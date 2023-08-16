using System.Runtime.InteropServices;
using System.ComponentModel;

internal class HiPerfTimer
{
    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceCounter(
        out long lpPerformanceCount);

    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(
        out long lpFrequency);

    private long startTime, stopTime;
    private long freq;
    public bool isRunning = false;

    // Constructor
    public HiPerfTimer()
    {
        startTime = 0;
        stopTime  = 0;

        if (QueryPerformanceFrequency(out freq) == false)
        {
            // high-performance counter not supported
            throw new Win32Exception();
        }
    }

    // Start the timer
    public void Start()
    {
        isRunning = true;
        // let's do the waiting threads there work
        Thread.Sleep(0);

        QueryPerformanceCounter(out startTime);
    }

    // Stop the timer
    public void Stop()
    {
        isRunning = false;
        QueryPerformanceCounter(out stopTime);
    }

    public void Reset()
    {
        startTime = 0;
        stopTime  = 0;
    }

    // Returns the duration of the timer (in seconds)
    public double Duration
    {
        get
        {
            return (double)(stopTime - startTime) / (double) freq;
        }
    }

    public float ElapsedMilliseconds => (float)(Duration * 1000);


}
