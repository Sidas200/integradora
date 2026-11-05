using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

// Lectura de mandos (Xbox, PlayStation, AYN Thor y genéricos) combinando todos los conectados.
// En la AYN Thor y otras consolas Android el mando integrado puede aparecer como más de un
// dispositivo, y Gamepad.current apunta al último que mandó datos (a veces uno que nadie toca).
// Los botones se nombran por posición: South = A / ×, East = B / ○, West = X / □, North = Y / ▲.
public static class GamepadInput
{
    private enum Button { South, East, West, North, Start, Select, LeftShoulder, RightShoulder }

    public static bool Connected => Gamepad.all.Count > 0;

    public static bool SouthPressed => AnyPressed(Button.South);
    public static bool EastPressed => AnyPressed(Button.East);
    public static bool WestPressed => AnyPressed(Button.West);
    public static bool NorthPressed => AnyPressed(Button.North);
    // Start (Xbox / Thor) u Options (PS5)
    public static bool StartPressed => AnyPressed(Button.Start);
    public static bool LeftShoulderPressed => AnyPressed(Button.LeftShoulder);
    public static bool RightShoulderPressed => AnyPressed(Button.RightShoulder);

    // Inventario: View/Back en Xbox y Thor; touchpad (o Create) en PlayStation.
    public static bool InventoryPressed
    {
        get
        {
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                if (pads[i].selectButton.wasPressedThisFrame) return true;
                if (pads[i] is DualShockGamepad ps && ps.touchpadButton.wasPressedThisFrame) return true;
            }
            return false;
        }
    }

    // Cruceta o stick izquierdo del mando que más se inclina. En cada mando la cruceta tiene prioridad.
    public static Vector2 Move
    {
        get
        {
            Vector2 best = Vector2.zero;
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                Vector2 dpad = pads[i].dpad.ReadValue();
                Vector2 value = dpad.sqrMagnitude > 0.01f ? dpad : pads[i].leftStick.ReadValue();
                if (value.sqrMagnitude > best.sqrMagnitude) best = value;
            }
            return best;
        }
    }

    public static Vector2 RightStick
    {
        get
        {
            Vector2 best = Vector2.zero;
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                Vector2 value = pads[i].rightStick.ReadValue();
                if (value.sqrMagnitude > best.sqrMagnitude) best = value;
            }
            return best;
        }
    }

    private static bool AnyPressed(Button button)
    {
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            if (Control(pads[i], button).wasPressedThisFrame) return true;
        }
        return false;
    }

    private static ButtonControl Control(Gamepad pad, Button button)
    {
        switch (button)
        {
            case Button.South: return pad.buttonSouth;
            case Button.East: return pad.buttonEast;
            case Button.West: return pad.buttonWest;
            case Button.North: return pad.buttonNorth;
            case Button.Start: return pad.startButton;
            case Button.Select: return pad.selectButton;
            case Button.LeftShoulder: return pad.leftShoulder;
            default: return pad.rightShoulder;
        }
    }
}

// Navegación de listas con repetición al mantener inclinado el stick, la cruceta o una tecla.
// Usa histéresis: se activa al pasar 0.5 y no se suelta hasta bajar de 0.3, así un stick que
// queda cerca del umbral (o algo en diagonal) no corta la repetición ni da pasos dobles.
// Step devuelve el paso del frame: x = -1 izquierda / +1 derecha, y = -1 arriba / +1 abajo.
public class NavRepeat
{
    private const float PressThreshold = 0.5f;
    private const float ReleaseThreshold = 0.3f;

    private readonly float initialDelay;
    private readonly float repeatInterval;
    private Vector2Int held;
    private float nextTime;

    public NavRepeat(float initialDelay = 0.3f, float repeatInterval = 0.12f)
    {
        this.initialDelay = initialDelay;
        this.repeatInterval = repeatInterval;
    }

    public void Reset()
    {
        held = Vector2Int.zero;
        nextTime = 0f;
    }

    // input: y positivo = arriba (como el stick).
    public Vector2Int Step(Vector2 input)
    {
        Vector2Int dir = Resolve(input);
        if (dir == Vector2Int.zero)
        {
            Reset();
            return Vector2Int.zero;
        }

        float now = Time.unscaledTime;
        if (dir != held)
        {
            // Primer paso inmediato
            held = dir;
            nextTime = now + initialDelay;
            return dir;
        }

        if (now >= nextTime)
        {
            nextTime = now + repeatInterval;
            return dir;
        }

        return Vector2Int.zero;
    }

    private Vector2Int Resolve(Vector2 v)
    {
        // Mantener la dirección actual mientras siga inclinada por encima del umbral de soltar
        if (held.y != 0 && -v.y * held.y > ReleaseThreshold && Mathf.Abs(v.y) >= Mathf.Abs(v.x) * 0.5f)
            return held;
        if (held.x != 0 && v.x * held.x > ReleaseThreshold && Mathf.Abs(v.x) >= Mathf.Abs(v.y) * 0.5f)
            return held;

        if (Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y)) < PressThreshold)
            return Vector2Int.zero;

        return Mathf.Abs(v.y) >= Mathf.Abs(v.x)
            ? new Vector2Int(0, v.y > 0f ? -1 : 1)
            : new Vector2Int(v.x > 0f ? 1 : -1, 0);
    }
}
