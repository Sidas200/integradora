using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ambiente de exteriores: cielo pixelado con nubes en parallax (SkyParallax) y sol con sombras hacia
// el oeste (SunLight). Se instala solo al cargar una escena que tenga la raíz "Clouds" (la marca de
// exterior que deja el builder del capítulo), así no hay que tocar las escenas editadas a mano.
public static class OutdoorAmbience
{
    public const string OutdoorMarker = "Clouds";

    [Serializable]
    public struct Profile
    {
        [Header("Cielo")]
        public Color skyTint;
        public Color cloudTint;

        [Header("Luz global")]
        [Tooltip("Si es falso se respeta la luz de la escena (p. ej. la arena del guardián).")]
        public bool overrideAmbient;
        public Color ambient;
        public float intensity;
        [Tooltip("Variación suave de la intensidad (0.05 = ±5%).")]
        public float shimmer;

        [Header("Manchas de sol que recorren el suelo (los huecos se leen como sombra de nubes)")]
        public int sunPatches;
        public Color sunPatchColor;
        public float sunPatchIntensity;

        [Header("Sombras")]
        [Tooltip("Ángulo de la sombra en grados (90 = hacia la izquierda de la pantalla, el oeste).")]
        public float shadowAngle;
        [Tooltip("Largo de la sombra respecto a la altura del objeto.")]
        public float shadowLength;
        public float shadowAlpha;
    }

    // Mediodía soleado
    public static readonly Profile Sunny = new Profile
    {
        skyTint = Color.white,
        cloudTint = Color.white,
        overrideAmbient = true,
        ambient = new Color(1f, 0.96f, 0.86f),
        intensity = 0.92f,
        shimmer = 0.04f,
        sunPatches = 5,
        sunPatchColor = new Color(1f, 0.9f, 0.68f),
        sunPatchIntensity = 0.32f,
        shadowAngle = 94f,
        shadowLength = 0.55f,
        shadowAlpha = 0.3f,
    };

    // Tarde dorada (Bosque Quebrado)
    public static readonly Profile Golden = new Profile
    {
        skyTint = new Color(1f, 0.88f, 0.8f),
        cloudTint = new Color(1f, 0.9f, 0.82f),
        overrideAmbient = true,
        ambient = new Color(1f, 0.85f, 0.68f),
        intensity = 0.86f,
        shimmer = 0.05f,
        sunPatches = 4,
        sunPatchColor = new Color(1f, 0.76f, 0.46f),
        sunPatchIntensity = 0.3f,
        shadowAngle = 96f,
        shadowLength = 0.75f,
        shadowAlpha = 0.32f,
    };

    // Arena del guardián: se conserva su luz dramática; solo cambian cielo y sombras
    public static readonly Profile Dramatic = new Profile
    {
        skyTint = new Color(0.55f, 0.5f, 0.78f),
        cloudTint = new Color(0.62f, 0.56f, 0.8f),
        overrideAmbient = false,
        shimmer = 0f,
        sunPatches = 0,
        shadowAngle = 96f,
        shadowLength = 0.7f,
        shadowAlpha = 0.22f,
    };

    public static Profile For(string sceneName)
    {
        switch (sceneName)
        {
            case "05_CombatZone": return Golden;
            case "06_GuardianArena": return Dramatic;
            default: return Sunny;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject marker = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == OutdoorMarker) { marker = root; break; }
        if (marker == null) return;

        // Las nubes de relleno del builder se reemplazan por las nuevas
        marker.SetActive(false);

        var profile = For(scene.name);
        var go = new GameObject("OutdoorAmbience");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<SkyParallax>().Setup(profile);
        go.AddComponent<SunLight>().Setup(profile);
    }
}
