using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public static class ProtoSFMLExtentions
{
    public static Dictionary<Shape, bool> dragging = new Dictionary<Shape, bool>();
    public static bool IsBeingDragged(this Shape shape, Mouse.Button button)
    {
        if (MouseGestures.ButtonDown(button))
        {
            Vector2i mousePos = Mouse.GetPosition();
            if (shape.GetGlobalBounds().Contains(mousePos.X, mousePos.Y))
            {
                MouseGestures.overUI = true;
                if (!dragging.ContainsKey(shape))
                {
                    dragging.Add(shape, true);
                }
                else
                {
                    dragging[shape] = true;
                }
            }
        }

        if (MouseGestures.ButtonUp(button))
        {
            MouseGestures.overUI = false;
            if (dragging.ContainsKey(shape))
            {
                dragging[shape] = false;
            }
        }

        if (dragging.ContainsKey(shape))
        {
            return dragging[shape];
        }
        else
        {
            return false;
        }
    }

    public static bool Clicked(this Shape shape, Mouse.Button button)
    {
        if (MouseGestures.ButtonDown(button))
        {
            Vector2i mousePos = Mouse.GetPosition();
            if (shape.GetGlobalBounds().Contains(mousePos.X, mousePos.Y))
            {
                return true;
            }
        }

        return false;
    }
}