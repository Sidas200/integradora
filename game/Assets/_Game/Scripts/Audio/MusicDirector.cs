using UnityEngine;
using UnityEngine.SceneManagement;

// Música del juego. Elige la pista según la escena y la pelea, y cambia con un fundido:
// - Menú principal: intro, hasta dar Nueva partida o Continuar.
// - Zonas de juego: normal; cuando hay enemigos cerca, batalla, y al vencer al último vuelve la normal.
// - Zona del jefe (escena con Varn dormido): bossentrance.
// - Pelea con Varn: bossbattle; si falta, la de batalla. Al vencerlo vuelve la normal.
// - Cierre de la demo (después de vencer a Varn): la del menú (music_intro); al volver al menú sigue sin cortarse.
// - Intro de la historia: en silencio por ahora.
// Las pistas viven en Resources/Audio/Music; si falta una, esa parte queda en silencio.
public class MusicDirector : MonoBehaviour
{
    public const string MenuScene = "MainMenu";
    private const string Folder = "Audio/Music/";

    [SerializeField] private float volume = 0.6f;
    [Tooltip("Segundos que tarda en bajar la pista que se va.")]
    [SerializeField] private float fadeOutTime = 1.4f;
    [Tooltip("Segundos que tarda en subir la pista nueva.")]
    [SerializeField] private float fadeInTime = 2.2f;
    [Tooltip("La pista nueva empieza a subir cuando la anterior baja de esta fracción del volumen.")]
    [SerializeField, Range(0f, 1f)] private float crossoverLevel = 0.35f;
    [Tooltip("Distancia a la que un enemigo vivo cuenta como pelea.")]
    [SerializeField] private float combatRange = 3.5f;
    [Tooltip("Segundos sin enemigos cerca antes de volver a la música normal.")]
    [SerializeField] private float calmDelay = 1.5f;

    public static MusicDirector Instance { get; private set; }

    private AudioSource[] sources;
    private int active;
    private AudioClip intro, normal, battle, bossEntrance, bossBattle;
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
        bossEntrance = Resources.Load<AudioClip>(Folder + "bossentrance");
        bossBattle = Resources.Load<AudioClip>(Folder + "bossbattle");

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
        var scene = SceneManager.GetActiveScene();
        if (scene.name == MenuScene || IsDemoEnd(scene)) return intro;
        var context = SceneContext.Current;
        if (context == null || !context.IsGameplayZone) return null;

        var guardian = FindAnyObjectByType<GuardianBoss>();
        if (guardian != null && guardian.isActiveAndEnabled)
        {
            // Si el jugador muere, Varn vuelve a dormir y suena otra vez la entrada
            if (guardian.Current == GuardianBoss.State.Dormant && bossEntrance != null) return bossEntrance;
            if (guardian.Current != GuardianBoss.State.Dormant && guardian.Current != GuardianBoss.State.Dead)
                return bossBattle != null ? bossBattle : battle;
        }

        if (EnemyNearby()) lastCombatTime = Time.unscaledTime;
        return Time.unscaledTime - lastCombatTime < calmDelay ? battle : normal;
    }

    private static bool IsDemoEnd(Scene scene)
    {
        var flow = GameFlow.Instance;
        return flow != null && !string.IsNullOrEmpty(flow.DemoEndScene) &&
               (scene.path == flow.DemoEndScene || scene.name == flow.DemoEndScene);
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
        // Empieza a sonar en Fade, cuando le toca subir (así no se pierde el inicio de la pista)
        next.Stop();
    }

    // Cambio suave: la pista que se va baja primero y la nueva entra cuando la anterior ya está
    // baja (crossoverLevel), así el paso de calma a pelea no es de golpe.
    private void Fade()
    {
        float outStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeOutTime) * volume;
        float inStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeInTime) * volume;
        var leaving = sources[1 - active];
        bool waitForLeaving = leaving.clip != null && leaving.volume > volume * crossoverLevel;

        for (int i = 0; i < sources.Length; i++)
        {
            var s = sources[i];
            if (i == active)
            {
                if (s.clip != null && !waitForLeaving)
                {
                    if (!s.isPlaying) s.Play();
                    s.volume = Mathf.MoveTowards(s.volume, volume, inStep);
                }
            }
            else
            {
                s.volume = Mathf.MoveTowards(s.volume, 0f, outStep);
            }
            if (i != active && s.volume <= 0f && s.isPlaying)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }
}
