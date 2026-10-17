using UnityEngine;

// Identificador estable de un objeto del mundo ("escena/marcador") para recordar que ya se usó.
public class PersistentId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    public void SetId(string value) => id = value;
}
