using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31;
internal static class Input
{
    private static readonly HashSet<Keys> pressedKeys = new();
    private static readonly HashSet<Keys> justPressedKeys = new();
    private static readonly HashSet<Keys> justReleasedKeys = new();

    private static readonly HashSet<MouseButton> pressedButtons = new();
    private static readonly HashSet<MouseButton> justPressedButtons = new();
    private static readonly HashSet<MouseButton> justReleasedButtons = new();

    public static Vector2 MousePosition { get; private set; }
    public static bool MouseVisible 
    { 
        get
        {
            return Glfw.GetInputMode(window, InputMode.Cursor) == (int)CursorMode.Normal;
        }
        set 
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)(value ? CursorMode.Normal : CursorMode.Hidden));
        }
    }

    public static bool WindowFocused => Glfw.GetWindowAttribute(window, WindowAttribute.Focused);

    private static Window window;

    public static void Initialize(Window window)
    {
        Input.window = window;

        Glfw.SetKeyCallback(window, (nint window, Keys key, int scanCode, InputState state, ModifierKeys mods) =>
        {
            switch (state)
            {
                case InputState.Release:
                    OnKeyReleased(key);
                    break;
                case InputState.Press:
                    OnKeyPressed(key);
                    break;
                case InputState.Repeat:
                default:
                    break;
            }
        });

        Glfw.SetMouseButtonCallback(window, (nint window, MouseButton button, InputState state, ModifierKeys mods) =>
        {
            switch (state)
            {
                case InputState.Release:
                    OnMouseButtonReleased(button);
                    break;
                case InputState.Press:
                    OnMouseButtonPressed(button);
                    break;
                case InputState.Repeat:
                default:
                    break;
            }
        });
    }

    public static void Update()
    {
        Glfw.GetCursorPosition(window, out double x, out double y);
        MousePosition = new((float)x, (float)y);

        justReleasedButtons.Clear();
        justPressedButtons.Clear();

        justPressedKeys.Clear();
        justReleasedKeys.Clear();
    }

    private static void OnKeyPressed(Keys key)
    {
        pressedKeys.Add(key);
        justPressedKeys.Add(key);
    }

    private static void OnKeyReleased(Keys key)
    {
        pressedKeys.Remove(key);
        justReleasedKeys.Add(key);
    }

    public static bool IsKeyDown(Keys key)
    {
        return pressedKeys.Contains(key);
    }

    private static void OnMouseButtonReleased(MouseButton button)
    {
        pressedButtons.Remove(button);
        justReleasedButtons.Add(button);
    }

    private static void OnMouseButtonPressed(MouseButton button)
    {
        pressedButtons.Add(button);
        justPressedButtons.Add(button);
    }

    public static bool IsMouseButtonDown(MouseButton button)
    {
        return pressedButtons.Contains(button);
    }

    public static void SetMousePosition(Vector2 position)
    {
        Glfw.SetCursorPosition(window, position.X, position.Y);
    }

    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return justPressedButtons.Contains(button);
    }

    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return justReleasedButtons.Contains(button);
    }

    public static bool IsKeyPressed(Keys key)
    {
        return justPressedKeys.Contains(key);
    }

    public static bool IsKeyReleased(Keys key)
    {
        return justReleasedKeys.Contains(key);
    }
}
