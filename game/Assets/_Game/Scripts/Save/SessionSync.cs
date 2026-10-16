using UnityEngine;

// GAME-106 / SCRUM-82 (106.2): copia el estado del jugador al SessionState
// al salir de la escena y lo restaura al entrar a la siguiente.
// Va en el jugador, junto a Inventory, PlayerStats y Health.
[RequireComponent(typeof(Inventory), typeof(PlayerStats), typeof(Health))]
public class SessionSync : MonoBehaviour
{
    private Inventory inventory;
    private PlayerStats stats;
    private Health health;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        stats = GetComponent<PlayerStats>();
        health = GetComponent<Health>();
    }

        private void Start()
    {
        RestoreFromSession();
        if (SessionState.Instance != null)
            SessionState.Instance.BeforeSceneChange += CopyToSession;
    }

    private void OnDestroy()
    {
        if (SessionState.Instance != null)
            SessionState.Instance.BeforeSceneChange -= CopyToSession;
    }

    public void CopyToSession()
    {
        var session = SessionState.Instance;
        if (session == null) return;

        session.materials = inventory.ExportEntries();
        session.swordLevel = stats.SwordLevel;
        session.currentHealth = health.CurrentHealth;
    }

        public void RestoreFromSession()
    {
        var session = SessionState.Instance;
        if (session == null) return;

        inventory.ImportEntries(session.materials);
        stats.SetSwordLevel(session.swordLevel);
        if (session.currentHealth > 0)
            health.SetCurrentHealth(session.currentHealth);

        // 106.3: aparecer en el punto seguro si se pidió.
        if (session.respawnAtSafePoint)
        {
            session.respawnAtSafePoint = false;
            var pos = new Vector3(session.safePointX, session.safePointY, transform.position.z);
            transform.position = pos;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = pos;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    [ContextMenu("TEST - Guardar punto seguro aquí")]
    private void TestSaveSafePoint()
    {
        if (SessionState.Instance != null)
            SessionState.Instance.SaveSafePoint("base", transform.position);
    }
}