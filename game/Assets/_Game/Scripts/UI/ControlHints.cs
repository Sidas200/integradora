using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

// Textos de ayuda según el último dispositivo usado: teclado/mouse, Xbox o PlayStation.
// AYN Thor y cualquier mando genérico usan el esquema de Xbox.
// Revisa todos los mandos conectados (no solo Gamepad.current).
public static class ControlHints
{
    public enum Scheme { Keyboard, Xbox, PlayStation }

    // Símbolos de PlayStation que existen en la fuente del juego (LiberationSans + fallback).
    // ✕ y △ no están en la fuente y salían como cuadros.
    public const string Cross = "×";
    public const string Circle = "○";
    public const string Square = "□";
    public const string Triangle = "▲";

    private static bool initialized;
    private static Scheme scheme;
    private static int checkedFrame = -1;
    private static int padCount;
    private static Gamepad lastPad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        initialized = false;
        scheme = Scheme.Keyboard;
        checkedFrame = -1;
        padCount = 0;
        lastPad = null;
    }

    public static Scheme Current
    {
        get
        {
            DetectInput();
            return scheme;
        }
    }

    public static bool UsingGamepad => Current != Scheme.Keyboard;

    // Devuelve el texto apropiado para el dispositivo activo.
    public static string For(string keyboard, string xbox, string playstation)
    {
        switch (Current)
        {
            case Scheme.Xbox: return xbox;
            case Scheme.PlayStation: return playstation;
            default: return keyboard;
        }
    }

    // Botón o tecla de una acción del juego.
    public static string Key(string action)
    {
        switch (action)
        {
            case "move": return For("WASD / flechas", "Stick izquierdo", "Stick izquierdo");
            case "attack": return For("Clic / J", "X", Square);
            case "dash": return For("Espacio", "A", Cross);
            case "interact": return For("E", "Y", Triangle);
            case "potion": return For("Q", "RB", "R1");
            case "amrita": return For("G", "LB", "L1");
            case "inventory": return For("Tab", "View", "Touchpad");
            case "pause": return For("Esc", "Start", "Options");
            case "confirm": return For("Enter", "A", Cross);
            case "back": return For("Esc", "B", Circle);
            case "continue": return For("E / Clic", "A", Cross);
            case "skip": return For("X", "Start", "Options");
            default: return null;
        }
    }

    // Reemplaza {accion} por la tecla o botón actual, p. ej. "Pulsa {interact} para hablar".
    // Las llaves que no son una acción conocida se dejan igual.
    public static string Format(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;

        var sb = new StringBuilder(text.Length + 16);
        int i = 0;
        while (i < text.Length)
        {
            int open = text.IndexOf('{', i);
            if (open < 0) break;
            int close = text.IndexOf('}', open + 1);
            if (close < 0) break;

            sb.Append(text, i, open - i);
            string key = Key(text.Substring(open + 1, close - open - 1));
            if (key != null) sb.Append(key);
            else sb.Append(text, open, close - open + 1);
            i = close + 1;
        }
        sb.Append(text, i, text.Length - i);
        return sb.ToString();
    }

    public static bool IsPlayStation(Gamepad pad)
    {
        if (pad == null) return false;
        if (pad is DualShockGamepad) return true;

        // En Android algunos mandos de Sony llegan como mando genérico.
        var d = pad.description;
        return Contains(d.manufacturer, "Sony") ||
               Contains(d.product, "DualSense") ||
               Contains(d.product, "DualShock") ||
               Contains(d.product, "PlayStation") ||
               Contains(d.product, "Wireless Controller");
    }

    private static bool Contains(string value, string part) =>
        !string.IsNullOrEmpty(value) && value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;

    private static Scheme SchemeOf(Gamepad pad) => IsPlayStation(pad) ? Scheme.PlayStation : Scheme.Xbox;

    private static void DetectInput()
    {
        // Solo revisar una vez por frame.
        if (checkedFrame == Time.frameCount) return;
        checkedFrame = Time.frameCount;

        var pads = Gamepad.all;

        if (!initialized)
        {
            initialized = true;
            padCount = pads.Count;
            lastPad = Gamepad.current;
            scheme = lastPad != null ? SchemeOf(lastPad) : Scheme.Keyboard;
        }

        // Mando conectado o desconectado
        if (pads.Count != padCount)
        {
            bool added = pads.Count > padCount;
            padCount = pads.Count;
            if (pads.Count == 0)
            {
                lastPad = null;
                scheme = Scheme.Keyboard;
            }
            else if (added)
            {
                lastPad = pads[pads.Count - 1];
                scheme = SchemeOf(lastPad);
            }
            else if (lastPad == null || !lastPad.added)
            {
                lastPad = pads[0];
                if (scheme != Scheme.Keyboard) scheme = SchemeOf(lastPad);
            }
        }

        Gamepad usedPad = null;
        for (int p = 0; p < pads.Count && usedPad == null; p++)
        {
            var controls = pads[p].allControls;
            for (int c = 0; c < controls.Count; c++)
            {
                // Incluye botones, gatillos, cruceta y las direcciones del stick (umbral 0.5)
                if (controls[c] is ButtonControl button && button.wasPressedThisFrame)
                {
                    usedPad = pads[p];
                    break;
                }
            }
        }

        if (usedPad != null)
        {
            // Si el mando y el teclado se usan en el mismo frame gana el mando: en Android
            // algunos mandos también mandan "Escape" al pulsar B.
            lastPad = usedPad;
            scheme = SchemeOf(usedPad);
            return;
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool keyboardMouseUsed =
            (kb != null && kb.anyKey.wasPressedThisFrame) ||
            (mouse != null &&
             (mouse.delta.ReadValue().sqrMagnitude > 16f ||
              mouse.scroll.ReadValue().sqrMagnitude > 1f ||
              mouse.leftButton.wasPressedThisFrame ||
              mouse.rightButton.wasPressedThisFrame ||
              mouse.middleButton.wasPressedThisFrame));

        if (keyboardMouseUsed) scheme = Scheme.Keyboard;
    }
}
