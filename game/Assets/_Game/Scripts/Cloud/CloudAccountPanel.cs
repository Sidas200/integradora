using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cuenta en el menú principal: estado de la sesión, iniciar y cerrar sesión.
// Se construye por código (CloudSync la crea al cargar el menú) para no depender de la escena
// generada por el builder del capítulo. La cuenta se crea en la web o en la app móvil.
public class CloudAccountPanel : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.10f, 1f);
    private static readonly Color DimColor = new Color(0.02f, 0.02f, 0.05f, 0.8f);
    private static readonly Color Crimson = new Color(0.72f, 0.14f, 0.24f, 1f);
    private static readonly Color TextColor = new Color(0.95f, 0.93f, 0.90f, 1f);
    private static readonly Color MutedText = new Color(0.72f, 0.70f, 0.78f, 1f);
    private static readonly Color FieldColor = new Color(0.12f, 0.10f, 0.18f, 1f);
    private static readonly Color ErrorText = new Color(1f, 0.55f, 0.6f, 1f);

    private TMP_Text accountText;
    private TMP_Text statusText;
    private Button accountButton;
    private TMP_Text accountButtonLabel;

    private GameObject loginPanel;
    private TMP_InputField emailField;
    private TMP_InputField passwordField;
    private TMP_Text loginMessage;
    private Button submitButton;
    private bool busy;

    public bool LoginOpen => loginPanel != null && loginPanel.activeSelf;

    public static CloudAccountPanel Create(Transform sceneRoot)
    {
        var go = new GameObject("CloudAccountCanvas", typeof(RectTransform));
        if (sceneRoot != null) go.transform.SetParent(sceneRoot, false);
        var panel = go.AddComponent<CloudAccountPanel>();
        panel.Build();
        return panel;
    }

    private void OnEnable()
    {
        if (CloudSync.Instance == null) return;
        CloudSync.Instance.StatusChanged += Refresh;
        CloudSync.Instance.Session.Changed += Refresh;
    }

    private void OnDisable()
    {
        if (CloudSync.Instance == null) return;
        CloudSync.Instance.StatusChanged -= Refresh;
        CloudSync.Instance.Session.Changed -= Refresh;
    }

    private void Update()
    {
        if (!LoginOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame) CloseLogin();
        else if (kb.tabKey.wasPressedThisFrame) Focus(emailField.isFocused ? passwordField : emailField);
        else if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && !busy) Submit();
    }

    // ---------------------------------------------------------------- acciones

    private void OnAccountButton()
    {
        var sync = CloudSync.Instance;
        if (sync == null || busy) return;
        if (sync.Session.IsSignedIn) Logout();
        else OpenLogin();
    }

    private async void Logout()
    {
        busy = true;
        accountButton.interactable = false;
        try { await CloudSync.Instance.Logout(); }
        finally
        {
            busy = false;
            if (this != null) Refresh();
        }
    }

    private void OpenLogin()
    {
        loginPanel.SetActive(true);
        loginMessage.text = "";
        loginMessage.color = MutedText;
        passwordField.text = "";
        Focus(string.IsNullOrEmpty(emailField.text) ? emailField : passwordField);
    }

    private void CloseLogin()
    {
        if (busy) return;
        loginPanel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(accountButton.gameObject);
    }

    private async void Submit()
    {
        string email = emailField.text.Trim();
        string password = passwordField.text;
        if (email.Length == 0 || password.Length == 0)
        {
            ShowError("Escribe tu correo y tu contraseña.");
            return;
        }

        busy = true;
        submitButton.interactable = false;
        loginMessage.color = MutedText;
        loginMessage.text = "Iniciando sesión…";
        string error;
        try { error = await CloudSync.Instance.Login(email, password); }
        finally
        {
            busy = false;
            if (submitButton != null) submitButton.interactable = true;
        }
        if (this == null) return;

        if (error != null)
        {
            ShowError(error);
            passwordField.text = "";
            Focus(passwordField);
            return;
        }
        passwordField.text = "";
        CloseLogin();
        Refresh();
    }

    private void ShowError(string message)
    {
        loginMessage.color = ErrorText;
        loginMessage.text = message;
    }

    private void Refresh()
    {
        var sync = CloudSync.Instance;
        if (sync == null || accountText == null) return;
        var session = sync.Session;
        accountText.text = session.IsSignedIn ? $"Cuenta: <b>{session.Player.Username}</b>" : "Sin cuenta conectada";
        statusText.text = sync.Status;
        accountButtonLabel.text = session.IsSignedIn ? "Cerrar sesión" : "Iniciar sesión";
        accountButton.interactable = !busy && session.State != CloudSessionState.Restoring;
    }

    private static void Focus(TMP_InputField field)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(field.gameObject);
        field.ActivateInputField();
    }

    // ---------------------------------------------------------------- construcción

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // sobre el menú (10), bajo el fade de GameSystems (50)
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var root = (RectTransform)transform;

        // Tarjeta de cuenta (arriba a la derecha)
        var card = Box(root, "AccountCard", new Vector2(1, 1), new Vector2(-40, -36), new Vector2(560, 200));
        Image(card, PanelColor);
        accountText = Label(card, "Account", "", 30, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(22, -16), new Vector2(516, 40), TextColor);
        statusText = Label(card, "Status", "", 22, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(22, -58), new Vector2(516, 60), MutedText);
        accountButton = Button(card, "AccountButton", "Iniciar sesión", new Vector2(0, 0), new Vector2(22, 16), new Vector2(300, 56), 26);
        accountButtonLabel = accountButton.GetComponentInChildren<TMP_Text>();
        accountButton.onClick.AddListener(OnAccountButton);

        // Ventana de inicio de sesión
        var dim = Stretch(root, "Login");
        Image(dim, DimColor).raycastTarget = true;
        loginPanel = dim.gameObject;
        var box = Box(dim, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 640));
        Image(box, PanelColor);
        Label(box, "Title", "Iniciar sesión", 56, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(700, 76), TextColor);
        Label(box, "Subtitle", "Usa la cuenta que creaste en la web o en la app.", 26, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(720, 40), MutedText);

        Label(box, "EmailLabel", "Correo", 26, TextAlignmentOptions.Left, new Vector2(0.5f, 1), new Vector2(0, -172), new Vector2(640, 34), MutedText);
        emailField = Field(box, "Email", "tu@correo.com", new Vector2(0, -210), TMP_InputField.ContentType.EmailAddress);
        Label(box, "PasswordLabel", "Contraseña", 26, TextAlignmentOptions.Left, new Vector2(0.5f, 1), new Vector2(0, -296), new Vector2(640, 34), MutedText);
        passwordField = Field(box, "Password", "", new Vector2(0, -334), TMP_InputField.ContentType.Password);

        loginMessage = Label(box, "Message", "", 26, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(720, 70), MutedText);

        submitButton = Button(box, "Submit", "Entrar", new Vector2(0.5f, 0), new Vector2(-170, 40), new Vector2(300, 74), 32);
        submitButton.onClick.AddListener(() => { if (!busy) Submit(); });
        var cancel = Button(box, "Cancel", "Cancelar", new Vector2(0.5f, 0), new Vector2(170, 40), new Vector2(300, 74), 32);
        cancel.onClick.AddListener(CloseLogin);

        loginPanel.SetActive(false);
        Refresh();
    }

    private TMP_InputField Field(RectTransform parent, string name, string placeholder, Vector2 pos, TMP_InputField.ContentType type)
    {
        var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(640, 70);

        var bg = go.GetComponent<Image>();
        bg.color = FieldColor;

        var field = go.GetComponent<TMP_InputField>();
        field.contentType = type;
        field.characterLimit = type == TMP_InputField.ContentType.Password ? 72 : 254;
        field.textComponent.fontSize = 30;
        field.textComponent.color = TextColor;
        field.caretColor = TextColor;
        field.customCaretColor = true;
        field.selectionColor = new Color(Crimson.r, Crimson.g, Crimson.b, 0.5f);
        if (field.placeholder is TMP_Text ph)
        {
            ph.text = placeholder;
            ph.fontSize = 30;
            ph.color = new Color(MutedText.r, MutedText.g, MutedText.b, 0.5f);
        }
        return field;
    }

    private Button Button(RectTransform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
    {
        var rt = Box(parent, name, anchor, pos, size);
        var img = Image(rt, Color.white);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.13f, 0.22f, 0.96f);
        colors.highlightedColor = Crimson;
        colors.selectedColor = Crimson;
        colors.pressedColor = new Color(0.45f, 0.08f, 0.15f, 1f);
        colors.disabledColor = new Color(0.12f, 0.12f, 0.14f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        var text = Text(Stretch(rt, "Label"), label, fontSize, TextAlignmentOptions.Center, TextColor);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private Image Image(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text Label(RectTransform parent, string name, string text, float size, TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 boxSize, Color color) =>
        Text(Box(parent, name, anchor, pos, boxSize), text, size, align, color);

    private static TMP_Text Text(RectTransform rt, string text, float size, TextAlignmentOptions align, Color color)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static RectTransform Stretch(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform Box(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }
}
