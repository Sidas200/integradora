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

    public static class Espada
    {
        private const string F = "Tsuki_Espada/";
        public const string QuickCut = F + "01_Espada_Corte_Rapido";
        public const string HorizontalCut = F + "02_Espada_Corte_Horizontal";
        public const string VerticalCut = F + "03_Espada_Corte_Vertical";
        public const string DoubleCombo = F + "04_Espada_Combo_Doble";
        public const string HeavyFinal = F + "05_Espada_Corte_Fuerte_Final";
        public const string HitEnemy = F + "06_Espada_Impacto_Enemigo";
        public const string HitRock = F + "07_Espada_Impacto_Roca";
        public const string HitSlime = F + "08_Espada_Impacto_Slime";
        public const string HitArmor = F + "09_Espada_Rozar_Armadura";
    }

    public static class Slime
    {
        private const string F = "Slime_Azul/";
        public const string Idle = F + "01_Slime_Burbujeo_Idle";
        public const string Move = F + "02_Slime_Movimiento_Gelatinoso";
        public const string Jump = F + "03_Slime_Salto";
        public const string Land = F + "04_Slime_Aterrizaje";
        public const string Alert = F + "05_Slime_Alerta";
        public const string Ram = F + "06_Slime_Ataque_Choque";
        public const string Hurt1 = F + "07_Slime_Recibe_Dano_1";
        public const string Hurt2 = F + "08_Slime_Recibe_Dano_2";
        public const string Death = F + "09_Slime_Muerte_Desintegracion";
    }

    public static class Distancia
    {
        private const string F = "Enemigo_Distancia/";
        public const string Idle = F + "01_Idle_Flotar_Nucleo_Rojo";
        public const string Alert = F + "02_Alerta_Detecta_Jugador";
        public const string Charge = F + "03_Cargar_Proyectil_Carmesi";
        public const string Shot1 = F + "04_Disparo_Energia_01";
        public const string Shot2 = F + "05_Disparo_Energia_02";
        public const string Shot3 = F + "06_Disparo_Energia_03";
        public const string Burst = F + "07_Rafaga_Tres_Proyectiles";
        public const string Flight = F + "08_Proyectil_En_Vuelo";
        public const string NearMiss = F + "09_Proyectil_Pasa_Cerca";
        public const string HitGround = F + "10_Impacto_Proyectil_Suelo";
        public const string HitTsuki = F + "11_Impacto_Proyectil_Tsuki";
        public const string Fade = F + "12_Proyectil_Se_Dispersa";
        public const string Hurt = F + "13_Enemigo_Recibe_Dano";
        public const string Death = F + "14_Enemigo_Muerte_Desintegracion";
        public const string Blocked = F + "15_Proyectil_Bloqueado";
        public const string Heavy = F + "16_Proyectil_Pesado";
    }

    // Golpe de la espada de Tsuki según contra qué pega. Sin daño (armadura o invulnerable) roza.
    public static string SwordImpact(Component target, bool damaged)
    {
        if (!damaged) return Espada.HitArmor;
        if (target == null) return Espada.HitEnemy;
        if (target.GetComponentInParent<SlimeController>() != null) return Espada.HitSlime;
        if (target.GetComponentInParent<FragmentoErranteController>() != null) return Espada.HitRock;
        return Espada.HitEnemy;
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
