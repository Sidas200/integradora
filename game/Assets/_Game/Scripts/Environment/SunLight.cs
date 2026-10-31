using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Sol de exteriores:
// - Luz global cálida con una variación muy suave (el día "respira").
// - Manchas de sol grandes que derivan con el viento; entre ellas el suelo queda en la sombra de las nubes.
// - Todas las sombras apuntan al oeste (izquierda de la pantalla): las proyectadas de árboles y rocas
//   que dejó el builder se reorientan, y Tsuki y los monstruos reciben una sombra que copia su sprite.
[DefaultExecutionOrder(100)]
public class SunLight : MonoBehaviour
{
    private const string ProjectedShadowName = "ProjectedShadow";
    private static readonly Color ShadowColor = new Color(0.04f, 0.04f, 0.14f);

    private class CasterShadow
    {
        public Transform root;
        public SpriteRenderer source;
        public Transform pivot;
        public SpriteRenderer shadow;
    }

    private class Patch
    {
        public Light2D light;
        public Vector2 local;
        public float speedMul;
    }

    private OutdoorAmbience.Profile profile;
    private Light2D global;
    private float baseIntensity;
    private float seed;
    private Camera cam;
    private SkyParallax sky;
    private readonly List<Patch> patches = new List<Patch>();
    private readonly List<CasterShadow> casters = new List<CasterShadow>();
    private readonly HashSet<Transform> tracked = new HashSet<Transform>();
    private float nextScan;

    public void Setup(OutdoorAmbience.Profile p) => profile = p;

    private void Start()
    {
        cam = Camera.main;
        sky = GetComponent<SkyParallax>();
        seed = Random.value * 100f;
        global = FindGlobalLight();
        if (global != null)
        {
            if (profile.overrideAmbient)
            {
                global.color = profile.ambient;
                global.intensity = profile.intensity;
            }
            baseIntensity = global.intensity;
        }

        CreatePatches();
        OrientProjectedShadows();
        ScanCasters();
    }

    private Light2D FindGlobalLight()
    {
        foreach (var light in FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            if (light.lightType == Light2D.LightType.Global && light.gameObject.scene == gameObject.scene) return light;
        return null;
    }

    // ---------------------------------------------------------------- luz

    private void CreatePatches()
    {
        if (cam == null) return;
        Vector2 half = PatchSpan();
        Vector2 camPos = cam.transform.position;
        for (int i = 0; i < profile.sunPatches; i++)
        {
            var go = new GameObject("SunPatch_" + i);
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = profile.sunPatchColor;
            light.intensity = profile.sunPatchIntensity;
            float radius = Random.Range(1.8f, 2.8f);
            light.pointLightInnerRadius = radius * 0.3f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.75f;
            light.shadowsEnabled = false;
            var rel = new Vector2(Mathf.Lerp(-half.x, half.x, (i + Random.value) / profile.sunPatches), Random.Range(-half.y, half.y));
            patches.Add(new Patch { light = light, local = camPos + rel, speedMul = Random.Range(0.7f, 1.3f) });
        }
    }

    private Vector2 PatchSpan() => new Vector2(cam.orthographicSize * cam.aspect + 2.5f, cam.orthographicSize + 2f);

    private void Update()
    {
        if (global != null && profile.shimmer > 0f)
        {
            float n = Mathf.PerlinNoise(seed, Time.time * 0.25f) * 2f - 1f;
            global.intensity = baseIntensity * (1f + n * profile.shimmer);
        }

        if (cam != null && patches.Count > 0)
        {
            // Las manchas viajan con el viento (fijas al mundo) y dan la vuelta alrededor de la cámara
            Vector2 camPos = cam.transform.position;
            Vector2 half = PatchSpan();
            float t = Time.time;
            for (int i = 0; i < patches.Count; i++)
            {
                var p = patches[i];
                p.local.x += 0.12f * p.speedMul * Time.deltaTime;
                Vector2 rel = p.local - camPos;
                if (rel.x > half.x) p.local.x -= half.x * 2f;
                else if (rel.x < -half.x) p.local.x += half.x * 2f;
                if (rel.y > half.y) p.local.y -= half.y * 2f;
                else if (rel.y < -half.y) p.local.y += half.y * 2f;
                p.light.transform.position = new Vector3(p.local.x, p.local.y, 0f);
                float indoor = sky != null ? sky.IndoorBlend : 0f; // bajo techo no hay sol
                p.light.intensity = profile.sunPatchIntensity * (1f - indoor) * (0.8f + 0.2f * Mathf.PerlinNoise(seed + i * 7.3f, t * 0.3f));
            }
        }

        if (Time.time >= nextScan) ScanCasters();
    }

    // ---------------------------------------------------------------- sombras

    private static Quaternion ShadowRotation(float angle) => Quaternion.Euler(0f, 0f, angle);

    // Las sombras del builder ya copian el sprite con el pie como pivote: basta con girarlas y alargarlas
    private void OrientProjectedShadows()
    {
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (sr.name != ProjectedShadowName || sr.gameObject.scene != gameObject.scene) continue;
            sr.transform.localRotation = ShadowRotation(profile.shadowAngle);
            sr.transform.localScale = new Vector3(1f, profile.shadowLength, 1f);
            sr.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, profile.shadowAlpha);
        }
    }

    // Tsuki y los monstruos (también los que aparecen después: oleadas, invocaciones)
    private void ScanCasters()
    {
        nextScan = Time.time + 0.5f;
        var player = SceneContext.FindPlayer();
        if (player != null) Track(player.transform);
        foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude))
            Track(enemy.transform);
    }

    private void Track(Transform root)
    {
        if (tracked.Contains(root)) return;
        var visual = root.Find("Visual");
        var source = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (source == null) return;
        tracked.Add(root);

        var pivot = new GameObject("SunShadow_" + root.name).transform;
        pivot.SetParent(transform, false);
        var go = new GameObject("Shadow");
        go.transform.SetParent(pivot, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 8; // como las sombras de los árboles: sobre el suelo, debajo de todo lo demás
        sr.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, profile.shadowAlpha);
        casters.Add(new CasterShadow { root = root, source = source, pivot = pivot, shadow = sr });
    }

    private void LateUpdate()
    {
        for (int i = casters.Count - 1; i >= 0; i--)
        {
            var c = casters[i];
            if (c.root == null || c.source == null)
            {
                if (c.pivot != null) Destroy(c.pivot.gameObject);
                if (c.root != null) tracked.Remove(c.root);
                casters.RemoveAt(i);
                continue;
            }

            bool visible = c.source.enabled && c.source.gameObject.activeInHierarchy && c.source.sprite != null;
            c.shadow.enabled = visible;
            if (!visible) continue;

            // Pivote en los pies; el sprite se aplasta a lo alto y luego se gira hacia el oeste
            float k = profile.shadowLength;
            Vector3 offset = c.source.transform.position - c.root.position;
            Vector3 scale = c.source.transform.lossyScale;
            c.pivot.position = c.root.position;
            c.pivot.rotation = ShadowRotation(profile.shadowAngle);
            c.shadow.transform.localPosition = new Vector3(offset.x, offset.y * k, 0f);
            c.shadow.transform.localScale = new Vector3(scale.x, scale.y * k, 1f);
            c.shadow.sprite = c.source.sprite;
            c.shadow.flipX = c.source.flipX;
            var col = c.source.color;
            c.shadow.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, profile.shadowAlpha * col.a);
        }
    }
}
