using System.Collections.Generic;
using UnityEngine;

// Efectos de sonido cortos del combate (2D). Los clips viven en Resources/Audio/SFX y se cargan
// por nombre la primera vez; si falta uno, simplemente no suena.
// Cada efecto varía un poco el tono (0.95-1.05) para que no se oiga repetido.
public static class Sfx
{
    private const string Folder = "Audio/SFX/";
    private const int Voices = 10;
    private const float Volume = 0.45f;   // ~ -7 dB, lo que sugiere el paquete

    public const string PlayerHit = "Golpe_Recibido_Jugador";

    public static class Lobo
    {
        private const string F = "Lobo_del_Velo/";
        public const string SwingMiss = F + "01_Zarpazo_Aire";
        public const string Bite = F + "02_Mordida_Impacto";
        public const string ClawHit = F + "03_Zarpazo_Impacto";
        public const string Leap = F + "04_Salto_Ataque";
        public const string Howl = F + "05_Rugido_Alerta";
        public const string Hurt = F + "06_Lobo_Recibe_Dano";
        public const string BiteAlt = F + "07_Mordida_Alternativa";
    }

    public static class Fragmento
    {
        private const string F = "Fragmento_Errante/";
        public const string RockHeavy = F + "01_Golpe_Roca_Pesado";
        public const string RockDry = F + "02_Golpe_Roca_Seco";
        public const string RockHuge = F + "03_Golpe_Roca_Enorme";
        public const string Charge = F + "04_Cargar_Onda_De_Choque";
        public const string Wave = F + "05_Explosion_Onda_De_Choque";
        public const string Hurt = F + "06_Fragmento_Recibe_Dano";
        public const string Crumble = F + "07_Fragmento_Se_Desmorona";
    }

    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private static AudioSource[] sources;
    private static int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        clips.Clear();
        sources = null;
        next = 0;
    }

    public static void Play(string name, float volume = 1f)
    {
        var clip = Load(name);
        if (clip == null) return;
        var s = NextSource();
        s.clip = clip;
        s.volume = Volume * volume;
        s.pitch = Random.Range(0.95f, 1.05f);
        s.Play();
    }

    // Uno al azar de varios (variantes del mismo golpe)
    public static void PlayAny(params string[] names)
    {
        if (names.Length > 0) Play(names[Random.Range(0, names.Length)]);
    }

    private static AudioClip Load(string name)
    {
        if (!clips.TryGetValue(name, out var clip))
            clips[name] = clip = Resources.Load<AudioClip>(Folder + name);
        return clip;
    }

    // Varias fuentes en rueda para que un sonido no corte al anterior
    private static AudioSource NextSource()
    {
        if (sources == null || sources[0] == null)
        {
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                sources[i] = go.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }
        }
        next = (next + 1) % Voices;
        return sources[next];
    }
}
