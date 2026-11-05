using System;
using System.Collections.Generic;
using UnityEngine;

// Animación por frames de Varn. GuardianBoss decide qué tocar; este componente solo avanza frames.
// Clips de fase (vista sur): Intro, Fase1_*, CambioFase, Fase2_*, Derrota.
// Clips de 8 direcciones: "<Acción>_<Dirección>" (Walk_Este, Idle_Noroeste...).
[RequireComponent(typeof(SpriteRenderer))]
public class GuardianVarnAnimator : MonoBehaviour
{
    public const string Intro = "Intro_Trono_SeLevanta_DesapareceTrono";
    public const string FistCombo = "Fase1_Punios_Combo";
    public const string FistSlam = "Fase1_Punios_GolpeSuelo";
    public const string DrawSword = "CambioFase_SacaEspadaDelPecho";
    public const string SwordHorizontal = "Fase2_Espada_CorteHorizontal";
    public const string SwordVertical = "Fase2_Espada_CorteVertical";
    public const string Defeat = "Derrota_SeArrodilla_Acepta";

    // En sentido antihorario desde la derecha, de 45 en 45 grados
    private static readonly string[] Directions = { "Este", "Noreste", "Norte", "Noroeste", "Oeste", "Suroeste", "Sur", "Sureste" };

    [Serializable]
    public class Clip
    {
        public string name;
        public Sprite[] frames;
    }

    [SerializeField] private Clip[] clips = Array.Empty<Clip>();

    private SpriteRenderer sr;
    private Dictionary<string, Sprite[]> byName;
    private Sprite[] current;
    private int from, to, frame;
    private float fps, timer;
    private bool loop;

    // Avanza aunque el juego esté detenido (la escena en que se saca la espada)
    public bool UnscaledTime { get; set; }

    // Termina la reproducción actual (los clips sin loop se quedan en su último frame).
    public bool Done => current == null || (!loop && frame >= to);
    public float Duration(string clip, float framesPerSecond) => Frames(clip).Length / framesPerSecond;

    public void SetClips(Clip[] value) => clips = value;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        byName = new Dictionary<string, Sprite[]>();
        foreach (var c in clips)
            if (c != null && c.frames != null && c.frames.Length > 0) byName[c.name] = c.frames;
    }

    public static string Direction(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        int sector = Mathf.RoundToInt(angle / 45f);
        return Directions[(sector % 8 + 8) % 8];
    }

    public void Play(string clip, float framesPerSecond, bool looping) => PlayRange(clip, 0, -1, framesPerSecond, looping);

    public void PlayFacing(string action, Vector2 dir, float framesPerSecond, bool looping) =>
        Play(action + "_" + Direction(dir), framesPerSecond, looping);

    // Reproduce los frames [first, last] del clip; last < 0 es el final.
    public void PlayRange(string clip, int first, int last, float framesPerSecond, bool looping)
    {
        var frames = Frames(clip);
        if (frames.Length == 0) return;
        if (last < 0 || last >= frames.Length) last = frames.Length - 1;
        first = Mathf.Clamp(first, 0, last);
        // Cambiar de dirección a mitad de una caminata conserva el paso
        bool keepFrame = looping && loop && current != null && current.Length == frames.Length && first == from && last == to;
        current = frames;
        from = first;
        to = last;
        fps = framesPerSecond;
        loop = looping;
        if (!keepFrame)
        {
            frame = first;
            timer = 0f;
        }
        sr.sprite = current[frame];
    }

    public void Hold(string clip, int index) => PlayRange(clip, index, index, 1f, false);

    private Sprite[] Frames(string clip)
    {
        if (byName == null) Awake();
        return byName.TryGetValue(clip, out var f) ? f : Array.Empty<Sprite>();
    }

    private void Update()
    {
        if (current == null || from == to || fps <= 0f) return;
        if (!loop && frame >= to) return;
        timer += UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float step = 1f / fps;
        while (timer >= step)
        {
            timer -= step;
            if (frame < to) frame++;
            else if (loop) frame = from;
            else break;
        }
        sr.sprite = current[frame];
    }

    // EnemyHealth apaga al jefe al morir: una copia del sprite se queda en la arena para el
    // choque de poder (PowerClash) y al final se arrodilla.
    public GuardianVarnAnimator SpawnCopy()
    {
        var copy = new GameObject("Varn_Derrota");
        copy.transform.SetPositionAndRotation(transform.position, transform.rotation);
        copy.transform.localScale = transform.lossyScale;
        var csr = copy.AddComponent<SpriteRenderer>();
        csr.sprite = sr.sprite;
        csr.sharedMaterial = sr.sharedMaterial;
        csr.sortingLayerID = sr.sortingLayerID;
        csr.sortingOrder = sr.sortingOrder;
        csr.flipX = false;
        var anim = copy.AddComponent<GuardianVarnAnimator>();
        anim.clips = clips;
        anim.Awake();
        return anim;
    }
}
