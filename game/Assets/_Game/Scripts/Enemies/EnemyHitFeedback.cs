
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Efecto al recibir daño")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private EnemyHealth enemyHealth;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private EnemyMeleeController meleeController;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        meleeController = GetComponent<EnemyMeleeController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        enemyHealth.OnDamaged += HandleDamage;
    }

    private void OnDisable()
    {
        enemyHealth.OnDamaged -= HandleDamage;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void HandleDamage(int damage)
    {
        // Empuje y pausa al recibir daño
        if (meleeController != null)
            meleeController.ApplyHitReaction();

        // Destello rojo
        if (spriteRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashEffect());
    }

    private IEnumerator FlashEffect()
    {
        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}
