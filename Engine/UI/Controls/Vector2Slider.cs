using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public class Vector2Slider : UpdatableControl<Vector2f>
{
    public Vector2f min;
    public Vector2f max;
    public float step;
    protected Vector2f tempRealValue; // this value is used during sliding and is not rounded, whereas the Value is rounded to the nearest step

    protected readonly RectangleShape background = new();
    protected readonly RectangleShape xAxis = new();
    protected readonly RectangleShape yAxis = new();

    protected readonly CircleShape xSlider = new();
    protected readonly CircleShape ySlider = new();
    protected readonly CircleShape xySlider = new();

    

    public Vector2Slider(string label, Panel panel, SetValueDelegate setValue, Vector2f defaultValue, Vector2f min, Vector2f max, float step) : base(label, panel, setValue, defaultValue)
    {
        this.min = min;
        this.max = max;
        this.step = step;
        this.tempRealValue = defaultValue;
    }

    protected override void Update()
    {
        base.Update();

        var mod = Keyboard.IsKeyPressed(Keyboard.Key.LShift) ? 1 / 10f : 1;
        var delta = new Vector2f(MouseGestures.mouseDelta.X * mod, MouseGestures.mouseDelta.Y * mod);

        if (xySlider.IsBeingDragged(Mouse.Button.Left, window))
        {
            if(!isMouseCaptured)
            {
                tempRealValue = Value;
                isMouseCaptured = true;
            }

            tempRealValue = new
            (
                tempRealValue.X + (delta.X / xAxis.Size.X * (max.X - min.X)),
                tempRealValue.Y + (delta.Y / yAxis.Size.Y * (max.Y - min.Y))
            );
        }
        else if (ySlider.IsBeingDragged(Mouse.Button.Left, window))
        {
            if(!isMouseCaptured)
            {
                tempRealValue = Value;
                isMouseCaptured = true;
            }

            tempRealValue = new(tempRealValue.X, tempRealValue.Y + (delta.Y / yAxis.Size.Y * (max.Y - min.Y)));
        }
        else if (xSlider.IsBeingDragged(Mouse.Button.Left, window))
        {
            if(!isMouseCaptured)
            {
                tempRealValue = Value;
                isMouseCaptured = true;
            }

            tempRealValue = new(tempRealValue.X + (delta.X / xAxis.Size.X * (max.X - min.X)), tempRealValue.Y);
        }
        else
        {
            isMouseCaptured = false;
        }

        if (xSlider.Clicked(Mouse.Button.Right, window))
            tempRealValue = new(defaultValue.X, Value.Y);

        if (ySlider.Clicked(Mouse.Button.Right, window))
            tempRealValue = new(Value.X, defaultValue.Y);

        if (xySlider.Clicked(Mouse.Button.Right, window))
            tempRealValue = defaultValue;

        Value = new
        (
            Utils.RoundToMagnitude(MathF.Min(MathF.Max(tempRealValue.X, min.X), max.X), step), 
            Utils.RoundToMagnitude(MathF.Min(MathF.Max(tempRealValue.Y, min.Y), max.Y), step)
        );

        var xPercent = (Value.X - min.X) / (max.X - min.X);
        var yPercent = (Value.Y - min.Y) / (max.Y - min.Y);
        xSlider.Position = new Vector2f(xAxis.Position.X + xAxis.Size.X * xPercent - xSlider.Radius, xAxis.Position.Y + xAxis.Size.Y / 2 - xSlider.Radius);
        ySlider.Position = new Vector2f(yAxis.Position.X + yAxis.Size.X / 2 - ySlider.Radius, yAxis.Position.Y + yAxis.Size.Y * yPercent - ySlider.Radius);
        xySlider.Position = new Vector2f(xSlider.Position.X, ySlider.Position.Y);
    }

    public override void Draw(float y)
    {
        base.Draw(y);

        var leftText = left + Utils.CeilToMagnitude(MathF.Max(Theme.fontSize * 5 * 0.75f, labelText.GetGlobalBounds().Width), (int)(Theme.fontSize * 2));
        var rightText = right - Utils.CeilToMagnitude(Theme.fontSize * 5 * 0.75f, (int)(Theme.fontSize * 2));

        var sideLegth = Math.Min(rightText - leftText, LineHeight - Theme.padding * 2);
        var emptySpace = Math.Max(rightText - leftText, LineHeight - Theme.padding * 2) - sideLegth;

        background.Size = new Vector2f(sideLegth, sideLegth);
        background.Position = new Vector2f(leftText + emptySpace / 2, top + LineHeight / 2 - background.Size.Y / 2);
        background.FillColor = Theme.barColor;
        background.OutlineColor = Theme.accentColor;
        background.OutlineThickness = Theme.outlineThickness;
        window.Draw(background);

        xAxis.Size = new Vector2f(background.Size.X, Theme.lineThickness);
        xAxis.Position = new Vector2f(background.Position.X, background.Position.Y + background.Size.Y / 2 - xAxis.Size.Y / 2);
        xAxis.FillColor = Theme.xAxisColor;
        window.Draw(xAxis);

        yAxis.Size = new Vector2f(Theme.lineThickness, background.Size.Y);
        yAxis.Position = new Vector2f(background.Position.X + background.Size.X / 2 - yAxis.Size.X / 2, background.Position.Y);
        yAxis.FillColor = Theme.yAxisColor;
        window.Draw(yAxis);

        var xPercent = (Value.X - min.X) / (max.X - min.X);
        var yPercent = (Value.Y - min.Y) / (max.Y - min.Y);
        xSlider.Position = new Vector2f(xAxis.Position.X + xAxis.Size.X * xPercent - xSlider.Radius, xAxis.Position.Y + xAxis.Size.Y / 2 - xSlider.Radius);
        ySlider.Position = new Vector2f(yAxis.Position.X + yAxis.Size.X / 2 - ySlider.Radius, yAxis.Position.Y + yAxis.Size.Y * yPercent - ySlider.Radius);
        xySlider.Position = new Vector2f(xSlider.Position.X, ySlider.Position.Y);

        xSlider.Radius = Theme.nobSize;
        xSlider.FillColor = Theme.xAxisColor;
        window.Draw(xSlider);

        ySlider.Radius = Theme.nobSize;
        ySlider.FillColor = Theme.yAxisColor;
        window.Draw(ySlider);

        xySlider.Radius = Theme.nobSize;
        xySlider.FillColor = Theme.accentColor;
        window.Draw(xySlider);
    }
}

