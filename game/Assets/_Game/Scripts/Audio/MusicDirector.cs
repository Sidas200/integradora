using UnityEngine;
using UnityEngine.SceneManagement;

// Música del juego. Elige la pista según la escena y la pelea, y cambia con un fundido:
// - Menú principal: intro, hasta dar Nueva partida o Continuar.
// - Zonas de juego: normal; cuando hay enemigos cerca, batalla, y al vencer al último vuelve la normal.
// - Pelea con Varn: la del jefe si existe (music_boss), si no la de batalla.
// - Escenas sin juego (intro de la historia, cierre de la demo): en silencio por ahora.
// Las pistas viven en Resources/Audio/Music; si falta una, esa parte queda en silencio.
public class MusicDirector : MonoBehaviour
{
    public const string MenuScene = "MainMenu";
    private const string Folder = "Audio/Music/";

    [SerializeField] private float volume = 0.6f;
    [SerializeField] private float fadeTime = 1.2f;
    [Tooltip("Distancia a la que un enemigo vivo cuenta como pelea.")]
    [SerializeField] private float combatRange = 3.5f;
    [Tooltip("Segundos sin enemigos cerca antes de volver a la música normal.")]
    [SerializeField] private float calmDelay = 1.5f;

    public static MusicDirector Instance { get; private set; }

    private AudioSource[] sources;
    private int active;
    private AudioClip intro, normal, battle, boss;
    private float nextCheck, lastCombatTime = -100f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("MusicDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        intro = Resources.Load<AudioClip>(Folder + "music_intro");
        normal = Resources.Load<AudioClip>(Folder + "music_normal");
        battle = Resources.Load<AudioClip>(Folder + "music_battle");
        boss = Resources.Load<AudioClip>(Folder + "music_boss");

        // Dos fuentes para pasar de una pista a otra con fundido cruzado
        sources = new AudioSource[2];
        for (int i = 0; i < sources.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
            s.ignoreListenerPause = true;
            sources[i] = s;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Se decide 4 veces por segundo; los diálogos pausan el tiempo, por eso todo es unscaled
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + 0.25f;
            Play(Desired());
        }
        Fade();
    }

    private AudioClip Desired()
    {
        if (SceneManager.GetActiveScene().name == MenuScene) return intro;
        var context = SceneContext.Current;
        if (context == null || !context.IsGameplayZone) return null;

        if (BossFighting()) return boss != null ? boss : battle;
        if (EnemyNearby()) lastCombatTime = Time.unscaledTime;
        return Time.unscaledTime - lastCombatTime < calmDelay ? battle : normal;
    }

    private static bool BossFighting()
    {
        var guardian = FindAnyObjectByType<GuardianBoss>();
        return guardian != null && guardian.isActiveAndEnabled
            && guardian.Current != GuardianBoss.State.Dormant && guardian.Current != GuardianBoss.State.Dead;
    }

    private bool EnemyNearby()
    {
        var player = SceneContext.FindPlayer();
        if (player == null) return false;
        Vector2 me = player.transform.position;
        foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead || enemy.GetComponent<GuardianBoss>() != null) continue;
            if (Vector2.Distance(me, enemy.transform.position) <= combatRange) return true;
        }
        return false;
    }

    private void Play(AudioClip clip)
    {
        if (sources[active].clip == clip) return;
        active = 1 - active;
        // Si se vuelve a la pista que se estaba desvaneciendo, sigue donde iba en lugar de empezar de nuevo
        if (sources[active].clip == clip && clip != null) return;
        var next = sources[active];
        next.clip = clip;
        next.volume = 0f;
        if (clip != null) next.Play();
    }

    private void Fade()
    {
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime) * volume;
        for (int i = 0; i < sources.Length; i++)
        {
            var s = sources[i];
            float target = i == active && s.clip != null ? volume : 0f;
            s.volume = Mathf.MoveTowards(s.volume, target, step);
            if (i != active && s.volume <= 0f && s.isPlaying)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }
}
