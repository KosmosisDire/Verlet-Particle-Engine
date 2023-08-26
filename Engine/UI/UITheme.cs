using SFML.Graphics;

namespace ProtoGUI;

public struct UITheme
{
    public Color backgroundColor = new(35, 39, 46);
    public Color barColor = new(30, 34, 39);
    public Color fillColor = new(93, 103, 122);
    public Color strokeColor = new(44, 49, 57);
    public Color textColor = new(198, 120, 221);
    public Color textBackgroundColor = new(144, 96, 169);
    public Color accentColor = new(229, 163, 71);
    public Color xAxisColor = new(224, 108, 117);
    public Color yAxisColor = new(108, 224, 117);

    public Font font;
    public uint fontSize = 12;
    public uint lineHeight = 16;
    public float padding = 2;
    public float nobSize = 5;
    public float lineThickness = 1;
    public float outlineThickness = 1;


    public UITheme()
    {
        font = new Font("C:/Main Documents/Projects/Coding/C#/Particle Life/Engine/Resources/MPLUSRounded1c-Regular.ttf");
    }
}