using System.Collections.Generic;
using UnityEngine;

// Base de todo lo que se usa con E (NPC, espada, estatua, forja, cristal de guardado).
// PlayerInteractor elige el más cercano dentro de su radio y muestra el aviso en el HUD.
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    [SerializeField] protected string prompt = "Interactuar";
    [SerializeField] protected float radius = 0.45f;
    [Tooltip("Icono \"!\" que aparece cuando el jugador está cerca (opcional).")]
    [SerializeField] protected GameObject marker;

    public float Radius => radius;
    public virtual string Prompt => prompt;
    public virtual bool CanInteract => true;

    protected virtual void OnEnable() => All.Add(this);

    protected virtual void OnDisable()
    {
        All.Remove(this);
        SetHighlighted(false);
    }

    public abstract void Interact();

    public void SetHighlighted(bool value)
    {
        if (marker != null) marker.SetActive(value);
    }

    public void Configure(string promptText, float range, GameObject markerObject)
    {
        prompt = promptText;
        radius = range;
        marker = markerObject;
        if (marker != null) marker.SetActive(false);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
