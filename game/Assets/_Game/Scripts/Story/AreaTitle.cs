using UnityEngine;

// Muestra el nombre de un área dentro de una zona al entrar (p. ej. "Campamento oculto").
[RequireComponent(typeof(Collider2D))]
public class AreaTitle : MonoBehaviour
{
    [SerializeField] private string title;
    [SerializeField] private float cooldown = 20f;

    private float nextAt;

    public void Setup(string areaTitle) => title = areaTitle;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.unscaledTime < nextAt || other.GetComponentInParent<PlayerController>() == null) return;
        nextAt = Time.unscaledTime + cooldown;
        if (GameHud.Instance != null) GameHud.Instance.ShowZoneTitle(title);
    }
}
