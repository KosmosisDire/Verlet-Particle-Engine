using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SFML;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public class Panel 
{
    public RectangleShape topBar = new();
    public RectangleShape background = new();

    public float radius = 5;
    public float borderWidth = 1;
    public float topBarHeight = 10;

    public Vector2f position;
    public Vector2f size;

    public UITheme theme;
    readonly List<Control> controls = new();

    public Window onWindow;

    public Panel(Window window)
    {
        onWindow = window;
    }

    public void AddControl(Control control)
    {
        controls.Add(control);
        control.onWindow = onWindow;
    }


    public void UpdateRects()
    {
        if(topBar.IsBeingDragged(Mouse.Button.Left, onWindow))
        {
            position += MouseGestures.mouseDelta;
        }
        
        background.Position = new Vector2f(position.X, position.Y + topBarHeight);
        background.Size = new Vector2f(size.X, size.Y - topBarHeight);
        topBar.Position = new Vector2f(position.X, position.Y);
        topBar.Size = new Vector2f(size.X, topBarHeight);

        background.FillColor = theme.backgroundColor;
        background.OutlineColor = theme.strokeColor;
        background.OutlineThickness = borderWidth;

        topBar.FillColor = theme.barColor;
        topBar.OutlineColor = theme.strokeColor;
        topBar.OutlineThickness = borderWidth;
    }

    public Panel(Vector2f position, Vector2f size, UITheme? colorTheme = null)
    {
        theme = colorTheme ?? GUIManager.globalTheme;
        this.position = position;
        this.size = size;
        GUIManager.AddPanel(this);
        UpdateRects();
    }

    public void Draw(RenderWindow window)
    {
        window.Draw(background);
        window.Draw(topBar);

        float height = 0;
        foreach (Control control in controls)
        {
            control.Draw(window, this, height);
            height += control.height + control.verticalMargin * 2;
        }
    }

    public bool ContainsPoint(Vector2f point)
    {
        return point.X > position.X && point.X < position.X + size.X && point.Y > position.Y && point.Y < position.Y + size.Y;
    }

    public bool ContainsPoint(Vector2i point)
    {
        return point.X > position.X && point.X < position.X + size.X && point.Y > position.Y && point.Y < position.Y + size.Y;
    }

    public bool IsMouseCaptured()
    {
        if(topBar.IsBeingDragged(Mouse.Button.Left, onWindow)) return true;
        
        for (var i = 0; i < controls.Count; i++)
        {
            if(controls[i].IsMouseCaptured())
            {
                return true;
            }
        }

        return false;
    }

    public void Destroy()
    {
        GUIManager.RemovePanel(this);
    }
}