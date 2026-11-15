using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ícono del aviso de interacción del HUD según lo que dice: disquete para guardar, globo para hablar
// y un rombo para lo demás.
public class UiPromptIcon : MonoBehaviour
{
    private TMP_Text text;
    private Image icon;
    private string shown;

    public void Setup(TMP_Text promptText, Image iconImage)
    {
        text = promptText;
        icon = iconImage;
        shown = null;
    }

    private void LateUpdate()
    {
        if (text == null || icon == null || text.text == shown) return;
        shown = text.text;
        string kind = "diamond";
        if (!string.IsNullOrEmpty(shown))
        {
            if (shown.Contains("Guardar")) kind = "save";
            else if (shown.Contains("Hablar")) kind = "talk";
        }
        icon.sprite = UiSkin.IconSprite(kind);
        icon.SetNativeSize();
        icon.rectTransform.sizeDelta *= UiSkin.IconScale;
    }
}
