using System.Collections;
using UnityEngine;

// Controla el encuentro con el guardián: Varn duerme hasta que Tsuki le ofrece una Gota de
// Amrita (E frente a él o G). Después: presentación, barra de vida, derrota, choque de poder
// (PowerClash), permiso (una sola vez), guardado y paso al cierre de la demo.
[RequireComponent(typeof(Collider2D))]
public class GuardianArena : MonoBehaviour
{
    public const string MetFlag = "guardian_met";
    public const string AwakenedFlag = "guardian_awakened";

    [SerializeField] private GuardianBoss boss;
    [SerializeField] private PermitPickup permit;
    [SerializeField] private DialogueData sleepingDialogue;
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private DialogueData againDialogue;
    [SerializeField] private DialogueData defeatDialogue;
    [Tooltip("Cuando Varn se saca la espada del pecho (50% de vida).")]
    [SerializeField] private DialogueData swordDialogue;
    [Header("Cámara")]
    [Tooltip("Tamaño de la cámara al enfocar a Varn (menor = más cerca).")]
    [SerializeField, Range(0.3f, 1f)] private float focusZoom = 0.7f;
    [Tooltip("Segundos de silencio con la cámara sobre Varn antes de que despierte.")]
    [SerializeField] private float wakeSuspense = 1f;
    [Tooltip("Salida de regreso al bosque: se cierra mientras dura la pelea.")]
    [SerializeField] private GameObject retreatExit;

    private static GuardianArena current;
    private bool started;
    private bool playerInside;
    private ItemUser itemUser;

    // Verdadero mientras el jugador está en la arena con el guardián dormido.
    public bool AwaitingAmrita => playerInside && !started && !StoryFlags.Has(AwakenedFlag);
    public static bool CanReceiveAmrita => current != null && current.AwaitingAmrita;

    public void Setup(GuardianBoss guardian, PermitPickup permitPickup, DialogueData sleeping, DialogueData intro, DialogueData again, DialogueData defeat)
    {
        boss = guardian;
        permit = permitPickup;
        sleepingDialogue = sleeping;
        introDialogue = intro;
        againDialogue = again;
        defeatDialogue = defeat;
    }

    public void SetRetreatExit(GameObject exit) => retreatExit = exit;

    public void SetSwordDialogue(DialogueData dialogue) => swordDialogue = dialogue;

    private void OnEnable() => current = this;

    private void OnDisable()
    {
        if (current == this) current = null;
        if (itemUser != null) itemUser.AmritaUsed -= OnAmritaUsed;
    }

    private void Start()
    {
        if (StoryFlags.Has(ChapterObjectives.PermitObtained))
        {
            // Ya vencido (al continuar una partida): no reaparece ni vuelve a dar el permiso
            boss.gameObject.SetActive(false);
            permit.gameObject.SetActive(false);
            started = true;
            return;
        }
        permit.gameObject.SetActive(false);
        boss.Health.Died += OnBossDied;
        boss.SwordCutscene = SwordCutscene;
        boss.SetSleeping(!StoryFlags.Has(AwakenedFlag));

        var player = SceneContext.FindPlayer();
        itemUser = player != null ? player.GetComponent<ItemUser>() : null;
        if (itemUser != null) itemUser.AmritaUsed += OnAmritaUsed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || other.GetComponentInParent<PlayerController>() == null) return;
        if (playerInside) return;
        playerInside = true;

        if (StoryFlags.Has(AwakenedFlag))
        {
            // Ya despierto (reintento en la misma sesión): pelea directa
            StartFight(StoryFlags.Has(MetFlag) ? againDialogue : introDialogue);
            return;
        }
        DialogueUI.Play(sleepingDialogue);
    }

    private void OnAmritaUsed()
    {
        if (started) return;
        StartCoroutine(Awaken());
    }

    private IEnumerator Awaken()
    {
        started = true;
        StoryFlags.Set(AwakenedFlag);
        GameInput.Lock(this);
        // Suspenso: la cámara va hacia Varn dormido y espera antes de que se levante
        FocusOnBoss();
        yield return new WaitForSeconds(wakeSuspense);
        yield return boss.WakeUp();
        GameInput.Unlock(this);
        StartFight(introDialogue);
    }

    private void StartFight(DialogueData dialogue)
    {
        started = true;
        if (retreatExit != null) retreatExit.SetActive(false);
        DialogueUI.Play(dialogue, () =>
        {
            CameraFocus.Release();
            StoryFlags.Set(MetFlag);
            if (GameHud.Instance != null) GameHud.Instance.ShowBoss(boss);
            boss.Begin();
            if (TryGetComponent<GuardianSummons>(out var summons)) summons.Begin(boss);
        });
    }

    // Al cruzar el 50%: el juego se detiene, la cámara va a Varn, primero el diálogo y después la
    // animación de la espada (2 s) para que se aprecie. Luego sigue la pelea.
    private IEnumerator SwordCutscene()
    {
        GameInput.Lock(this);
        float previousScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        FocusOnBoss();
        yield return new WaitForSecondsRealtime(0.7f);

        bool read = false;
        // Al cerrarse, el diálogo pone la escala en 1: se vuelve a detener en el mismo momento
        DialogueUI.Play(swordDialogue, () => { read = true; Time.timeScale = 0f; });
        while (!read) yield return null;

        yield return boss.PlayDrawSword(boss.DrawSwordTime);
        yield return new WaitForSecondsRealtime(0.3f);

        CameraFocus.Release();
        Time.timeScale = previousScale;
        GameInput.Unlock(this);
    }

    // La cámara sigue a Varn aunque Tsuki esté lejos
    private void FocusOnBoss() => CameraFocus.Show(() => boss.FocusPoint, focusZoom);

    private void OnBossDied()
    {
        if (TryGetComponent<GuardianSummons>(out var summons)) summons.Clear();
        if (GameHud.Instance != null) GameHud.Instance.HideBoss();
        StartCoroutine(AfterVictory(boss.transform.position));
    }

    private IEnumerator AfterVictory(Vector3 position)
    {
        GameInput.Lock(this);
        var body = boss.DefeatedBody;
        if (body != null)
        {
            // Choque de poder final; Varn se arrodilla al perderlo
            yield return new WaitForSeconds(0.6f);
            if (!TryGetComponent<PowerClash>(out var clash)) clash = gameObject.AddComponent<PowerClash>();
            yield return clash.Play(boss, body);
            if (clash.Result == PowerClash.Outcome.PlayerDied)
            {
                GameInput.Unlock(this);
                yield break;
            }
            if (clash.Result == PowerClash.Outcome.Skipped) body.Play(GuardianVarnAnimator.Defeat, 8f, false);
            position = body.transform.position;
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
        GameInput.Unlock(this);
        DialogueUI.Play(defeatDialogue, () =>
        {
            permit.transform.position = position;
            permit.gameObject.SetActive(true);
        });
    }
}
