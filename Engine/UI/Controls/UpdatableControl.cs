using SFML.Graphics;
using SFML.System;

namespace ProtoGUI;

// This class is so the type with a value can be matched without having to deal with generics
public abstract class ValuedControl : Control
{
    public delegate string GetDisplayValueDelegate();

    protected object? _value;
    protected Text valueText = new();

    public GetDisplayValueDelegate? getDisplayValue;
    public bool drawValue = true;

    public float ValueTextWidth => !drawValue ? Theme.padding : MathF.Max(Theme.fontSize * 5, valueText.GetGlobalBounds().Width + Theme.fontSize * 2 + Theme.padding * 2);
    protected float MaxPanelValueWidth => panel.maxValueWidth;

    public ValuedControl(string label, Panel panel) : base(label, panel) { }
}

public class UpdatableControl<T> : ValuedControl
{
    public delegate T GetValueDelegate();
    public delegate void SetValueDelegate(T value);

    public GetValueDelegate? getValue;
    public SetValueDelegate? onSetValue;

    protected T? defaultValue;
    public T? Value
    {
        get => (T?)_value;
        set
        {
            this._value = value;
            if(value != null) onSetValue?.Invoke(value);
        }
    }

    public UpdatableControl(string label, Panel panel, GetValueDelegate setValue) : base(label, panel)
    {
        this.getValue = setValue;
        this._value = setValue();
        defaultValue = (T?)_value;
    }

    public UpdatableControl(string label, Panel panel, SetValueDelegate onSetValue, T defaultValue) : base(label, panel)
    {
        this.onSetValue = onSetValue;
        this.defaultValue = defaultValue;
        Value = defaultValue;
    }

    protected override void Update()
    {
        if (getValue != null) _value = getValue();
    }

    public override void Draw(float y)
    {
        Update();

        base.Draw(y);

        if(drawValue) 
        {
            valueText.Font = Theme.font;
            valueText.CharacterSize = Theme.fontSize;
            valueText.DisplayedString = getDisplayValue != null ? getDisplayValue() : Value?.ToString();
            valueText.Position = new Vector2f(right - valueText.GetGlobalBounds().Width, top + LineHeight / 2 - Theme.fontSize * 0.75f);
            valueText.FillColor = Theme.textColor;

            window.Draw(valueText);
        }
    }
}
