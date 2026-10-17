using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD del capítulo (SRS): vida, nombre de zona y avisos en pantalla.
// Todo lo importante se comunica con texto para poder jugar sin sonido (RNF).
public class GameHud : MonoBehaviour
{
    public static GameHud Instance { get; private set; }

    [SerializeField] private CanvasGroup root;

    [Header("Vida")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Objetivo e interacción")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private TMP_Text promptText;

    [Header("Materiales (HEX-04)")]
    [SerializeField] private MaterialData[] trackedMaterials;
    [SerializeField] private int[] materialGoals;
    [SerializeField] private TMP_Text[] materialTexts;

    [Header("Jefe")]
    [SerializeField] private CanvasGroup bossGroup;
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private Image bossFill;
    [SerializeField] private TMP_Text bossState;

    [Header("Avisos")]
    [SerializeField] private CanvasGroup zoneTitleGroup;
    [SerializeField] private TMP_Text zoneTitleText;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;

    private Health playerHealth;
    private Inventory inventory;
    private Coroutine zoneRoutine;
    private Coroutine toastRoutine;

    private string lastObjective;
    private GuardianBoss boss;

    private void Awake()
    {
        Instance = this;
        if (zoneTitleGroup != null) zoneTitleGroup.alpha = 0f;
        if (toastGroup != null) toastGroup.alpha = 0f;
        if (promptGroup != null) promptGroup.alpha = 0f;
        if (bossGroup != null) bossGroup.alpha = 0f;
        StoryFlags.Changed += HandleFlagChanged;
        ChapterObjectives.AmountProvider = AmountOf;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        StoryFlags.Changed -= HandleFlagChanged;
        Unbind();
    }

    private void HandleFlagChanged(string flag) => RefreshObjective(true);

    // Actualiza el objetivo; si cambió, lo anuncia con un aviso (legible sin sonido).
    public void RefreshObjective(bool announce = false)
    {
        if (objectiveText == null) return;
        string current = ChapterObjectives.Current();
        objectiveText.text = current;
        if (announce && lastObjective != null && current != lastObjective)
            ShowToast("Nuevo objetivo: " + current.Split('\n')[0]);
        lastObjective = current;
    }

    public void SetPrompt(string text)
    {
        if (promptGroup == null) return;
        bool show = !string.IsNullOrEmpty(text);
        promptGroup.alpha = show ? 1f : 0f;
        if (show) promptText.text = text;
    }

    // Barra de vida del guardián con su estado (legible sin sonido)
    public void ShowBoss(GuardianBoss guardian)
    {
        HideBoss();
        boss = guardian;
        if (boss == null || bossGroup == null) return;
        bossName.text = boss.DisplayName;
        boss.Health.HealthChanged += UpdateBossHealth;
        boss.StateChanged += UpdateBossState;
        UpdateBossHealth(boss.Health.CurrentHealth, boss.Health.MaxHealth);
        UpdateBossState(boss.Current);
        bossGroup.alpha = 1f;
    }

    public void HideBoss()
    {
        if (boss != null)
        {
            boss.Health.HealthChanged -= UpdateBossHealth;
            boss.StateChanged -= UpdateBossState;
        }
        boss = null;
        if (bossGroup != null) bossGroup.alpha = 0f;
    }

    private void UpdateBossHealth(int current, int max)
    {
        bossFill.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    private void UpdateBossState(GuardianBoss.State state)
    {
        switch (state)
        {
            case GuardianBoss.State.Vulnerable:
                bossState.text = "¡Vulnerable! Ataca ahora";
                bossState.color = new Color(0.6f, 1f, 0.7f);
                break;
            case GuardianBoss.State.Telegraph:
                bossState.text = "¡Cuidado! Sal de la zona roja";
                bossState.color = new Color(1f, 0.55f, 0.55f);
                break;
            default:
                bossState.text = "Protegido por su armadura";
                bossState.color = new Color(1f, 0.85f, 0.55f);
                break;
        }
    }

    public void OnSceneReady(SceneContext context)
    {
        HideBoss();
        SetVisible(context.ShowHud);
        Bind(SceneContext.FindPlayer());
        RefreshObjective();
        if (context.ShowHud && !string.IsNullOrEmpty(context.ZoneName)) ShowZoneTitle(context.ZoneName);
    }

    public void SetVisible(bool visible)
    {
        if (root == null) return;
        root.alpha = visible ? 1f : 0f;
    }

    private void Bind(GameObject player)
    {
        Unbind();
        inventory = player != null ? player.GetComponent<Inventory>() : null;
        knownAmounts.Clear();
        if (inventory != null)
        {
            foreach (var e in inventory.ExportEntries()) knownAmounts[e.id] = e.amount;
            inventory.OnAmountChanged += HandleAmountChanged;
            RefreshMaterials();
        }
        playerHealth = player != null ? player.GetComponent<Health>() : null;
        if (playerHealth == null) return;
        playerHealth.HealthChanged += UpdateHealth;
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void Unbind()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= UpdateHealth;
        playerHealth = null;
        if (inventory != null) inventory.OnAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    private readonly System.Collections.Generic.Dictionary<string, int> knownAmounts = new System.Collections.Generic.Dictionary<string, int>();

    private int AmountOf(string id) => inventory != null ? inventory.GetAmount(id) : 0;

    private void HandleAmountChanged(string id, int amount)
    {
        knownAmounts.TryGetValue(id, out int before);
        knownAmounts[id] = amount;
        RefreshMaterials();
        RefreshObjective(false);
        if (trackedMaterials == null) return;
        for (int i = 0; i < trackedMaterials.Length; i++)
        {
            var m = trackedMaterials[i];
            if (m == null || m.id != id || amount <= before) continue; // solo al recoger
            int goal = GoalOf(i);
            ShowToast(goal > 0 ? $"{m.displayName}: {amount}/{goal}" : $"{m.displayName}: {amount}", 1.6f);
        }
    }

    private int GoalOf(int i) => materialGoals != null && i < materialGoals.Length ? materialGoals[i] : 0;

    private void RefreshMaterials()
    {
        if (trackedMaterials == null || materialTexts == null) return;
        for (int i = 0; i < trackedMaterials.Length && i < materialTexts.Length; i++)
        {
            var m = trackedMaterials[i];
            if (m == null || materialTexts[i] == null) continue;
            int have = AmountOf(m.id);
            int goal = GoalOf(i);
            materialTexts[i].text = goal > 0 ? $"{have}/{goal}" : have.ToString();
            materialTexts[i].color = goal > 0 && have >= goal ? new Color(0.6f, 1f, 0.7f) : Color.white;
        }
    }

    private void UpdateHealth(int current, int max)
    {
        if (healthFill != null) healthFill.fillAmount = max > 0 ? (float)current / max : 0f;
        if (healthText != null) healthText.text = $"{current}/{max}";
    }

    public void ShowZoneTitle(string title)
    {
        if (zoneTitleText == null) return;
        zoneTitleText.text = title;
        if (zoneRoutine != null) StopCoroutine(zoneRoutine);
        zoneRoutine = StartCoroutine(Flash(zoneTitleGroup, 2.5f));
    }

    public void ShowToast(string message, float seconds = 2.5f)
    {
        if (toastText == null) return;
        toastText.text = message;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Flash(toastGroup, seconds));
    }

    private static IEnumerator Flash(CanvasGroup group, float hold)
    {
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.3f; yield return null; }
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(hold);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.5f; yield return null; }
        group.alpha = 0f;
    }
}
