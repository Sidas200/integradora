using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MenuToggle : MonoBehaviour
{
    [SerializeField] public GameObject menu;              // Canvas del menú (se busca solo si lo dejas vacío)
    [SerializeField] public bool iniciarVisible = false;  // ¿el menú empieza abierto?
    [SerializeField] public bool pausarJuego = true;      // congela el juego mientras el menú está abierto

    bool cerrando;

    void Awake()
    {
        if (menu == null)
        {
            var intro = FindFirstObjectByType<MenuIntro>(FindObjectsInactive.Include);
            if (intro != null) menu = intro.gameObject;
        }
    }

    void Start()
    {
        if (menu == null) { Debug.LogWarning("MenuToggle: no encontré el Canvas del menú"); return; }
        Mostrar(iniciarVisible);
    }

    void Update()
    {
        if (menu != null && TeclaPresionada()) Alternar();
    }

    public void Alternar()
    {
        if (cerrando) return; // no hacer nada mientras se está cerrando

        if (menu.activeSelf) StartCoroutine(CerrarConAnimacion());
        else Mostrar(true);
    }

    IEnumerator CerrarConAnimacion()
    {
        cerrando = true;

        var intro = menu.GetComponent<MenuIntro>();
        float espera = intro != null ? intro.Salir() : 0f;
        yield return new WaitForSecondsRealtime(espera);

        Mostrar(false);
        cerrando = false;
    }

    public void Mostrar(bool visible)
    {
        menu.SetActive(visible);
        if (pausarJuego) Time.timeScale = visible ? 0f : 1f;
    }

    bool TeclaPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.escapeKey.wasPressedThisFrame || k.mKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M);
#endif
    }
}