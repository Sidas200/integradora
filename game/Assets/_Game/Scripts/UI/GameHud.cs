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

    [Header("Avisos")]
    [SerializeField] private CanvasGroup zoneTitleGroup;
    [SerializeField] private TMP_Text zoneTitleText;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;

    private Health playerHealth;
    private Coroutine zoneRoutine;
    private Coroutine toastRoutine;

    private void Awake()
    {
        Instance = this;
        if (zoneTitleGroup != null) zoneTitleGroup.alpha = 0f;
        if (toastGroup != null) toastGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unbind();
    }

    public void OnSceneReady(SceneContext context)
    {
        SetVisible(context.ShowHud);
        Bind(SceneContext.FindPlayer());
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
        playerHealth = player != null ? player.GetComponent<Health>() : null;
        if (playerHealth == null) return;
        playerHealth.HealthChanged += UpdateHealth;
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void Unbind()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= UpdateHealth;
        playerHealth = null;
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
