using UnityEngine;
using UnityEngine.InputSystem.UI;

// Raíz persistente del capítulo: SessionState, carga de escenas, flujo, HUD y menús.
// Se crea desde Resources (SceneContext.EnsureSystems) y sobrevive a los cambios de escena.
[DefaultExecutionOrder(-150)]
public class GameSystems : MonoBehaviour
{
    public static GameSystems Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var module = GetComponentInChildren<InputSystemUIInputModule>();
        if (module != null && module.actionsAsset == null) module.AssignDefaultActions();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
