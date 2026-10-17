using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cambios de escena con fundido. Vive en GameSystems (persistente).
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private ScreenFader fader;

    public bool IsBusy { get; private set; }

    private bool sceneLoaded;

    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => sceneLoaded = true;

    // Transición normal: el jugador copia su estado a la sesión antes de salir.
    public void LoadScene(string scenePath, string spawnId)
    {
        Run(() =>
        {
            var session = SessionState.Instance;
            session.pendingSpawnId = spawnId;
            session.ChangeScene(scenePath);
        });
    }

    // Carga sin copiar el estado actual del jugador (reintento, nueva partida, menú).
    public void LoadSceneFresh(string scenePath, string spawnId)
    {
        Run(() =>
        {
            if (SessionState.Instance != null) SessionState.Instance.pendingSpawnId = spawnId;
            SceneManager.LoadScene(scenePath);
        });
    }

    public void LoadFromSave(SaveData data)
    {
        Run(() => SessionState.Instance.LoadFromSave(data));
    }

    public void TeleportInScene(string spawnId)
    {
        if (IsBusy) return;
        StartCoroutine(TeleportRoutine(spawnId));
    }

    private void Run(Action load)
    {
        if (IsBusy) return;
        StartCoroutine(LoadRoutine(load));
    }

    private IEnumerator LoadRoutine(Action load)
    {
        IsBusy = true;
        GameInput.Lock(this);
        yield return fader.FadeOut();

        Time.timeScale = 1f;
        sceneLoaded = false;
        load();
        while (!sceneLoaded) yield return null;

        // Un frame para que Awake/Start de la escena nueva coloquen al jugador
        yield return null;
        GameInput.ClearDestroyed();
        yield return fader.FadeIn();
        GameInput.Unlock(this);
        IsBusy = false;
    }

    private IEnumerator TeleportRoutine(string spawnId)
    {
        IsBusy = true;
        GameInput.Lock(this);
        yield return fader.FadeOut(0.25f);
        SceneContext.MovePlayerTo(SpawnPoint.Find(spawnId));
        yield return null;
        yield return fader.FadeIn(0.25f);
        GameInput.Unlock(this);
        IsBusy = false;
    }
}
