using UnityEngine;

// Aplica al jugador lo que depende de la historia: sin la espada carmesí no puede atacar.
[RequireComponent(typeof(PlayerController))]
public class PlayerStoryState : MonoBehaviour
{
    private PlayerController controller;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void OnEnable() => StoryFlags.Changed += HandleFlag;
    private void OnDisable() => StoryFlags.Changed -= HandleFlag;

    private void Start() => Apply();

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
