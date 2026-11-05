
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Efecto al recibir daño")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [Tooltip("Parpadeo rojo / semitransparente, visible aunque el sprite ya sea rojo. " +
             "Se activa solo en los enemigos a distancia (EnemyRangedController).")]
    [SerializeField] private bool blink;
    [SerializeField] private int blinkCount = 3;

    // Mientras dura el destello, otros scripts no deben cambiar el color del sprite
    public bool IsFlashing => flashCoroutine != null;
    public SpriteRenderer Renderer => spriteRenderer;

    private EnemyHealth enemyHealth;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private EnemyMeleeController meleeController;
    private SlimeController slimeController;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        meleeController = GetComponent<EnemyMeleeController>();
        slimeController = GetComponent<SlimeController>();

        // El enemigo a distancia y el Fragmento ya son carmesí: un tinte rojo no se distinguía
        if (GetComponent<EnemyRangedController>() != null || GetComponent<FragmentoErranteController>() != null)
            blink = true;

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
        if (slimeController != null)
            slimeController.ApplyHitReaction();

        // Destello rojo
        if (spriteRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashEffect());
    }

    private IEnumerator FlashEffect()
    {
        if (!blink)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
        }
        else
        {
            var faded = new Color(originalColor.r, originalColor.g, originalColor.b, 0.25f);
            float half = flashDuration * 0.5f;
            for (int i = 0; i < blinkCount; i++)
            {
                spriteRenderer.color = flashColor;
                yield return new WaitForSeconds(half);
                spriteRenderer.color = faded;
                yield return new WaitForSeconds(half * 0.7f);
            }
        }

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}
