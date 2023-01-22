using SFML.System;
using SFML.Window;
using ProtoGUI;

namespace Engine.Rendering;

public class Camera
{
    public Vector2f center;
    public float scale;
    Canvas canvas;


    public Vector2f Resolution => new Vector2f(canvas.width, canvas.height);
    public Vector2f Size => Resolution * scale;
    public Rect RectBounds => new Rect(center.ToFloat2(), Size.ToFloat2()){scale = scale};

    public float wheelDelta;



    public Camera(Vector2f center, Canvas activeCanvas, float scale = 1)
    {
        this.center = center;
        this.canvas = activeCanvas;
        activeCanvas.viewCamera = this;
        this.scale = scale;
        canvas.window.MouseWheelScrolled += (sender, e) => 
        {
            wheelDelta = e.Delta;
        };
    }

    // Vector2f mouseDelta;
    // Vector2f lastPos;
    bool firstMiddleMouseFrame = true;
    public void UpdatePanning(Mouse.Button button)
    {
        //camera panning
        Vector2f mousePos = (Vector2f)Mouse.GetPosition();

        if(Mouse.IsButtonPressed(button) && !MouseGestures.overUI)
        {
            // mouseDelta = mousePos - lastPos;
            center -= (MouseGestures.mouseDelta * scale);
        }
        else firstMiddleMouseFrame = true;

        //lastPos = mousePos;
    }

    public void UpdateZooming()
    {
        //camera zooming
        if (wheelDelta != 0)
        {
            scale -= wheelDelta * scale * 0.1f;
            wheelDelta = 0;
        }
    }

    public Vector2f ScreenToWorld(Vector2f screenPos)
    {
        return (screenPos * scale) + (center - Size/2);
    }

    public Vector2f ScreenToWorld(Vector2i screenPos)
    {
        return ScreenToWorld((Vector2f)screenPos);
    }

    public Vector2f WorldToScreen(Vector2f worldPos)
    {
        return (worldPos - (center - Size/2)) / scale;
    }

    public Vector2f WorldToScreen(Vector2i worldPos)
    {
        return WorldToScreen((Vector2f)worldPos);
    }
}