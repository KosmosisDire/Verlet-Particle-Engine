using SFML.Graphics;

namespace ProtoGUI;

public struct UITheme
{
    public Color backgroundColor = new Color(35, 39, 46);
    public Color barColor = new Color(30, 34, 39);
    public Color fillColor = new Color(93, 103, 122);
    public Color strokeColor = new Color(44, 49, 57);
    public Color textColor = new Color(198, 120, 221);
    public Color textBackgroundColor = new Color(144, 96, 169);
    public Color accentColor = new Color(229, 163, 71);
    public Color xAxisColor = new Color(224, 108, 117);
    public Color yAxisColor = new Color(108, 224, 117);

    public Font font;
    public uint fontSize = 12;
    public float padding = 2;
    public float nobSize = 5;
    public float lineThickness = 1;
    public float outlineThickness = 1;


    public UITheme()
    {
        font = new Font("C:/Main Documents/Projects/Coding/C#/Particle Life/UI/Resources/MPLUSRounded1c-Regular.ttf");
    }

    public UITheme(Font font)
    {
        this.font = font;
    }

    public UITheme(Color backgroundColor, Color barColor, Color fillColor, Color strokeColor, Color textColor, Color textBackgroundColor, Color accentColor, Color xAxisColor, Color yAxisColor, Font font, uint fontSize, float padding, float nobSize, float lineThickness, float outlineThickness)
    {
        this.backgroundColor = backgroundColor;
        this.barColor = barColor;
        this.fillColor = fillColor;
        this.strokeColor = strokeColor;
        this.textColor = textColor;
        this.textBackgroundColor = textBackgroundColor;
        this.accentColor = accentColor;
        this.xAxisColor = xAxisColor;
        this.yAxisColor = yAxisColor;
        this.font = font;
        this.fontSize = fontSize;
        this.padding = padding;
        this.nobSize = nobSize;
        this.lineThickness = lineThickness;
        this.outlineThickness = outlineThickness;
    }
}