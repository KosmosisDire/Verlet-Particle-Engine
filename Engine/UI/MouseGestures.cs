using SFML.System;
using SFML.Window;

namespace ProtoGUI;

public static class MouseGestures 
{
    public static Vector2f mouseDelta;
    static Vector2f mousePosition;
    static Vector2f lastPos;

    static bool leftButtonDown = false;
    static bool leftButtonUp = false;
    static float leftHold = 0;
    static bool middleButtonDown = false;
    static bool middleButtonUp = false;
    static float middleHold = 0;
    static bool rightButtonDown = false;
    static bool rightButtonUp = false;
    static float rightHold = 0;

    public static void Update(float dt)
    {
        mousePosition = (Vector2f)Mouse.GetPosition();
        mouseDelta = mousePosition - lastPos;
        lastPos = mousePosition;

        if (Mouse.IsButtonPressed(Mouse.Button.Left))
        {
            if (!leftButtonDown && leftHold == 0)
            {
                leftButtonDown = true;
            }
            else if (leftButtonDown)
            {
                leftButtonDown = false;
            }

            leftHold += dt;
        }
        else
        {
            if (leftButtonUp) leftButtonUp = false;
            if (leftHold != 0) leftButtonUp = true;
            leftHold = 0;
            leftButtonDown = false;
        }

        if (Mouse.IsButtonPressed(Mouse.Button.Middle))
        {
            if (!middleButtonDown && middleHold == 0)
            {
                middleButtonDown = true;
            }
            else if (middleButtonDown)
            {
                middleButtonDown = false;
            }

            middleHold += dt;
        }
        else
        {
            if (middleButtonUp) middleButtonUp = false;
            if (middleHold != 0) middleButtonUp = true;
            middleHold = 0;
            middleButtonDown = false;
        }

        if (Mouse.IsButtonPressed(Mouse.Button.Right))
        {
            if (!rightButtonDown && rightHold == 0)
            {
                rightButtonDown = true;
            }
            else if (rightButtonDown)
            {
                rightButtonDown = false;
            }

            rightHold += dt;
        }
        else
        {
            if (rightButtonUp) rightButtonUp = false;
            if (rightHold != 0) rightButtonUp = true;
            rightHold = 0;
            rightButtonDown = false;
        }

    }

    public static bool ButtonDown(Mouse.Button button)
    {
        switch (button)
        {
            case Mouse.Button.Left:
                return leftButtonDown;
            case Mouse.Button.Middle:
                return middleButtonDown;
            case Mouse.Button.Right:
                return rightButtonDown;
            default:
                return false;
        }
    }

    public static bool ButtonUp(Mouse.Button button)
    {
        switch (button)
        {
            case Mouse.Button.Left:
                return leftButtonUp;
            case Mouse.Button.Middle:
                return middleButtonUp;
            case Mouse.Button.Right:
                return rightButtonUp;
            default:
                return false;
        }
    }

    public static bool ButtonHeld(Mouse.Button button, float duration = 0.5f)
    {
        switch (button)
        {
            case Mouse.Button.Left:
                return leftHold > duration;
            case Mouse.Button.Middle:
                return middleHold > duration;
            case Mouse.Button.Right:
                return rightHold > duration;
            default:
                return false;
        }
    }
    

}