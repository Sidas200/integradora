using System.Collections;
using UnityEngine;

// HEX-08: cristal de guardado (base y antes del jefe). Al acercarse guarda una vez por visita;
// con E vuelve a guardar. Siempre restaura la vida antes de guardar.
public class SavePoint : Interactable
{
    [SerializeField] private string safePointId = "base";
    [Tooltip("Dónde reaparece el jugador al cargar (delante del cristal)")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float autoSaveRadius = 0.7f;
    [SerializeField] private SpriteRenderer crystal;

    private bool autoSaved;

    public void Setup(string id, Transform respawn, SpriteRenderer crystalRenderer)
    {
        safePointId = id;
        respawnPoint = respawn;
        crystal = crystalRenderer;
    }

    public override string Prompt => "Guardar partida";

    private void Update()
    {
        if (autoSaved || GameInput.Locked) return;
        var player = SceneContext.FindPlayer();
        if (player == null) return;
        if (Vector2.Distance(player.transform.position, transform.position) <= autoSaveRadius)
        {
            autoSaved = true;
            Save();
        }
    }

    public override void Interact() => Save();

    private void Save()
    {
        var player = SceneContext.FindPlayer();
        var health = player != null ? player.GetComponent<Health>() : null;
        if (health != null && !health.IsDead) health.Heal(health.MaxHealth);

        var spot = respawnPoint != null ? respawnPoint.position : transform.position;
        bool ok = GameFlow.Instance != null && GameFlow.Instance.SaveAtSafePoint(safePointId, spot);
        if (GameHud.Instance != null)
            GameHud.Instance.ShowToast(ok ? "Partida guardada. Vida restaurada." : "No se pudo guardar la partida.");
        if (ok && crystal != null) StartCoroutine(Glow());
    }

    private IEnumerator Glow()
    {
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            crystal.color = Color.Lerp(new Color(0.6f, 1f, 1f), Color.white, t / 0.6f);
            yield return null;
        }
        crystal.color = Color.white;
    }
}
