using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pestaña "Preparar poción" del inventario: a la izquierda la cuadrícula con lo que llevas,
// a la derecha la receta (materiales tengo / necesito), la vista previa y el botón Preparar.
// Al preparar se gastan los materiales y se reproduce la animación de 3 cuadros
// (frasco vacío → mezcla → poción lista). Corre con tiempo sin escalar: el inventario pausa el juego.
public class PotionBrewPage : MonoBehaviour
{
    private static readonly Color Enough = new Color(0.55f, 0.92f, 0.45f);
    private static readonly Color Missing = new Color(1f, 0.45f, 0.45f);

    [SerializeField] private CraftingRecipe recipe;

    [Header("Cuadrícula (solo muestra)")]
    [SerializeField] private List<ItemSlotView> gridSlots = new List<ItemSlotView>();

    [Header("Receta")]
    [SerializeField] private ItemSlotView resultSlot;
    [SerializeField] private TMP_Text resultName;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private List<ItemSlotView> ingredientSlots = new List<ItemSlotView>();
    [SerializeField] private List<TMP_Text> ingredientTexts = new List<TMP_Text>();
    [SerializeField] private List<Image> previewFrames = new List<Image>();
    [SerializeField] private TMP_Text status;
    [SerializeField] private Button brewButton;
    [SerializeField] private TMP_Text brewLabel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text closeLabel;

    [Header("Animación")]
    [SerializeField] private Sprite[] brewFrames = new Sprite[3];
    [SerializeField] private CanvasGroup fxGroup;
    [SerializeField] private RectTransform fxPotion;
    [SerializeField] private Image fxImage;
    [SerializeField] private TMP_Text fxLabel;
    [SerializeField] private float emptyTime = 0.45f;
    [SerializeField] private float mixTime = 0.9f;
    [SerializeField] private float readyTime = 0.9f;

    private Inventory inventory;
    private Coroutine brewing;

    public Button BrewButton => brewButton;
    public Button CloseButton => closeButton;
    public IEnumerable<ItemSlotView> GridSlots => gridSlots;
    public bool IsBrewing => brewing != null;

    // Se dispara al gastar los materiales (el inventario vuelve a pintar la cuadrícula)
    public event System.Action Brewed;

    private void Awake()
    {
        if (brewButton != null) brewButton.onClick.AddListener(Brew);
        HideFx();
    }

    private void OnDisable()
    {
        // Cerrar el inventario o cambiar de pestaña corta la animación (los materiales ya se gastaron)
        if (brewing != null) StopCoroutine(brewing);
        brewing = null;
        HideFx();
    }

    public void Refresh(Inventory inv, IReadOnlyList<Sprite> icons, IReadOnlyList<string> amounts)
    {
        inventory = inv;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] == null) continue;
            if (icons != null && i < icons.Count) gridSlots[i].Show(icons[i], amounts[i]);
            else gridSlots[i].Clear();
            gridSlots[i].SetSelected(false);
            gridSlots[i].Button.interactable = false;
        }
        RefreshRecipe();
    }

    public void UpdateHints()
    {
        // El estilo de botones (UiSkin) fija 30 pt: aquí no cabe, se ajusta al ancho
        Fit(brewLabel);
        Fit(closeLabel);
        if (brewLabel != null)
        {
            string label = ControlHints.For("F / Enter: Preparar", "A: Preparar", ControlHints.Cross + ": Preparar");
            if (brewLabel.text != label) brewLabel.text = label;
        }
        if (closeLabel != null)
        {
            string label = ControlHints.For("Esc / Tab: Cerrar", "B: Cerrar", ControlHints.Circle + ": Cerrar");
            if (closeLabel.text != label) closeLabel.text = label;
        }
    }

    private static void Fit(TMP_Text label)
    {
        if (label == null || label.enableAutoSizing) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 22f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.margin = new Vector4(18f, 0f, 12f, 0f);
    }

    private void RefreshRecipe()
    {
        var result = recipe != null ? recipe.result : null;
        if (resultSlot != null) resultSlot.Show(result != null ? result.icon : null, "");
        if (resultName != null) resultName.text = result != null ? result.displayName : "Sin receta";
        if (resultText != null)
        {
            int owned = result != null && inventory != null ? inventory.GetAmount(result.id) : 0;
            resultText.text = result != null
                ? $"{result.description}\n<color=#f0d27a>Tienes: {owned}</color>"
                : "";
        }

        int count = recipe != null ? recipe.ingredients.Count : 0;
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            bool used = i < count && recipe.ingredients[i].material != null;
            ingredientSlots[i].gameObject.SetActive(used);
            if (i < ingredientTexts.Count) ingredientTexts[i].gameObject.SetActive(used);
            if (!used) continue;

            var ing = recipe.ingredients[i];
            int have = inventory != null ? inventory.GetAmount(ing.material.id) : 0;
            ingredientSlots[i].Show(ing.material.icon, "", false, have >= ing.amount ? 1f : 0.45f);
            if (i < ingredientTexts.Count)
            {
                var t = ingredientTexts[i];
                t.text = $"{ing.material.displayName}\n{have} / {ing.amount}";
                t.color = have >= ing.amount ? Enough : Missing;
            }
        }

        for (int i = 0; i < previewFrames.Count; i++)
        {
            if (previewFrames[i] == null) continue;
            previewFrames[i].sprite = i < brewFrames.Length ? brewFrames[i] : null;
            previewFrames[i].color = Color.white;
            previewFrames[i].rectTransform.localScale = Vector3.one;
        }

        bool ready = CanBrew;
        if (status != null)
        {
            status.text = ready ? "Listo para preparar" : "Te faltan materiales";
            status.color = ready ? Enough : Missing;
        }
        if (brewButton != null) brewButton.interactable = ready && !IsBrewing;
        UpdateHints();
    }

    private bool CanBrew => recipe != null && inventory != null && inventory.CanCraft(recipe);

    public void Brew()
    {
        if (IsBrewing || recipe == null || inventory == null || !isActiveAndEnabled) return;
        if (!inventory.CanCraft(recipe))
        {
            Toast("Te faltan materiales para la poción.");
            return;
        }
        if (!inventory.TryCraft(recipe)) return;

        if (GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
        brewing = StartCoroutine(BrewRoutine());
        Brewed?.Invoke();
    }

    // ------------------------------------------------------------------ animación

    private IEnumerator BrewRoutine()
    {
        if (brewButton != null) brewButton.interactable = false;
        if (fxGroup != null)
        {
            fxGroup.gameObject.SetActive(true);
            fxGroup.alpha = 0f;
        }

        // 1) Frasco vacío: aparece con un pequeño rebote
        SetFx(0, "Preparando…");
        yield return Phase(emptyTime, t =>
        {
            if (fxGroup != null) fxGroup.alpha = Mathf.Clamp01(t * 3f);
            Pose(Back(t) * 1f, 0f);
        });

        // 2) Mezcla: el frasco se balancea y late mientras gira el brillo
        SetFx(1, "Mezclando…");
        yield return Phase(mixTime, t =>
        {
            float wobble = Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t * 0.5f);
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.05f;
            Pose(pulse, wobble);
        });

        // 3) Lista: golpe de escala y destello
        SetFx(2, "¡Poción lista!");
        if (fxLabel != null) fxLabel.color = new Color(1f, 0.85f, 0.5f);
        yield return Phase(readyTime, t =>
        {
            float punch = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t * 2.5f) * Mathf.PI);
            Pose(punch, 0f);
            if (fxImage != null) fxImage.color = Color.Lerp(new Color(1.6f, 1.6f, 1.6f), Color.white, Mathf.Clamp01(t * 2f));
        });

        // Desvanecer
        yield return Phase(0.25f, t => { if (fxGroup != null) fxGroup.alpha = 1f - t; });

        HideFx();
        brewing = null;

        var result = recipe.result;
        if (result != null)
            Toast($"¡Preparaste: {result.displayName}! Ahora tienes {inventory.GetAmount(result.id)}.");
        RefreshRecipe();
    }

    private void SetFx(int frame, string label)
    {
        if (fxImage != null && frame < brewFrames.Length)
        {
            fxImage.sprite = brewFrames[frame];
            fxImage.color = Color.white;
        }
        if (fxLabel != null)
        {
            fxLabel.text = label;
            fxLabel.color = UiFxText;
        }
        // La vista previa acompaña: el cuadro actual resalta, los demás se atenúan
        for (int i = 0; i < previewFrames.Count; i++)
        {
            if (previewFrames[i] == null) continue;
            bool current = i == frame;
            previewFrames[i].color = current ? Color.white : new Color(1f, 1f, 1f, i < frame ? 0.7f : 0.35f);
            previewFrames[i].rectTransform.localScale = current ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    private static readonly Color UiFxText = new Color(0.98f, 0.94f, 0.88f);

    private void Pose(float scale, float angle)
    {
        if (fxPotion == null) return;
        fxPotion.localScale = Vector3.one * scale;
        fxPotion.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static IEnumerator Phase(float duration, System.Action<float> step)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            step(elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        step(1f);
    }

    // Rebote al llegar (ease out back), de 0 a 1
    private static float Back(float t)
    {
        const float c = 1.70158f;
        t -= 1f;
        return 1f + (c + 1f) * t * t * t + c * t * t;
    }

    private void HideFx()
    {
        if (fxGroup != null)
        {
            fxGroup.alpha = 0f;
            fxGroup.gameObject.SetActive(false);
        }
        Pose(1f, 0f);
    }

    private static void Toast(string text)
    {
        if (GameHud.Instance != null) GameHud.Instance.ShowToast(text, 2.5f);
    }
}
