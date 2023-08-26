using SFML.System;
using SFML.Window;
using ProtoGUI;

namespace Engine.Rendering;

public class Camera
{
    public Vector2f centerWorld;
    public float scale;
    private Screen? viewingScreen;
    public Screen? ViewingScreen
    {
        get => viewingScreen;
        set
        {
            if(value == null) return;
            viewingScreen = value;
            if(viewingScreen.ActiveCamera != this) viewingScreen.ActiveCamera = this;
        }
    }

    public Vector2f WorldSize => (viewingScreen?.Resolution ?? new Vector2f(0,0)) * scale;
    public Rect RectBoundsWorld => new(centerWorld.ToFloat2(), WorldSize.ToFloat2()){scale = scale};

    public Camera(Vector2f center, float scale = 1, Screen? viewingScreen = null)
    {
        this.centerWorld = center;
        this.scale = scale;
        ViewingScreen = viewingScreen;
    }

    public void UpdatePanning(Mouse.Button button)
    {
        if(viewingScreen == null) return;

        if (Mouse.IsButtonPressed(button) && !GUIManager.IsMouseCapturedByUI())
        {
            centerWorld -= viewingScreen.mouseDelta * scale;
        }
    }

    public void UpdateZooming()
    {
        if(viewingScreen == null) return;
        
        //camera zooming
        if (viewingScreen.wheelDelta != 0)
        {
            scale -= viewingScreen.wheelDelta * scale * 0.1f;
        }
    }
}