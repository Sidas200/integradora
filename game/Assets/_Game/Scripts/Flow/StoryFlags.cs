using System;

// Acceso a los eventos de historia guardados en SessionState (se guardan en ProgressData.flags).
public static class StoryFlags
{
    // Se dispara al activar un flag; el HUD de objetivos lo usa para actualizarse.
    public static event Action<string> Changed;

    public static bool Has(string flag)
    {
        var s = SessionState.Instance;
        return s != null && !string.IsNullOrEmpty(flag) && s.flags.Contains(flag);
    }

    // Devuelve true solo la primera vez (útil para recompensas de una sola vez).
    public static bool Set(string flag)
    {
        var s = SessionState.Instance;
        if (s == null || string.IsNullOrEmpty(flag) || s.flags.Contains(flag)) return false;
        s.flags.Add(flag);
        Changed?.Invoke(flag);
        return true;
    }
}
