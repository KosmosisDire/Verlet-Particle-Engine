using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public class Toggle : UpdatableControl<bool>
{
    readonly RectangleShape background = new();
    readonly RectangleShape slider = new();

    public Toggle(string label, Panel panel, SetValueDelegate onSetValue, bool defaultValue) : base(label, panel, onSetValue, defaultValue)
    {
        drawValue = false;
    }

    protected override void Update()
    {
        base.Update();

        if (background.Clicked(Mouse.Button.Left, window))
        {
            if(!isMouseCaptured)
            {
                Value = !Value;
                isMouseCaptured = true;
            }
        }
        else
        {
            isMouseCaptured = false;
        }

        if (slider.Clicked(Mouse.Button.Right, window))
            Value = defaultValue;
    }

    public override void Draw(float y)
    {
        base.Draw(y);

        background.Size = new Vector2f(Theme.fontSize * 3, Theme.fontSize);
        background.Position = new Vector2f(panel.Right - background.Size.X - ValueTextWidth, panel.position.Y + y);
        background.FillColor = Theme.barColor;
        background.OutlineColor = Theme.strokeColor;
        background.OutlineThickness = Theme.outlineThickness;
        window.Draw(background);

        slider.Size = new Vector2f(Theme.fontSize * 1.5f, Theme.fontSize);
        slider.Position = new Vector2f(background.Position.X + (Value ? background.Size.X - slider.Size.X : 0), background.Position.Y);
        slider.FillColor = Theme.accentColor;
        slider.OutlineColor = Theme.strokeColor;
        slider.OutlineThickness = Theme.outlineThickness;
        window.Draw(slider);
    }
}
