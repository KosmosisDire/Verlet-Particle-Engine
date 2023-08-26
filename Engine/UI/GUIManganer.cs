using System.Diagnostics;
using SFML.Graphics;
using SFML.Window;

namespace ProtoGUI;

public static class GUIManager
{
    static List<Panel> panels = new List<Panel>();
    public static RenderWindow? window;
    public static UITheme globalTheme;
    static Stopwatch frameTimer = new Stopwatch();

    static GUIManager()
    {
        globalTheme = new UITheme();
        Console.WriteLine("GUI Manager initialized");
    }

    public static void Init(IntPtr existingWindowHandle)
    {
        GUIManager.window = new RenderWindow(existingWindowHandle);
        GUIManager.window.SetFramerateLimit(60);
        GUIManager.window.SetVerticalSyncEnabled(true);
    }

    public static void Init(RenderWindow window)
    {
        GUIManager.window = window;
        window.SetFramerateLimit(60);
        window.SetVerticalSyncEnabled(true);
    }

    public static void AddPanel(Panel panel)
    {
        panels.Add(panel);
    }

    public static void RemovePanel(Panel panel)
    {
        panels.Remove(panel);
    }

    /// <summary>
    /// Updates all panels and inputs and draws them to the window.
    /// </summary>
    /// <param name="dt">Delta time in seconds.</param>
    public static void Update(float dt)
    {
        MouseGestures.Update(dt);

        if(window is null)
        {
            throw new Exception("GUIManager.window is null. Did you forget to call GUIManager.Init()?");
        }

        foreach (Panel panel in panels)
        {
            panel.UpdateRects();
            panel.Draw();
        }
    }

    public static bool IsMouseOverUI()
    {
        foreach (Panel panel in panels)
        {
            if (panel.ContainsPoint(Mouse.GetPosition(window)))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsMouseCapturedByUI(bool includeHovering = true)
    {
        foreach (Panel panel in panels)
        {
            if ((panel.ContainsPoint(Mouse.GetPosition(window)) && includeHovering) || panel.IsMouseCaptured())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Updates all panels and inputs and draws them to the window. Calculates the delta time for you.
    public static void Update()
    {
        float dt = frameTimer.ElapsedMilliseconds / 1000f;
        frameTimer.Restart();

        Update(dt);
    }
}