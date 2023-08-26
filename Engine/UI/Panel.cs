using Engine.Rendering;
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
    public float topBarHeight = 15;

    public Vector2f position;
    public Vector2f size;
    public float Left => position.X;
    public float Right => position.X + size.X;
    public float Top => position.Y;
    public float Bottom => position.Y + size.Y;

    public float maxLabelWidth = 0;
    public float maxValueWidth = 0;

    public UITheme theme;
    readonly List<Control> controls = new();

    public RenderWindow window;

    public Panel(Vector2f position, float width, Screen screen, UITheme? colorTheme = null)
    {
        theme = colorTheme ?? GUIManager.globalTheme;
        window = screen.window;
        size = new(width, 0);
        this.position = position;
        
        GUIManager.AddPanel(this);
        UpdateRects();
    }

    public void AddControl(Control control)
    {
        controls.Add(control);
    }

    public void UpdateRects()
    {
        if(topBar.IsBeingDragged(Mouse.Button.Left, window))
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

    public void Draw()
    {
        window.Draw(background);
        window.Draw(topBar);

        float yOffset = 0;
        float maxLabelWidthTemp = 0;
        float maxValueWidthTemp = 0;
        
        foreach (Control control in controls)
        {
            control.Draw(yOffset);
            yOffset += control.TotalHeight;

            if (control.LabelWidth > maxLabelWidthTemp && control is not Label) maxLabelWidthTemp = control.LabelWidth;
            if (control is ValuedControl valuedControl && valuedControl.ValueTextWidth > maxValueWidthTemp) maxValueWidthTemp = valuedControl.ValueTextWidth;
        }

        maxLabelWidth = maxLabelWidthTemp;
        maxValueWidth = maxValueWidthTemp;
        size.Y = yOffset + topBarHeight;
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
        if(topBar.IsBeingDragged(Mouse.Button.Left, window)) return true;
        
        for (int i = 0; i < controls.Count; i++)
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