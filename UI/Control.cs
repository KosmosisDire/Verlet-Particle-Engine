using System;
using System.Collections.Generic;
using System.Numerics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public abstract class Control
{
    public string label;
    protected Text labelText = new();
    public float height;
    public float verticalMargin;

    protected float left;
    protected float top;
    protected float right;
    protected float bottom;

    public Window? onWindow;

    protected bool isMouseCaptured = false;
    public bool IsMouseCaptured() => isMouseCaptured;
    

    protected Control(string label, float height)
    {
        this.label = label;
        this.height = height;
    }

    protected abstract void Update();

    public virtual void Draw(RenderWindow window, Panel parent, float y)
    {
        left = parent.position.X + parent.theme.padding;
        top = parent.position.Y + y + parent.topBarHeight + parent.theme.padding + verticalMargin;
        right = parent.position.X + parent.size.X - parent.theme.padding;
        bottom = top + height;

        labelText.Font = parent.theme.font;
        labelText.CharacterSize = parent.theme.fontSize;
        labelText.DisplayedString = label;
        labelText.Position = new Vector2f(left, top + height / 2 - parent.theme.fontSize * 0.75f);
        labelText.FillColor = parent.theme.textColor;
        window.Draw(labelText);
    }
}

public class Label : Control
{
    // basically just an alias for Control
    public Label(string label, float height) : base(label, height)
    {
    }

    protected override void Update()
    {
    }
}

public class UpdatableControl<T> : Control
{
    public delegate T GetValueDelegate();
    public delegate void SetValueDelegate(T value);

    protected Text valueText = new();
    public GetValueDelegate? toGetValue;
    public SetValueDelegate? onSetValue;
    protected T? defaultValue;
    private object? value;
    public T? Value
    {
        get => (T?)value;
        set
        {
            this.value = value;
            if(value != null) onSetValue?.Invoke(value);
        }
    }

    public UpdatableControl(string label, float height, GetValueDelegate toGetValue) : base(label, height)
    {
        this.toGetValue = toGetValue;
        this.value = toGetValue();
        defaultValue = (T?)value;
    }

    public UpdatableControl(string label, float height, T defaultValue, SetValueDelegate onSetValue) : base(label, height)
    {
        this.onSetValue = onSetValue;
        this.defaultValue = defaultValue;
        Value = defaultValue;
    }

    protected override void Update()
    {
        if (toGetValue != null) value = toGetValue();
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        Update();

        base.Draw(window, parent, y);

        valueText.Font = parent.theme.font;
        valueText.CharacterSize = parent.theme.fontSize;
        valueText.DisplayedString = Value?.ToString();
        valueText.Position = new Vector2f(right - valueText.GetGlobalBounds().Width, top + height / 2 - parent.theme.fontSize * 0.75f);
        valueText.FillColor = parent.theme.textColor;

        window.Draw(valueText);
    }
}

public class Slider : UpdatableControl<float>
{
    public float min;
    public float max;
    public float step;
    protected float tempRealValue; // this value is used during sliding and is not rounded, whereas the Value is rounded to the nearest step

    readonly RectangleShape background = new();
    readonly CircleShape slider = new();

    public Slider(string label, float height, float defaultValue, float min, float max, float step, SetValueDelegate onSetValue) : base(label, height, defaultValue, onSetValue)
    {
        this.min = min;
        this.max = max;
        this.step = step;
        this.tempRealValue = defaultValue;
    }

    protected override void Update()
    {
        base.Update();

        if (background.IsBeingDragged(Mouse.Button.Left, onWindow) || slider.IsBeingDragged(Mouse.Button.Left, onWindow))
        {
            if(!isMouseCaptured)
            {
                tempRealValue = Value;
                isMouseCaptured = true;
            }

            var percentage = (Mouse.GetPosition(onWindow).X - background.Position.X) / background.Size.X; 
            tempRealValue = min + percentage * (max - min);
        }
        else
        {
            isMouseCaptured = false;
        }

        if (slider.Clicked(Mouse.Button.Right, onWindow))
            tempRealValue = defaultValue;
        

        Value = Utils.RoundToMagnitude(Math.Min(Math.Max(tempRealValue, min), max), step);
        var percent = (Value - min) / (max - min);
        slider.Position = new Vector2f(background.Position.X + background.Size.X * percent, background.Position.Y + background.Size.Y / 2 - slider.Radius);
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        base.Draw(window, parent, y);

        var leftText = left + Utils.CeilToMagnitude(MathF.Max(parent.theme.fontSize * 12 * 0.75f, labelText.GetGlobalBounds().Width), (int)(parent.theme.fontSize * 2));
        var rightText = right - Utils.CeilToMagnitude(parent.theme.fontSize * 12 * 0.75f, (int)(parent.theme.fontSize * 2));

        background.Size = new Vector2f(rightText - leftText, height / 3f);
        background.Position = new Vector2f(leftText, top + height / 2 - background.Size.Y / 2);
        background.FillColor = parent.theme.barColor;
        background.OutlineColor = parent.theme.strokeColor;
        background.OutlineThickness = parent.theme.outlineThickness;
        window.Draw(background);

        slider.Radius = parent.theme.nobSize;
        slider.FillColor = parent.theme.accentColor;
        var percent = (Value - min) / (max - min);
        slider.Position = new Vector2f(background.Position.X + background.Size.X * percent, background.Position.Y + background.Size.Y / 2 - slider.Radius);
        window.Draw(slider);
    }
}

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

    

    public Vector2Slider(string label, float height, Vector2f defaultValue, Vector2f min, Vector2f max, float step, SetValueDelegate setValue) : base(label, height, defaultValue, setValue)
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

        if (xySlider.IsBeingDragged(Mouse.Button.Left, onWindow))
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
        else if (ySlider.IsBeingDragged(Mouse.Button.Left, onWindow))
        {
            if(!isMouseCaptured)
            {
                tempRealValue = Value;
                isMouseCaptured = true;
            }

            tempRealValue = new(tempRealValue.X, tempRealValue.Y + (delta.Y / yAxis.Size.Y * (max.Y - min.Y)));
        }
        else if (xSlider.IsBeingDragged(Mouse.Button.Left, onWindow))
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

        if (xSlider.Clicked(Mouse.Button.Right, onWindow))
            tempRealValue = new(defaultValue.X, Value.Y);

        if (ySlider.Clicked(Mouse.Button.Right, onWindow))
            tempRealValue = new(Value.X, defaultValue.Y);

        if (xySlider.Clicked(Mouse.Button.Right, onWindow))
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

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        base.Draw(window, parent, y);

        var leftText = left + Utils.CeilToMagnitude(MathF.Max(parent.theme.fontSize * 5 * 0.75f, labelText.GetGlobalBounds().Width), (int)(parent.theme.fontSize * 2));
        var rightText = right - Utils.CeilToMagnitude(parent.theme.fontSize * 5 * 0.75f, (int)(parent.theme.fontSize * 2));

        var sideLegth = Math.Min(rightText - leftText, height - parent.theme.padding * 2);
        var emptySpace = Math.Max(rightText - leftText, height - parent.theme.padding * 2) - sideLegth;

        background.Size = new Vector2f(sideLegth, sideLegth);
        background.Position = new Vector2f(leftText + emptySpace / 2, top + height / 2 - background.Size.Y / 2);
        background.FillColor = parent.theme.barColor;
        background.OutlineColor = parent.theme.accentColor;
        background.OutlineThickness = parent.theme.outlineThickness;
        window.Draw(background);

        xAxis.Size = new Vector2f(background.Size.X, parent.theme.lineThickness);
        xAxis.Position = new Vector2f(background.Position.X, background.Position.Y + background.Size.Y / 2 - xAxis.Size.Y / 2);
        xAxis.FillColor = parent.theme.xAxisColor;
        window.Draw(xAxis);

        yAxis.Size = new Vector2f(parent.theme.lineThickness, background.Size.Y);
        yAxis.Position = new Vector2f(background.Position.X + background.Size.X / 2 - yAxis.Size.X / 2, background.Position.Y);
        yAxis.FillColor = parent.theme.yAxisColor;
        window.Draw(yAxis);

        var xPercent = (Value.X - min.X) / (max.X - min.X);
        var yPercent = (Value.Y - min.Y) / (max.Y - min.Y);
        xSlider.Position = new Vector2f(xAxis.Position.X + xAxis.Size.X * xPercent - xSlider.Radius, xAxis.Position.Y + xAxis.Size.Y / 2 - xSlider.Radius);
        ySlider.Position = new Vector2f(yAxis.Position.X + yAxis.Size.X / 2 - ySlider.Radius, yAxis.Position.Y + yAxis.Size.Y * yPercent - ySlider.Radius);
        xySlider.Position = new Vector2f(xSlider.Position.X, ySlider.Position.Y);

        xSlider.Radius = parent.theme.nobSize;
        xSlider.FillColor = parent.theme.xAxisColor;
        window.Draw(xSlider);

        ySlider.Radius = parent.theme.nobSize;
        ySlider.FillColor = parent.theme.yAxisColor;
        window.Draw(ySlider);

        xySlider.Radius = parent.theme.nobSize;
        xySlider.FillColor = parent.theme.accentColor;
        window.Draw(xySlider);
    }
}

