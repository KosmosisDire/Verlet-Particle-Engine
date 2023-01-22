using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public abstract class Control
{
    public string label;
    protected Text labelText = new Text();
    public float height;
    public float verticalMargin;

    protected float left;
    protected float top;
    protected float right;
    protected float bottom;

    public delegate void VoidEvent();
    public VoidEvent? OnDraw;

    protected Control(string label, float height)
    {
        this.label = label;
        this.height = height;
    }

    protected abstract void Update();

    public virtual void Draw(RenderWindow window, Panel parent, float y)
    {
        OnDraw?.Invoke();
        left = parent.position.X + parent.theme.padding;
        top = parent.position.Y + y + parent.topBarHeight + parent.theme.padding + verticalMargin;
        right = parent.position.X + parent.size.X - parent.theme.padding;
        bottom = top + height;

        labelText.Font = parent.theme.font;
        labelText.CharacterSize = parent.theme.fontSize;
        labelText.DisplayedString = label;
        labelText.Position = new Vector2f(left, top + height/2 - parent.theme.fontSize * 0.75f);
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

public abstract class UpdatableControl : Control
{
    readonly List<Action?> OnValueChangedActions = new List<Action?>();
    readonly List<Action?> OnBeforeDrawActions = new List<Action?>();

    protected Text valueText = new Text();
    public int decimalPlaces = 3;

    public UpdatableControl(string label, float height) : base(label, height)
    {
    }

    public UpdatableControl(string label, float height, Action OnValueChanged) : base(label, height)
    {
        this.OnValueChangedActions.Add(OnValueChanged);
    }

    public void ConnectValueChanged(Action OnValueChanged)
    {
        this.OnValueChangedActions.Add(OnValueChanged);
    }

    public void ConnectBeforeDraw(Action OnBeforeDraw)
    {
        this.OnBeforeDrawActions.Add(OnBeforeDraw);
    }

    protected override void Update()
    {
        foreach (Action? action in OnValueChangedActions)
        {
            action?.Invoke();
        }
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        foreach (Action? action in OnBeforeDrawActions)
        {
            action?.Invoke();
        }
        base.Draw(window, parent, y);
    }
}

// float
public class FloatDisplay : UpdatableControl
{
    public float value;
    public float defaultValue;

    public void SetValue(float value)
    {
        this.value = value;
        Update();
    }

    public FloatDisplay(string label, float height, float defaultValue = 0) : base(label, height)
    {
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }

    public FloatDisplay(string label, float height, Action OnValueChanged, float defaultValue = 0) : base(label, height, OnValueChanged)
    {
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        base.Draw(window, parent, y);
        
        valueText.Font = parent.theme.font;
        valueText.CharacterSize = parent.theme.fontSize;
        valueText.DisplayedString = value.ToString("G" + decimalPlaces);
        valueText.Position = new Vector2f(right - valueText.GetGlobalBounds().Width, top + height/2 - parent.theme.fontSize * 0.75f);
        valueText.FillColor = parent.theme.textColor;
        window.Draw(valueText);
    }
}

public class Slider : FloatDisplay
{
    public float min;
    public float max;
    public float step;

    RectangleShape background = new RectangleShape();
    CircleShape slider = new CircleShape();


    public Slider (string label, float height, float defaultValue, float min, float max, float step = 0.1f) : base(label, height, defaultValue)
    {
        this.min = min;
        this.max = max;
        this.step = step;
    }

    protected override void Update()
    {
        if(slider.IsBeingDragged(Mouse.Button.Left))
        {
            float mod = Keyboard.IsKeyPressed(Keyboard.Key.LShift) ? 1/10f : 1;
            slider.Position += new Vector2f(MouseGestures.mouseDelta.X * mod, 0);
            value = (slider.Position.X - background.Position.X) / background.Size.X * (max - min) + min;
        }

        if(slider.Clicked(Mouse.Button.Right))
        {
            value = defaultValue;
        }

        value = Math.Min(Math.Max(value, min), max);
        float percent = (value - min) / (max - min);
        slider.Position = new Vector2f(background.Position.X + background.Size.X * percent, background.Position.Y + background.Size.Y / 2 - slider.Radius);

        base.Update();
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        Update();

        base.Draw(window, parent, y);

        float leftText = left + Utils.CeilToMagnitude(MathF.Max(parent.theme.fontSize * decimalPlaces * 0.75f, labelText.GetGlobalBounds().Width), (int)(parent.theme.fontSize * 2));
        float rightText = right - Utils.CeilToMagnitude(parent.theme.fontSize * decimalPlaces * 0.75f, (int)(parent.theme.fontSize * 2));

        background.Size = new Vector2f(rightText - leftText, height/3f);
        background.Position = new Vector2f(leftText, top + height / 2 - background.Size.Y / 2);
        background.FillColor = parent.theme.barColor;
        background.OutlineColor = parent.theme.strokeColor;
        background.OutlineThickness = parent.theme.outlineThickness;
        window.Draw(background);

        slider.Radius = parent.theme.nobSize;
        slider.FillColor = parent.theme.accentColor;
        window.Draw(slider);
    }
}

//Vector2 
public class Vector2Display : UpdatableControl
{
    public Vector2f value;

    public Vector2Display(string label, float height) : base(label, height)
    {
    }

    public void SetValue(Vector2f value)
    {
        this.value = value;
        this.Update();
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        base.Draw(window, parent, y);
        
        valueText.Font = parent.theme.font;
        valueText.CharacterSize = parent.theme.fontSize;
        valueText.DisplayedString = "X: " + value.X.ToString("G" + decimalPlaces) + " \nY: " + value.Y.ToString("G" + decimalPlaces);
        valueText.Position = new Vector2f(right - valueText.GetGlobalBounds().Width, top + height/2 - parent.theme.fontSize * 0.75f * 2);
        valueText.FillColor = parent.theme.textColor;
        window.Draw(valueText);
    }
}

public abstract class EditableVector2Control : Vector2Display
{
    public delegate void Vector2Changed(Vector2f newValue);

    public Vector2f defaultValue;
    public event Vector2Changed? OnValueChanged;

    protected EditableVector2Control(string label, float height, Vector2f defaultValue) : base(label, height)
    {
        this.value = defaultValue;
        this.defaultValue = defaultValue;
    }

    Vector2f lastValue;
    protected override void Update()
    {
        base.Update();
        if (value != lastValue)
        {
            OnValueChanged?.Invoke(value);
            lastValue = value;
        }
    }
}

public class Vector2Slider : EditableVector2Control
{
    public Vector2f min;
    public Vector2f max;
    public float step;

    RectangleShape background = new RectangleShape();
    RectangleShape xAxis = new RectangleShape();
    RectangleShape yAxis = new RectangleShape();

    CircleShape xSlider = new CircleShape();
    CircleShape ySlider = new CircleShape();
    CircleShape xySlider = new CircleShape(); 

    public Vector2Slider(string label, float height, Vector2f defaultValue, Vector2f min, Vector2f max, float step = 0.1f) : base(label, height, defaultValue)
    {
        this.min = min;
        this.max = max;
        this.step = step;
    }

    protected override void Update()
    {
        float mod = Keyboard.IsKeyPressed(Keyboard.Key.LShift) ? 1/10f : 1;
        if(xSlider.IsBeingDragged(Mouse.Button.Left))
        {
            xSlider.Position += new Vector2f(MouseGestures.mouseDelta.X * mod, 0);
            value.X = (xSlider.Position.X + xSlider.Radius - xAxis.Position.X) / xAxis.Size.X * (max.X - min.X) + min.X;
        }
        if(ySlider.IsBeingDragged(Mouse.Button.Left))
        {
            ySlider.Position += new Vector2f(0, MouseGestures.mouseDelta.Y * mod);
            value.Y = (ySlider.Position.Y + ySlider.Radius - yAxis.Position.Y) / yAxis.Size.Y * (max.Y - min.Y) + min.Y;
        }
        if(xySlider.IsBeingDragged(Mouse.Button.Left))
        {
            xySlider.Position += new Vector2f(MouseGestures.mouseDelta.X * mod, MouseGestures.mouseDelta.Y * mod);
            value.X = (xySlider.Position.X + xSlider.Radius - xAxis.Position.X) / xAxis.Size.X * (max.X - min.X) + min.X;
            value.Y = (xySlider.Position.Y + ySlider.Radius - yAxis.Position.Y) / yAxis.Size.Y * (max.Y - min.Y) + min.Y;
        }

        if(xSlider.Clicked(Mouse.Button.Right))
            value.X = defaultValue.X;
        
        if(ySlider.Clicked(Mouse.Button.Right))
            value.Y = defaultValue.Y;
        
        if(xySlider.Clicked(Mouse.Button.Right))
            value = defaultValue;

        value.X = Math.Min(Math.Max(value.X, min.X), max.X);
        value.Y = Math.Min(Math.Max(value.Y, min.Y), max.Y);
        float xPercent = (value.X - min.X) / (max.X - min.X);
        float yPercent = (value.Y - min.Y) / (max.Y - min.Y);
        xSlider.Position = new Vector2f(xAxis.Position.X + xAxis.Size.X * xPercent - xSlider.Radius, xAxis.Position.Y + xAxis.Size.Y / 2 - xSlider.Radius);
        ySlider.Position = new Vector2f(yAxis.Position.X + yAxis.Size.X / 2 - ySlider.Radius, yAxis.Position.Y + yAxis.Size.Y * yPercent - ySlider.Radius);
        xySlider.Position = new Vector2f(xSlider.Position.X, ySlider.Position.Y);

        base.Update();
    }

    public override void Draw(RenderWindow window, Panel parent, float y)
    {
        Update();
        base.Draw(window, parent, y);

        float leftText = left + Utils.CeilToMagnitude(MathF.Max(parent.theme.fontSize * (decimalPlaces + 1) * 0.75f, labelText.GetGlobalBounds().Width), (int)(parent.theme.fontSize * 2));
        float rightText = right - Utils.CeilToMagnitude(parent.theme.fontSize * (decimalPlaces + 1) * 0.75f, (int)(parent.theme.fontSize * 2));

        float sideLegth = Math.Min(rightText - leftText, height - parent.theme.padding * 2);
        float emptySpace = Math.Max(rightText - leftText, height - parent.theme.padding * 2) - sideLegth;
        
        background.Size = new Vector2f(sideLegth, sideLegth);
        background.Position = new Vector2f(leftText + emptySpace/2, top + height / 2 - background.Size.Y / 2);
        background.FillColor = parent.theme.barColor;
        background.OutlineColor = parent.theme.accentColor;
        background.OutlineThickness = parent.theme.outlineThickness;
        window.Draw(background);
        
        xAxis.Size = new Vector2f(background.Size.X, parent.theme.lineThickness);
        xAxis.Position = new Vector2f(background.Position.X, background.Position.Y + background.Size.Y / 2 - xAxis.Size.Y / 2);
        xAxis.FillColor = parent.theme.xAxisColor;
        window.Draw(xAxis);
        
        yAxis.Size = new Vector2f(parent.theme.lineThickness, background.Size.Y);
        yAxis.Position = new Vector2f(background.Position.X + background.Size.X/2 - yAxis.Size.X / 2, background.Position.Y);
        yAxis.FillColor = parent.theme.yAxisColor;
        window.Draw(yAxis);
        
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