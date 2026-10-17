using UnityEngine;

// Aplica al jugador lo que depende de la historia: sin la espada carmesí no puede atacar.
[RequireComponent(typeof(PlayerController))]
public class PlayerStoryState : MonoBehaviour
{
    private PlayerController controller;
    private PlayerStats stats;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        StoryFlags.Changed += HandleFlag;
        if (stats != null) stats.OnStatsChanged += HandleStats;
    }

    private void OnDisable()
    {
        StoryFlags.Changed -= HandleFlag;
        if (stats != null) stats.OnStatsChanged -= HandleStats;
    }

    private void Start() => Apply();

    // HEX-05: al mejorar la espada en la forja (15 → 25) avanza la historia, una sola vez
    private void HandleStats()
    {
        if (stats.SwordLevel < 1 || SessionState.Instance == null) return;
        if (StoryFlags.Set(ChapterObjectives.SwordUpgraded) && GameHud.Instance != null)
            GameHud.Instance.ShowToast($"¡La espada carmesí despertó! Daño: 15 → {stats.Attack}");
    }

    private void HandleFlag(string flag)
    {
        if (flag == SwordPickup.Flag) Apply();
    }

    private void Apply()
    {
        // Sin SessionState (escenas de prueba del equipo) se deja atacar normal
        controller.AttackEnabled = SessionState.Instance == null || StoryFlags.Has(SwordPickup.Flag);
    }
}
