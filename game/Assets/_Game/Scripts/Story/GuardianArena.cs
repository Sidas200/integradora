using System.Collections;
using UnityEngine;

// Controla el encuentro con el guardián: Varn duerme hasta que Tsuki le ofrece una Gota de
// Amrita (E frente a él o G). Después: presentación, barra de vida, derrota, permiso (una
// sola vez), guardado y paso al cierre de la demo.
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
            StoryFlags.Set(MetFlag);
            if (GameHud.Instance != null) GameHud.Instance.ShowBoss(boss);
            boss.Begin();
            if (TryGetComponent<GuardianSummons>(out var summons)) summons.Begin(boss);
        });
    }

    private void OnBossDied()
    {
        if (TryGetComponent<GuardianSummons>(out var summons)) summons.Clear();
        if (GameHud.Instance != null) GameHud.Instance.HideBoss();
        StartCoroutine(AfterVictory(boss.transform.position));
    }

    private IEnumerator AfterVictory(Vector3 position)
    {
        GameInput.Lock(this);
        yield return new WaitForSeconds(1f);
        GameInput.Unlock(this);
        DialogueUI.Play(defeatDialogue, () =>
        {
            permit.transform.position = position;
            permit.gameObject.SetActive(true);
        });
    }
}
