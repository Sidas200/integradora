using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// La doncella que vive en la espada (Aoi, después de su prueba). Con H / R3 Tsuki la llama en
// cualquier momento: levanta la espada, Aoi sale de la punta y flota a su lado mientras hablan
// (el diálogo tiene una decisión: qué hacer ahora, consejo de combate o hablar de ella; sus
// respuestas cambian según cómo va la historia). Al terminar vuelve a la espada.
// Va en el prefab de Tsuki. También guarda el diálogo de cuando Aoi entra a la espada (MaidenStatue).
public class SwordCompanion : MonoBehaviour
{
    [Tooltip("Lo que pasa al llamarla (H / R3).")]
    [SerializeField] private DialogueData adviceDialogue;
    [Tooltip("Tsuki y Aoi hablan después de que Aoi entra a la espada y del Paso de Rayo de demostración.")]
    [SerializeField] private DialogueData joinDialogue;
    [Tooltip("Sprite de Aoi cuando sale de la espada (pivote en los pies).")]
    [SerializeField] private Sprite spiritSprite;
    [Tooltip("Dónde flota Aoi, al lado de Tsuki (x hacia donde mira, en unidades).")]
    [SerializeField] private Vector2 spiritOffset = new Vector2(0.34f, 0.04f);

    private const float OutTime = 0.55f;
    private const float BackTime = 0.4f;

    private PlayerController player;
    private SpriteRenderer playerSr;
    private bool talking;

    // Hay alguien en la espada con quien hablar
    public static bool Available => StoryFlags.Has(MaidenStatue.Flag);

    public DialogueData JoinDialogue => joinDialogue;

    public void Setup(DialogueData advice, DialogueData join, Sprite spirit)
    {
        adviceDialogue = advice;
        joinDialogue = join;
        spiritSprite = spirit;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerSr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (talking || adviceDialogue == null || !Available || GameInput.GameplayBlocked) return;
        // Evita abrirlo con la misma tecla con la que se cerró otro diálogo
        if (Time.unscaledTime - DialogueUI.LastClosedTime < 0.2f) return;
        if (!Pressed()) return;
        StartCoroutine(Talk());
    }

    private static bool Pressed() =>
        (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) ||
        GamepadInput.RightStickPressed;

    private IEnumerator Talk()
    {
        talking = true;
        GameInput.Lock(this);
        player.StopActions();
        bool right = player.FacingDirection.x >= 0f;

        // Levanta la espada y Aoi sale de la punta hasta quedar flotando a su lado
        player.HoldSlashPose(right, 0);
        yield return new WaitForSeconds(0.2f);
        var spirit = CreateSpirit(out var visual);
        Vector3 spot = SpiritSpot(right);
        player.LightningBurst(3, 0.35f);
        for (float t = 0f; t < OutTime; t += Time.deltaTime)
        {
            float e = Smooth(t / OutTime);
            spirit.transform.position = Vector3.Lerp(SwordTip(right), spot, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.12f;
            spirit.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, e);
            visual.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.3f, 1f, e));
            yield return null;
        }
        spirit.transform.position = spot;
        spirit.transform.localScale = Vector3.one;
        visual.color = Color.white;
        visual.gameObject.AddComponent<FloatBob>();
        player.HoldSlashPose(right, -1);
        yield return new WaitForSeconds(0.15f);

        // Hablan
        GameInput.Unlock(this);
        bool done = false;
        DialogueUI.Play(adviceDialogue, () => done = true);
        // También termina si el diálogo se cancela sin su aviso (p. ej. al cambiar de escena)
        while (!done && DialogueUI.Instance != null && DialogueUI.Instance.IsOpen) yield return null;

        // Vuelve a la espada
        GameInput.Lock(this);
        player.HoldSlashPose(right, 0);
        yield return new WaitForSeconds(0.15f);
        Vector3 from = spirit.transform.position;
        for (float t = 0f; t < BackTime; t += Time.deltaTime)
        {
            float e = Smooth(t / BackTime);
            spirit.transform.position = Vector3.Lerp(from, SwordTip(right), e);
            spirit.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, e);
            visual.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.4f, e));
            yield return null;
        }
        Destroy(spirit);
        player.LightningBurst(4, 0.4f);
        yield return new WaitForSeconds(0.2f);
        player.HoldSlashPose(right, -1);
        GameInput.Unlock(this);
        talking = false;
    }

    // Aoi: un objeto raíz (se mueve y escala) con el sprite en un hijo (flota)
    private GameObject CreateSpirit(out SpriteRenderer visual)
    {
        var root = new GameObject("Aoi_FromSword");
        var child = new GameObject("Visual");
        child.transform.SetParent(root.transform, false);
        visual = child.AddComponent<SpriteRenderer>();
        visual.sprite = spiritSprite;
        if (playerSr != null)
        {
            visual.sharedMaterial = playerSr.sharedMaterial;
            visual.sortingLayerName = playerSr.sortingLayerName;
            visual.sortingOrder = playerSr.sortingOrder;
        }
        return root;
    }

    // Al lado de Tsuki, hacia donde mira (con los pies a la altura de los suyos)
    private Vector3 SpiritSpot(bool right) =>
        transform.position + new Vector3(right ? spiritOffset.x : -spiritOffset.x, spiritOffset.y, 0f);

    // Punta de la espada en alto (arriba del sprite, hacia donde mira)
    private Vector3 SwordTip(bool right)
    {
        if (playerSr == null) return transform.position;
        var b = playerSr.bounds;
        return new Vector3(b.center.x + (right ? 0.25f : -0.25f) * b.extents.x, b.max.y - b.size.y * 0.1f, transform.position.z);
    }

    private static float Smooth(float k) => k * k * (3f - 2f * k);

    private void OnDisable()
    {
        // Si se destruye a media escena (cambio de escena), que no deje el juego bloqueado
        if (talking) GameInput.Unlock(this);
        talking = false;
    }
}
