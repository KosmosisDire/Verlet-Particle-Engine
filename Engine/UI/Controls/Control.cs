using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public abstract class Control
{
    protected Text labelText = new();
    public float LabelWidth => !drawLabel ? Theme.padding : MathF.Max(Theme.fontSize * 5, labelText.GetGlobalBounds().Width + Theme.fontSize * 2 + Theme.padding * 2);
    protected float MaxPanelLabelWidth => panel.maxLabelWidth;


    public string label;
    public bool drawLabel = true;
    public Vector2f margin = new(0,0);
    private UITheme? themeOverride = null;
    public UITheme Theme
    {
        get => themeOverride ?? panel.theme;
        set => themeOverride = value;
    }
    private float? lineHeightOverride = null;
    public float LineHeight
    {
        get => lineHeightOverride ?? Theme.lineHeight;
        set => lineHeightOverride = value;
    }

    protected float left;
    protected float top;
    protected float right;
    protected float bottom;
    public float TotalHeight => bottom - top;
    

    public Panel panel;
    public RenderWindow window => panel.window;

    protected bool isMouseCaptured = false;
    public bool IsMouseCaptured() => isMouseCaptured;
    

    protected Control(string label, Panel panel)
    {
        this.label = label;
        this.panel = panel;
        panel.AddControl(this);
    }

    protected abstract void Update();

    public virtual void Draw(float y)
    {
        left = panel.position.X + Theme.padding + margin.X;
        top = panel.position.Y + y + panel.topBarHeight + Theme.padding + margin.Y;
        right = panel.position.X + panel.size.X - Theme.padding - margin.X;
        bottom = top + LineHeight + Theme.padding + margin.Y;

        if(drawLabel)
        {
            labelText.Font = Theme.font;
            labelText.CharacterSize = Theme.fontSize;
            labelText.DisplayedString = label;
            labelText.Position = new Vector2f(left, top + LineHeight / 2 - Theme.fontSize * 0.75f);
            labelText.FillColor = Theme.textColor;
            window.Draw(labelText);
        }
    }
}
