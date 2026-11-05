using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Entrenamiento en la Isla de las Flores: al salir de la cabaña con la espada aparece en la explanada
// de arena de abajo un muñeco de práctica que no hace daño. Una tarjeta arriba explica paso a paso: moverse, atacar,
// combo, esquivar, curarse y guardar en el cristal (el corte a distancia llega después, con el Módulo
// de espada). Después el muñeco se va y llega un enemigo de verdad;
// hasta vencerlo el puente sigue cerrado (FlagGate con DoneFlag).
// Si Tsuki muere en la pelea real, al volver se salta la práctica (BasicsFlag) y repite solo la pelea.
public class CombatTutorial : MonoBehaviour
{
    public const string DoneFlag = "tutorial_done";
    public const string BasicsFlag = "tutorial_basics";
    private const int TotalSteps = 7;

    [SerializeField] private GameObject dummyPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private MaterialData potion;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform cabinDoor;
    [Tooltip("Cristal de guardado de la isla (paso de guardar).")]
    [SerializeField] private Transform saveCrystal;
    [Tooltip("Qué tan cerca de la puerta de la cabaña tiene que estar Tsuki (afuera) para empezar.")]
    [SerializeField] private float startRadius = 1.6f;

    [Header("Pasos")]
    [SerializeField, Min(1)] private int hitsNeeded = 3;

    [Header("Ataque de práctica (esquivar)")]
    [SerializeField] private float dodgeWarning = 1.1f;
    [SerializeField] private float dodgeRadius = 0.32f;
    [SerializeField, Min(1)] private int dodgesNeeded = 2;

    private PlayerController tsuki;
    private Health health;
    private Inventory inventory;
    private GameObject dummy;
    private EnemyHealth dummyHealth;

    // Tarjeta
    private GameObject ui;
    private CanvasGroup card;
    private TextMeshProUGUI stepLabel, titleLabel, bodyLabel;
    private Image cardTint;
    private string rawBody;
    private float success;

    public void Setup(GameObject dummyEnemy, GameObject realEnemy, MaterialData potionData, Sprite circle, Transform spawn, Transform door, Transform crystal)
    {
        saveCrystal = crystal;
        dummyPrefab = dummyEnemy;
        enemyPrefab = realEnemy;
        potion = potionData;
        circleSprite = circle;
        spawnPoint = spawn;
        cabinDoor = door;
    }

    private IEnumerator Start()
    {
        // Ya lo hizo, o la isla ya se desvaneció en una partida anterior
        if (StoryFlags.Has(DoneFlag) || StoryFlags.Has(IslandVanish.Flag)) yield break;

        var player = SceneContext.FindPlayer();
        while (player == null) { yield return null; player = SceneContext.FindPlayer(); }
        tsuki = player.GetComponent<PlayerController>();
        health = player.GetComponent<Health>();
        inventory = player.GetComponent<Inventory>();

        // Espera a que tenga la espada y salga de la cabaña
        while (!StoryFlags.Has(SwordPickup.Flag) || GameInput.Locked ||
               Vector2.Distance(player.transform.position, cabinDoor.position) > startRadius)
            yield return null;

        BuildUi();
        if (GameFlow.Instance != null) GameFlow.Instance.Saved += OnSaved;
        if (!StoryFlags.Has(BasicsFlag))
        {
            SpawnDummy();
            yield return Step(1, "Muévete", "Camina con {move} hasta el muñeco de práctica,\nen la explanada de abajo.", WaitReach());
            yield return Step(2, "Ataca", "Golpea al muñeco con {attack}. No te hará daño.", WaitHits());
            yield return Step(3, "Combo", "Presiona {attack} dos veces seguidas: el segundo golpe empuja y aturde.", WaitCombo());
            yield return Step(4, "Esquiva", "El muñeco marcará el suelo en rojo.\nPresiona {dash} justo antes del golpe para esquivarlo.", WaitDodges());
            yield return Step(5, "Cúrate", "Toma una Poción y presiona {potion} para recuperar vida.", WaitHeal());
            yield return Step(6, "Guarda tu avance", "Acércate al cristal de guardado: guarda la partida y te cura.\nSi caes en combate, vuelves aquí. Con {interact} guardas otra vez.", WaitSave());
            StoryFlags.Set(BasicsFlag);
        }
        yield return Step(7, "¡Pelea de verdad!", "Este enemigo sí hace daño. Usa todo lo que aprendiste y derrótalo.", RealFight());

        StoryFlags.Set(DoneFlag);
        Show(0, "¡Entrenamiento completo!", "Ya puedes cruzar el puente.");
        success = 1f;
        yield return new WaitForSeconds(3.5f);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { card.alpha = 1f - t / 0.5f; yield return null; }
        Destroy(ui);
    }

    // ---------------------------------------------------------------- pasos

    private IEnumerator Step(int index, string title, string body, IEnumerator wait)
    {
        Show(index, title, body);
        yield return wait;
        success = 1f;
        if (index < TotalSteps)
        {
            bodyLabel.text = "<color=#9EF0B0>¡Bien hecho!</color>";
            rawBody = null;
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator WaitReach()
    {
        while (Vector2.Distance(tsuki.transform.position, dummy.transform.position) > 0.7f)
            yield return null;
    }

    private IEnumerator WaitHits()
    {
        int hits = 0;
        Action<bool> onHit = _ => hits++;
        tsuki.HitLanded += onHit;
        while (hits < hitsNeeded)
        {
            SetProgress(hits, hitsNeeded);
            yield return null;
        }
        tsuki.HitLanded -= onHit;
    }

    private IEnumerator WaitCombo()
    {
        bool done = false;
        Action<bool> onHit = finisher => { if (finisher) done = true; };
        tsuki.HitLanded += onHit;
        while (!done) yield return null;
        tsuki.HitLanded -= onHit;
    }

    // El muñeco marca un círculo donde está Tsuki y golpea al final. No hace daño: cuenta si lo
    // esquiva con dash (esquiva perfecta) o si se sale a tiempo.
    private IEnumerator WaitDodges()
    {
        var warning = Telegraph.Create(circleSprite, "TutorialWarning");
        int dodged = 0;
        while (dodged < dodgesNeeded)
        {
            SetProgress(dodged, dodgesNeeded);
            yield return new WaitForSeconds(0.9f);
            Vector2 center = tsuki.transform.position;
            warning.ShowCircle(center, dodgeRadius, dodgeWarning, Telegraph.Danger);
            yield return Wiggle(dodgeWarning);
            warning.Impact(0.2f);
            CameraShake.Impact(0.02f, 0.3f);

            if (tsuki.IsDodging)
            {
                tsuki.PerfectDodgeFeedback();
                dodged++;
                Flash("<color=#9EF0B0>¡Esquiva perfecta!</color>");
            }
            else if (!CombatUtil.InsideIsoCircle(tsuki.transform.position, center, dodgeRadius))
            {
                dodged++;
                Flash("<color=#9EF0B0>¡Bien, te saliste del círculo!</color>");
            }
            else Flash("<color=#FF9AA8>Te alcanzó (no hace daño). ¡Otra vez!</color>");
            yield return new WaitForSeconds(1f);
        }
        Destroy(warning.gameObject);
    }

    private IEnumerator WaitHeal()
    {
        // El muñeco "le quita" vida para que tenga qué curar (espera a que el golpe pueda entrar)
        int target = Mathf.RoundToInt(health.MaxHealth * 0.55f);
        while (health.CurrentHealth > target)
        {
            while (tsuki.IsDodging || health.IsInvulnerable) yield return null;
            health.TakeDamage(health.CurrentHealth - target + health.Defense);
            yield return null;
        }
        if (potion != null && inventory != null && inventory.GetAmount(potion.id) <= 0) inventory.Add(potion, 1);
        int before = health.CurrentHealth;
        while (health.CurrentHealth <= before) yield return null;
    }

    // Cuenta un guardado hecho durante este paso. El cristal guarda solo la primera vez que se acerca;
    // si ya lo hizo antes (pasó cerca), aquí aprende a guardar con el botón de interactuar.
    private IEnumerator WaitSave()
    {
        savedDuringTutorial = false;
        if (GameFlow.Instance == null)
        {
            // Escena de prueba sin GameFlow: basta con llegar al cristal
            while (saveCrystal != null && Vector2.Distance(tsuki.transform.position, saveCrystal.position) > 0.7f) yield return null;
            yield break;
        }
        while (!savedDuringTutorial) yield return null;
    }

    private bool savedDuringTutorial;
    private void OnSaved(string id) => savedDuringTutorial = true;

    private void OnDisable()
    {
        if (GameFlow.Instance != null) GameFlow.Instance.Saved -= OnSaved;
    }

    private IEnumerator RealFight()
    {
        if (dummy != null)
        {
            Poof(dummy.transform.position);
            Destroy(dummy);
        }
        yield return new WaitForSeconds(0.6f);
        var enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemy.name = "Tutorial_Enemy";
        if (enemy.TryGetComponent<PersistentId>(out var id)) id.SetId(gameObject.scene.name + "/Tutorial_Enemy");
        Poof(enemy.transform.position);
        var enemyHealth = enemy.GetComponent<EnemyHealth>();
        while (enemyHealth != null && !enemyHealth.IsDead) yield return null;
    }

    // ---------------------------------------------------------------- muñeco

    private void SpawnDummy()
    {
        dummy = Instantiate(dummyPrefab, spawnPoint.position, Quaternion.identity);
        dummy.name = "Tutorial_Dummy";
        if (dummy.TryGetComponent<PersistentId>(out var id)) id.SetId(gameObject.scene.name + "/Tutorial_Dummy");
        // Quieto y sin atacar: sin IA y sin empujones
        foreach (var ai in dummy.GetComponentsInChildren<MonoBehaviour>())
            if (ai is EnemyMeleeController || ai is EnemyRangedController || ai is SlimeController) ai.enabled = false;
        if (dummy.TryGetComponent<Rigidbody2D>(out var rb)) rb.bodyType = RigidbodyType2D.Kinematic;
        foreach (var sr in dummy.GetComponentsInChildren<SpriteRenderer>())
            if (sr.sprite != null && !sr.name.Contains("Shadow")) sr.color = new Color(0.78f, 0.86f, 1f, 1f);
        dummyHealth = dummy.GetComponent<EnemyHealth>();
        // Nunca se cae: al quedarse con poca vida se recupera
        dummyHealth.HealthChanged += (current, max) => { if (current <= max * 0.3f) dummyHealth.Revive(); };
        dummyLabel = Label("Muñeco de práctica");
        Poof(dummy.transform.position);
    }

    // El muñeco tiembla mientras se prepara para golpear
    private IEnumerator Wiggle(float time)
    {
        var visual = dummy != null ? dummy.GetComponentInChildren<SpriteRenderer>() : null;
        Vector3 basePos = visual != null ? visual.transform.localPosition : Vector3.zero;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (visual != null) visual.transform.localPosition = basePos + new Vector3(Mathf.Sin(t * 50f) * 0.01f, 0f, 0f);
            yield return null;
        }
        if (visual != null) visual.transform.localPosition = basePos;
    }

    // Suelto (no hijo del muñeco): dentro de su grupo de orden quedaría detrás de los árboles
    private Transform dummyLabel;
    private const float LabelHeight = 0.5f;
    private SpriteRenderer dummyVisual;

    private static Transform Label(string text)
    {
        var go = new GameObject("DummyLabel");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 1.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.97f, 0.84f, 0.55f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(3f, 0.4f);
        tmp.sortingLayerID = SortingLayer.NameToID("UI");
        // Contorno oscuro para que se lea sobre el pasto (material propio de este texto)
        tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineColor = new Color32(30, 10, 24, 255);
        tmp.outlineWidth = 0.25f;
        return go.transform;
    }

    // Nube de chispas al aparecer y desaparecer
    private void Poof(Vector2 at)
    {
        CameraShake.Shake(0.015f, 0.12f);
        StartCoroutine(PoofRoutine(at));
    }

    private IEnumerator PoofRoutine(Vector2 at)
    {
        var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        var dot = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        var parts = new SpriteRenderer[14];
        var vel = new Vector2[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = new GameObject("Poof").AddComponent<SpriteRenderer>();
            parts[i].sprite = dot;
            parts[i].sortingLayerName = "UI";
            parts[i].sortingOrder = -15;
            parts[i].transform.position = at + Vector2.up * 0.2f;
            parts[i].transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 2.2f);
            parts[i].color = i % 2 == 0 ? new Color(1f, 0.9f, 0.6f) : Color.white;
            float a = i / (float)parts.Length * Mathf.PI * 2f;
            vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * UnityEngine.Random.Range(0.5f, 0.9f);
        }
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].transform.position += (Vector3)(vel[i] * Time.deltaTime);
                var c = parts[i].color;
                c.a = 1f - t / 0.45f;
                parts[i].color = c;
            }
            yield return null;
        }
        foreach (var p in parts) Destroy(p.gameObject);
    }

    // ---------------------------------------------------------------- tarjeta

    private void Show(int index, string title, string body)
    {
        stepLabel.text = index > 0 ? $"ENTRENAMIENTO  ·  PASO {index} DE {TotalSteps}" : "ENTRENAMIENTO";
        titleLabel.text = title;
        rawBody = body;
        bodyLabel.text = ControlHints.Format(body);
    }

    private void SetProgress(int have, int need)
    {
        if (rawBody == null) return;
        string text = ControlHints.Format(rawBody) + $"   <color=#F6D58E>{have}/{need}</color>";
        if (bodyLabel.text != text) bodyLabel.text = text;
    }

    private void Flash(string message)
    {
        bodyLabel.text = message;
        rawBody = null;
        StartCoroutine(RestoreBody(ControlHints.Format("El muñeco marcará el suelo en rojo.\nPresiona {dash} justo antes del golpe para esquivarlo."), 0.9f));
    }

    private IEnumerator RestoreBody(string text, float after)
    {
        yield return new WaitForSeconds(after);
        if (rawBody == null && bodyLabel != null) bodyLabel.text = text;
    }

    private void LateUpdate()
    {
        if (dummyLabel == null) return;
        if (dummy == null) { Destroy(dummyLabel.gameObject); return; }
        // Sobre la cabeza del sprite "Visual" (los demás son efectos de ataque y la sombra)
        if (dummyVisual == null)
        {
            var visual = dummy.transform.Find("Visual");
            dummyVisual = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        }
        dummyLabel.position = dummyVisual != null
            ? new Vector3(dummyVisual.bounds.center.x, dummyVisual.bounds.max.y + 0.06f, 0f)
            : dummy.transform.position + new Vector3(0f, LabelHeight, 0f);
    }

    private void Update()
    {
        if (card == null) return;
        // Debajo del cuadro de diálogo no se ve: se esconde mientras hay uno abierto
        bool dialogue = DialogueUI.Instance != null && DialogueUI.Instance.IsOpen;
        card.alpha = Mathf.MoveTowards(card.alpha, dialogue ? 0f : 1f, Time.unscaledDeltaTime * 6f);
        success = Mathf.MoveTowards(success, 0f, Time.unscaledDeltaTime * 2f);
        if (cardTint != null) cardTint.color = new Color(0.5f, 1f, 0.6f, success * 0.25f);
    }

    private void OnDestroy()
    {
        if (ui != null) Destroy(ui);
    }

    // Como el HUD: panel oscuro con marco dorado, arriba al centro
    private void BuildUi()
    {
        ui = new GameObject("CombatTutorialUI", typeof(RectTransform));
        var canvas = ui.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Debajo de la UI de GameSystems (50): el diálogo y la pausa quedan encima
        canvas.sortingOrder = 44;
        var scaler = ui.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var box = (RectTransform)new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup)).transform;
        box.SetParent(ui.transform, false);
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 1f);
        // Debajo de los paneles de vida y objetivo, para no taparlos
        box.anchoredPosition = new Vector2(0f, -205f);
        box.sizeDelta = new Vector2(760f, 158f);
        card = box.GetComponent<CanvasGroup>();
        card.blocksRaycasts = false;
        card.alpha = 0f;
        UiSkin.ThemePanel(box);

        cardTint = new GameObject("SuccessTint", typeof(RectTransform)).AddComponent<Image>();
        cardTint.rectTransform.SetParent(box, false);
        cardTint.rectTransform.anchorMin = Vector2.zero;
        cardTint.rectTransform.anchorMax = Vector2.one;
        cardTint.rectTransform.offsetMin = new Vector2(8f, 8f);
        cardTint.rectTransform.offsetMax = new Vector2(-8f, -8f);
        cardTint.raycastTarget = false;
        cardTint.color = Color.clear;

        var font = HudFont();
        stepLabel = Text(box, font, 20f, -16f, 28f, UiArt.TextMuted);
        titleLabel = Text(box, font, 34f, -40f, 44f, Color.white);
        titleLabel.enableVertexGradient = true;
        titleLabel.colorGradient = new VertexGradient(
            new Color(1f, 0.93f, 0.70f), new Color(1f, 0.93f, 0.70f),
            new Color(0.90f, 0.62f, 0.32f), new Color(0.90f, 0.62f, 0.32f));
        bodyLabel = Text(box, font, 25f, -86f, 66f, UiArt.TextCream);
        bodyLabel.fontStyle = FontStyles.Normal;
    }

    // top: borde de arriba del texto, medido desde arriba de la tarjeta
    private static TextMeshProUGUI Text(RectTransform parent, TMP_FontAsset font, float size, float top, float height, Color color)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        var rt = t.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(700f, height);
        if (font != null) t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Top;
        t.raycastTarget = false;
        return t;
    }

    private static TMP_FontAsset HudFont()
    {
        var hud = GameHud.Instance;
        var label = hud != null ? UiNav.FindLabel(hud.transform, "HealthText") : null;
        return label != null ? label.font : null;
    }
}
