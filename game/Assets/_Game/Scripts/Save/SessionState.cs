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
    [Tooltip("Ruta de la escena del punto seguro")]
    public string safePointScene = "";
    public float safePointX;
    public float safePointY;
    public bool sealOpened;
    public bool guardianPermission;

    // 106.3: si es true, al cargar la escena el jugador aparece en el punto seguro.
    // No se guarda en el archivo; solo vive durante el cambio de escena.
    [NonSerialized] public bool respawnAtSafePoint;

    public bool HasSafePoint => !string.IsNullOrEmpty(safePointScene);

    // Se dispara justo antes de cambiar de escena, para que el jugador copie su estado.
    public event Action BeforeSceneChange;

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

    // Acepta nombre o ruta de escena ("Assets/.../Escena.unity").
    public void ChangeScene(string scene)
    {
        BeforeSceneChange?.Invoke();
        LoadSceneInternal(scene);
    }

    // GAME-106 / SCRUM-83 (106.3): guarda dónde reaparece el jugador.
    public void SaveSafePoint(string id, Vector3 position)
    {
        safePointId = id;
        safePointScene = SceneManager.GetActiveScene().path;
        safePointX = position.x;
        safePointY = position.y;
        Debug.Log($"Punto seguro '{id}' guardado en {safePointScene} ({safePointX:0.00}, {safePointY:0.00})");
    }

    // GAME-106 / SCRUM-83 (106.3): lleva al jugador a su punto seguro (muerte o cargar partida).
    public void RestoreSafePoint()
    {
        if (!HasSafePoint)
        {
            Debug.LogWarning("No hay punto seguro guardado.");
            return;
        }
        respawnAtSafePoint = true;
        ChangeScene(safePointScene);
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
        safePointX = 0f;
        safePointY = 0f;
        sealOpened = false;
        guardianPermission = false;
        respawnAtSafePoint = false;
    }

    private void LoadSceneInternal(string scene)
    {
#if UNITY_EDITOR
        // En el editor, si la escena no está en Build Profiles, la carga por ruta.
        string path = scene.EndsWith(".unity") ? scene : SceneManager.GetActiveScene().path;
        if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene);
    }

    [ContextMenu("TEST - Recargar escena")]
    private void TestReloadScene()
    {
        ChangeScene(SceneManager.GetActiveScene().path);
    }

    [ContextMenu("TEST - Ir a punto seguro")]
    private void TestRestoreSafePoint()
    {
        RestoreSafePoint();
    }

    [ContextMenu("TEST - Ver JSON de guardado")]
    private void TestShowSaveJson()
    {
        var data = SaveData.FromSession(this);
        Debug.Log(JsonUtility.ToJson(data, true));
    }
}

[Serializable]
public class MaterialEntry
{
    public string id;
    public int amount;
}