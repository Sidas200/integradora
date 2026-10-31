using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ayudas para menús con EventSystem (botones que se eligen con stick/cruceta y se confirman con A).
public static class UiNav
{
    public static bool CanSelect(Selectable s) =>
        s != null && s.IsActive() && s.interactable;

    public static void Select(Selectable s)
    {
        if (!CanSelect(s) || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    // Si se está usando el mando (o las flechas) y no hay nada válido seleccionado, por ejemplo
    // después de un clic en el fondo, vuelve a seleccionar `fallback` para que se pueda navegar.
    // `valid` limita qué selección se acepta (null = cualquier botón activo e interactuable).
    public static void KeepSelection(Selectable fallback, IList<Selectable> valid = null)
    {
        var es = EventSystem.current;
        if (es == null || !CanSelect(fallback)) return;
        if (!ControlHints.UsingGamepad && !KeyboardNavigationPressed()) return;

        var current = es.currentSelectedGameObject;
        var selectable = current != null ? current.GetComponent<Selectable>() : null;
        bool ok = CanSelect(selectable) && (valid == null || Contains(valid, selectable));
        if (!ok) es.SetSelectedGameObject(fallback.gameObject);
    }

    // Quita la selección si está dentro de `scope` (al cerrar un panel).
    public static void ClearSelectionIn(Transform scope)
    {
        var es = EventSystem.current;
        if (es == null || scope == null) return;
        var current = es.currentSelectedGameObject;
        if (current != null && current.transform.IsChildOf(scope))
            es.SetSelectedGameObject(null);
    }

    // Para paneles que manejan el mando por código (forja, inventario): el EventSystem no debe
    // mover la selección ni confirmar con A a la vez, o la acción se ejecuta dos veces.
    // El clic con el mouse sigue funcionando.
    public static void DisableNavigation(IEnumerable<Selectable> selectables)
    {
        foreach (var s in selectables)
        {
            if (s == null) continue;
            var nav = s.navigation;
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
        }
    }

    // Recorrido cíclico entre los botones activos (arriba/abajo y, si se pide, izquierda/derecha).
    public static void ChainNavigation(IList<Selectable> candidates, bool horizontalToo)
    {
        var list = new List<Selectable>();
        foreach (var s in candidates)
            if (CanSelect(s)) list.Add(s);

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            var previous = list[(i - 1 + count) % count];
            var next = list[(i + 1) % count];
            var nav = list[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = previous;
            nav.selectOnDown = next;
            nav.selectOnLeft = horizontalToo ? previous : null;
            nav.selectOnRight = horizontalToo ? next : null;
            list[i].navigation = nav;
        }
    }

    private static bool Contains(IList<Selectable> list, Selectable s)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == s) return true;
        return false;
    }

    private static bool KeyboardNavigationPressed()
    {
        var kb = Keyboard.current;
        return kb != null &&
               (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame);
    }

    // Busca un texto hijo por nombre (etiquetas que crea el builder del capítulo).
    public static TMPro.TMP_Text FindLabel(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (t.name == name) return t;
        return null;
    }
}
