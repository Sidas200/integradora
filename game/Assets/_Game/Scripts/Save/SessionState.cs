using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// GAME-106 / SCRUM-81 (106.1): estado único de la sesión (SRS-HEX-03).
// Sobrevive a cambios de escena. Si ya existe uno, el nuevo se destruye.
[DefaultExecutionOrder(-100)]
public class SessionState : MonoBehaviour
{
    public static SessionState Instance { get; private set; }

    [Header("Jugador")]
    [Tooltip("-1 = sin dato todavía (usar vida máxima)")]
    public int currentHealth = -1;
    public int swordLevel;

    [Header("Materiales")]
    public List<MaterialEntry> materials = new List<MaterialEntry>();

    [Header("Progreso")]
    [Tooltip("\"base\" o \"boss\" (HEX-08)")]
    public string safePointId = "";
    public string safePointScene = "";
    public bool sealOpened;
    public bool guardianPermission;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetMaterial(string id)
    {
        var entry = materials.Find(m => m.id == id);
        return entry != null ? entry.amount : 0;
    }

    public void SetMaterial(string id, int amount)
    {
        var entry = materials.Find(m => m.id == id);
        if (entry != null) entry.amount = amount;
        else materials.Add(new MaterialEntry { id = id, amount = amount });
    }

    // Nueva partida: deja todo en cero.
    public void ResetState()
    {
        currentHealth = -1;
        swordLevel = 0;
        materials.Clear();
        safePointId = "";
        safePointScene = "";
        sealOpened = false;
        guardianPermission = false;
    }

    [ContextMenu("TEST - Recargar escena")]
    private void TestReloadScene()
    {
#if UNITY_EDITOR
        // En el editor carga la escena aunque no esté en Build Profiles.
        string path = SceneManager.GetActiveScene().path;
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
#endif
    }
}

[Serializable]
public class MaterialEntry
{
    public string id;
    public int amount;
}