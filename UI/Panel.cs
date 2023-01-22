using System.Collections.Generic;
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

    public void AddControl(Control control)
    {
        controls.Add(control);
    }


    public void UpdateRects()
    {
        if(topBar.IsBeingDragged(Mouse.Button.Left))
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

    public void Destroy()
    {
        GUIManager.RemovePanel(this);
    }
}