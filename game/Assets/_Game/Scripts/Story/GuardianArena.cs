using System.Collections;
using UnityEngine;

// Controla el encuentro con el guardián: presentación, barra de vida, derrota,
// permiso (una sola vez), guardado y paso al cierre de la demo.
[RequireComponent(typeof(Collider2D))]
public class GuardianArena : MonoBehaviour
{
    public const string MetFlag = "guardian_met";

    [SerializeField] private GuardianBoss boss;
    [SerializeField] private PermitPickup permit;
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private DialogueData againDialogue;
    [SerializeField] private DialogueData defeatDialogue;

    private bool started;

    public void Setup(GuardianBoss guardian, PermitPickup permitPickup, DialogueData intro, DialogueData again, DialogueData defeat)
    {
        boss = guardian;
        permit = permitPickup;
        introDialogue = intro;
        againDialogue = again;
        defeatDialogue = defeat;
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || other.GetComponentInParent<PlayerController>() == null) return;
        started = true;
        var dialogue = StoryFlags.Has(MetFlag) ? againDialogue : introDialogue;
        DialogueUI.Play(dialogue, () =>
        {
            StoryFlags.Set(MetFlag);
            if (GameHud.Instance != null) GameHud.Instance.ShowBoss(boss);
            boss.Begin();
        });
    }

    private void OnBossDied()
    {
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
