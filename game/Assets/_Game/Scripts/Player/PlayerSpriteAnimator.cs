using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Caminata (8 frames cada una)")]
    public Sprite[] caminarDerecha;    // frames 16-23
    public Sprite[] caminarFrente;     // frames 24-31
    public Sprite[] caminarIzquierda;  // frames 32-39
    public Sprite[] caminarAtras;      // frames 40-47

    [Header("Ataque (16 frames cada uno)")]
    public Sprite[] ataqueDerecha;     // frames 0-15
    public Sprite[] ataqueIzquierda;   // frames 48-63

    [Header("Velocidad")]
    public float fpsCaminar = 10f;
    public float fpsAtaque = 20f;

    SpriteRenderer sr;
    Vector3 posAnterior;
    Sprite[] animActual;
    Sprite[] ultimaDireccion;
    float timer;
    int frame;
    bool atacando;
    bool mirandoDerecha = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        posAnterior = transform.position;
        ultimaDireccion = caminarFrente;
    }

    void Update()
    {
        // Detecta movimiento sin importar qué script mueve al personaje
        Vector2 mov = transform.position - posAnterior;
        posAnterior = transform.position;
        bool moviendo = mov.sqrMagnitude > 0.000001f;

        // Ataque
        if (!atacando && PresionoAtaque())
        {
            atacando = true;
            Cambiar(mirandoDerecha ? ataqueDerecha : ataqueIzquierda);
        }

        if (atacando)
        {
            Avanzar(fpsAtaque, false);
            return;
        }

        // Caminata
        if (moviendo)
        {
            Sprite[] dir;
            if (Mathf.Abs(mov.x) >= Mathf.Abs(mov.y))
            {
                mirandoDerecha = mov.x > 0;
                dir = mirandoDerecha ? caminarDerecha : caminarIzquierda;
            }
            else
            {
                dir = mov.y > 0 ? caminarAtras : caminarFrente;
            }

            ultimaDireccion = dir;
            Cambiar(dir);
            Avanzar(fpsCaminar, true);
        }
        else
        {
            // Quieto: primer frame de la última dirección
            animActual = null;
            sr.sprite = ultimaDireccion[0];
        }
    }

    void Cambiar(Sprite[] nueva)
    {
        if (nueva == animActual) return;
        animActual = nueva;
        frame = 0;
        timer = 0f;
        sr.sprite = animActual[0];
    }

    void Avanzar(float fps, bool loop)
    {
        timer += Time.deltaTime;
        if (timer < 1f / fps) return;
        timer -= 1f / fps;
        frame++;

        if (frame >= animActual.Length)
        {
            if (loop) frame = 0;
            else
            {
                atacando = false;
                animActual = null;
                return;
            }
        }
        sr.sprite = animActual[frame];
    }

    bool PresionoAtaque()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}