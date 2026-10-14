using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class PocketMapMenuBuilder
{
    // =========================================================
    // ASSET PATHS
    // =========================================================

    private const string PIXEL_MAP =
        "Sprites/Pixel Map";

    private const string SIDE_TABS =
        "Sprites/Content/Side Tabs";

    private const string HOLDERS =
        "Sprites/Content/Holders";

    private const string ICONS =
        "Sprites/Content/Icons";

    private const string ITEMS =
        "Sprites/Content/Items";

    private const string NEXT_FLIP =
        "Sprites/Page Flip/Next Page";

    private const string PREVIOUS_FLIP =
        "Sprites/Page Flip/Previous Page";

    private const string SPRITESHEET_FOLDER =
        "PNG SpriteSheet";

    // =========================================================
    // BUILD
    // =========================================================

    [MenuItem("Tools/Pocket Inventory/Build Pixel Map Menu")]
    public static void Build()
    {
        DeleteOldMenu();
        EnsureEventSystem();

        // =====================================================
        // CANVAS
        // =====================================================

        GameObject root =
            new GameObject(
                "PocketMapMenu",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        Canvas canvas =
            root.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 200;


        CanvasScaler scaler =
            root.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        // =====================================================
        // BACKGROUND
        // =====================================================

        GameObject dark =
            CreateImage(
                "ScreenDim",
                root.transform,
                null
            );

        Stretch(
            dark.GetComponent<RectTransform>()
        );

        dark.GetComponent<Image>().color =
            new Color(
                0f,
                0f,
                0f,
                0.72f
            );

        // =====================================================
        // VISUAL ROOT
        // =====================================================

        GameObject menu =
            CreateRect(
                "PixelMapRoot",
                root.transform
            );

        SetRect(
            menu.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(
                920,
                760
            )
        );

        // =====================================================
        // FULL MAP BASE
        // =====================================================

        Sprite fullMap =
            FindSprite(
                PIXEL_MAP,
                "0"
            );

        GameObject mapVisual =
            CreateImage(
                "PixelMapVisual",
                menu.transform,
                fullMap
            );

        SetRect(
            mapVisual.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(
                920,
                760
            )
        );

        // =====================================================
        // PAPER AREA
        // =====================================================

        GameObject paperArea =
            CreateRect(
                "PaperArea",
                menu.transform
            );

        SetRect(
            paperArea.GetComponent<RectTransform>(),
            new Vector2(
                108,
                12
            ),
            new Vector2(
                435,
                500
            )
        );

        // =====================================================
        // PAGES
        // =====================================================

        GameObject profilePage =
            CreatePage(
                "ProfilePage",
                paperArea.transform
            );

        GameObject inventoryPage =
            CreatePage(
                "InventoryPage",
                paperArea.transform
            );

        GameObject equipmentPage =
            CreatePage(
                "EquipmentPage",
                paperArea.transform
            );

        GameObject tasksPage =
            CreatePage(
                "TasksPage",
                paperArea.transform
            );

        GameObject savePage =
            CreatePage(
                "SavePage",
                paperArea.transform
            );

        GameObject settingsPage =
            CreatePage(
                "SettingsPage",
                paperArea.transform
            );


        BuildProfilePage(
            profilePage.transform
        );

        BuildInventoryPage(
            inventoryPage.transform
        );

        BuildEquipmentPage(
            equipmentPage.transform
        );

        BuildTasksPage(
            tasksPage.transform
        );

        BuildSavePage(
            savePage.transform
        );

        BuildSettingsPage(
            settingsPage.transform
        );

        // =====================================================
        // PAGE FLIP
        //
        // MUY IMPORTANTE:
        // ahora es hijo del PaperArea.
        // =====================================================

        GameObject pageFlip =
            CreateImage(
                "PageFlip",
                paperArea.transform,
                null
            );

        Image pageFlipImage =
            pageFlip.GetComponent<Image>();

        pageFlipImage.raycastTarget = false;

        // Que cubra TODA la página beige.
        Stretch(
            pageFlip.GetComponent<RectTransform>()
        );

        // Para que el sprite de la hoja pueda ocupar toda el área.
        pageFlipImage.preserveAspect = false;

        pageFlip.SetActive(false);


        Sprite[] nextFrames =
            LoadSpritesFromFolder(
                NEXT_FLIP
            );

        Sprite[] previousFrames =
            LoadSpritesFromFolder(
                PREVIOUS_FLIP
            );

        // =====================================================
        // ANIMACIÓN DE ABRIR/CERRAR LIBRETA
        // =====================================================

        GameObject notebookAnimation =
            CreateImage(
                "NotebookAnimation",
                menu.transform,
                null
            );

        Image notebookAnimationImage =
            notebookAnimation.GetComponent<Image>();

        notebookAnimationImage.raycastTarget = false;

        SetRect(
            notebookAnimation.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(
                920,
                760
            )
        );

        notebookAnimationImage.preserveAspect = true;

        notebookAnimation.SetActive(false);


        Sprite[] notebookCloseFrames =
            LoadNamedSprites(
                "Close_"
            );

        // =====================================================
        // CONTROLLER
        // =====================================================

        PocketMapController controller =
            root.AddComponent<PocketMapController>();

        GameObject[] pages =
        {
            profilePage,
            inventoryPage,
            equipmentPage,
            tasksPage,
            savePage,
            settingsPage
        };

        controller.Setup(
            pages,
            pageFlipImage,
            nextFrames,
            previousFrames,
            notebookAnimationImage,
            notebookCloseFrames
        );

        // =====================================================
        // TABS
        // =====================================================

        BuildTabs(
            menu.transform,
            controller
        );

        // =====================================================
        // TOGGLE
        // =====================================================

        SetupToggle(
            root
        );

        Selection.activeGameObject =
            root;

        Debug.Log(
            "Pocket Map generado correctamente."
        );
    }

    // =========================================================
    // PROFILE
    // =========================================================

    private static void BuildProfilePage(
        Transform parent
    )
    {
        CreateText(
            "ProfileTitle",
            parent,
            "PROFILE",
            28,
            new Vector2(0, 205),
            new Vector2(300, 50)
        );

        Sprite portraitFrame =
            FindSprite(
                HOLDERS,
                "8"
            );

        GameObject frame =
            CreateImage(
                "PortraitFrame",
                parent,
                portraitFrame
            );

        SetRect(
            frame.GetComponent<RectTransform>(),
            new Vector2(-95, 65),
            new Vector2(135, 135)
        );

        Sprite character =
            FindSprite(
                ICONS,
                "14"
            );

        GameObject characterGO =
            CreateImage(
                "Character",
                parent,
                character
            );

        SetRect(
            characterGO.GetComponent<RectTransform>(),
            new Vector2(-95, 65),
            new Vector2(82, 110)
        );

        CreateText(
            "PlayerName",
            parent,
            "PLAYER",
            18,
            new Vector2(-95, -25),
            new Vector2(180, 35)
        );

        CreateText(
            "Stats",
            parent,
            "LEVEL 01\n\n" +
            "HP 100 / 100\n\n" +
            "ATTACK 12\n\n" +
            "DEFENSE 8",
            17,
            new Vector2(95, 15),
            new Vector2(170, 260),
            TextAlignmentOptions.MidlineLeft
        );
    }

    // =========================================================
    // INVENTORY
    // =========================================================

    private static void BuildInventoryPage(
        Transform parent
    )
    {
        CreateText(
            "InventoryTitle",
            parent,
            "INVENTORY",
            28,
            new Vector2(0, 205),
            new Vector2(320, 50)
        );

        Sprite holder =
            FindSprite(
                HOLDERS,
                "0"
            );

        int item = 0;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Vector2 pos =
                    new Vector2(
                        -140 + x * 72,
                        95 - y * 72
                    );

                GameObject slot =
                    CreateImage(
                        $"InventorySlot_{x}_{y}",
                        parent,
                        holder
                    );

                SetRect(
                    slot.GetComponent<RectTransform>(),
                    pos,
                    new Vector2(58, 58)
                );

                if (item <= 11)
                {
                    Sprite itemSprite =
                        FindSprite(
                            ITEMS,
                            item.ToString()
                        );

                    if (itemSprite != null)
                    {
                        GameObject itemGO =
                            CreateImage(
                                "Item_" + item,
                                parent,
                                itemSprite
                            );

                        SetRect(
                            itemGO.GetComponent<RectTransform>(),
                            pos,
                            new Vector2(42, 42)
                        );
                    }

                    item++;
                }
            }
        }
    }

    // =========================================================
    // EQUIPMENT
    // =========================================================

    private static void BuildEquipmentPage(
        Transform parent
    )
    {
        CreateText(
            "EquipmentTitle",
            parent,
            "EQUIPMENT",
            28,
            new Vector2(0, 205),
            new Vector2(320, 50)
        );

        CreateText(
            "EquipmentText",
            parent,
            "WEAPON\n\n" +
            "ARMOR\n\n" +
            "ACCESSORY\n\n" +
            "SPECIAL",
            19,
            new Vector2(-85, 10),
            new Vector2(160, 250),
            TextAlignmentOptions.MidlineLeft
        );

        Sprite holder =
            FindSprite(
                HOLDERS,
                "8"
            );

        for (int i = 0; i < 4; i++)
        {
            Vector2 pos =
                new Vector2(
                    95,
                    95 - i * 95
                );

            GameObject slot =
                CreateImage(
                    "EquipmentSlot_" + i,
                    parent,
                    holder
                );

            SetRect(
                slot.GetComponent<RectTransform>(),
                pos,
                new Vector2(72, 72)
            );

            Sprite icon =
                FindSprite(
                    ICONS,
                    i.ToString()
                );

            if (icon != null)
            {
                GameObject iconGO =
                    CreateImage(
                        "EquipmentIcon_" + i,
                        parent,
                        icon
                    );

                SetRect(
                    iconGO.GetComponent<RectTransform>(),
                    pos,
                    new Vector2(46, 46)
                );
            }
        }
    }

    // =========================================================
    // TASKS
    // =========================================================

    private static void BuildTasksPage(
        Transform parent
    )
    {
        CreateText(
            "TasksTitle",
            parent,
            "TASKS",
            28,
            new Vector2(0, 205),
            new Vector2(300, 50)
        );

        CreateText(
            "TaskList",
            parent,
            "MAIN OBJECTIVE\n" +
            "Reach the Floating Ruins\n\n" +
            "SIDE OBJECTIVE\n" +
            "Collect 5 materials\n\n" +
            "PROMISE\n" +
            "Return to the Sanctuary",
            17,
            new Vector2(0, 10),
            new Vector2(320, 280),
            TextAlignmentOptions.MidlineLeft
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    private static void BuildSavePage(
        Transform parent
    )
    {
        CreateText(
            "SaveTitle",
            parent,
            "SAVE",
            28,
            new Vector2(0, 205),
            new Vector2(300, 50)
        );

        CreateText(
            "SaveText",
            parent,
            "SAVE SLOT 01\n\n" +
            "PLAY TIME 02:14:55\n\n" +
            "FLOATING RUINS",
            18,
            new Vector2(0, 20),
            new Vector2(300, 240)
        );
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private static void BuildSettingsPage(
        Transform parent
    )
    {
        CreateText(
            "SettingsTitle",
            parent,
            "SETTINGS",
            28,
            new Vector2(0, 205),
            new Vector2(300, 50)
        );

        string[] settings =
        {
            "MUSIC",
            "SFX",
            "CONTROLS",
            "VIDEO",
            "RETURN"
        };

        for (int i = 0; i < settings.Length; i++)
        {
            CreateText(
                "Setting_" + i,
                parent,
                settings[i],
                20,
                new Vector2(
                    0,
                    100 - i * 60
                ),
                new Vector2(250, 42)
            );
        }
    }

    // =========================================================
    // TABS
    // =========================================================

    private static void BuildTabs(
        Transform parent,
        PocketMapController controller
    )
    {
        GameObject tabs =
            CreateRect(
                "SideTabs",
                parent
            );

        SetRect(
            tabs.GetComponent<RectTransform>(),
            new Vector2(-490, 0),
            new Vector2(110, 540)
        );

        for (int i = 0; i < 6; i++)
        {
            int pageIndex = i;

            Sprite background =
                FindSprite(
                    SIDE_TABS,
                    i < 4
                        ? i.ToString()
                        : "3"
                );

            Sprite icon =
                FindSprite(
                    SIDE_TABS,
                    Mathf.Min(
                        4 + i,
                        8
                    ).ToString()
                );

            GameObject tab =
                new GameObject(
                    "Tab_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            tab.transform.SetParent(
                tabs.transform,
                false
            );

            SetRect(
                tab.GetComponent<RectTransform>(),
                new Vector2(
                    0,
                    200 - i * 80
                ),
                new Vector2(74, 74)
            );

            Image image =
                tab.GetComponent<Image>();

            image.sprite =
                background;

            image.preserveAspect =
                true;

            Button button =
                tab.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                () =>
                {
                    controller.OpenPage(
                        pageIndex
                    );
                }
            );

            GameObject iconGO =
                CreateImage(
                    "Icon",
                    tab.transform,
                    icon
                );

            SetRect(
                iconGO.GetComponent<RectTransform>(),
                Vector2.zero,
                new Vector2(38, 38)
            );
        }

        if (
            tabs.GetComponent<PocketTabsAnimator>()
            == null
        )
        {
            tabs.AddComponent<PocketTabsAnimator>();
        }

        tabs.transform.SetSiblingIndex(0);
    }

    // =========================================================
    // LOAD NOTEBOOK FRAMES
    // =========================================================

    private static Sprite[] LoadNamedSprites(
        string prefix
    )
    {
        string[] textureGuids =
            AssetDatabase.FindAssets(
                "SpriteSheet t:Texture2D",
                new[] { "Assets" }
            );

        List<Sprite> sprites =
            new List<Sprite>();

        foreach (string guid in textureGuids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(guid)
                    .Replace("\\", "/");

            if (!path.Contains(SPRITESHEET_FOLDER))
                continue;

            Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(
                    path
                );

            foreach (Object asset in assets)
            {
                Sprite sprite =
                    asset as Sprite;

                if (sprite == null)
                    continue;

                if (
                    sprite.name.StartsWith(prefix)
                )
                {
                    sprites.Add(sprite);
                }
            }
        }

        return sprites
            .OrderBy(
                sprite => sprite.name
            )
            .ToArray();
    }

    // =========================================================
    // LOAD PAGE FLIP FRAMES
    // =========================================================

    private static Sprite[] LoadSpritesFromFolder(
        string folder
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { "Assets" }
            );

        List<Sprite> sprites =
            new List<Sprite>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(guid)
                    .Replace("\\", "/");

            if (!path.Contains(folder))
                continue;

            Sprite sprite =
                AssetDatabase
                    .LoadAssetAtPath<Sprite>(
                        path
                    );

            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        return sprites
            .OrderBy(
                sprite =>
                {
                    int number;

                    if (
                        int.TryParse(
                            sprite.name,
                            out number
                        )
                    )
                    {
                        return number;
                    }

                    return 999;
                }
            )
            .ToArray();
    }

    // =========================================================
    // FIND SPRITE
    // =========================================================

    private static Sprite FindSprite(
        string folder,
        string spriteName
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                spriteName + " t:Sprite",
                new[] { "Assets" }
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(guid)
                    .Replace("\\", "/");

            if (
                path.Contains(folder) &&
                path.EndsWith(
                    "/" +
                    spriteName +
                    ".png"
                )
            )
            {
                return
                    AssetDatabase
                        .LoadAssetAtPath<Sprite>(
                            path
                        );
            }
        }

        return null;
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    private static void SetupToggle(
        GameObject menu
    )
    {
        GameObject manager =
            GameObject.Find(
                "PocketMapManager"
            );

        if (manager == null)
        {
            manager =
                new GameObject(
                    "PocketMapManager"
                );
        }

        PocketMapToggle toggle =
            manager.GetComponent<PocketMapToggle>();

        if (toggle == null)
        {
            toggle =
                manager.AddComponent<PocketMapToggle>();
        }

        toggle.SetMenu(
            menu
        );
    }

    // =========================================================
    // EVENT SYSTEM
    // =========================================================

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            Object.FindFirstObjectByType<EventSystem>();

        if (existing != null)
            return;

        GameObject eventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem)
            );

#if ENABLE_INPUT_SYSTEM

        eventSystem.AddComponent<
            InputSystemUIInputModule
        >();

#else

        eventSystem.AddComponent<
            StandaloneInputModule
        >();

#endif
    }

    // =========================================================
    // CREATE IMAGE
    // =========================================================

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Sprite sprite
    )
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        go.transform.SetParent(
            parent,
            false
        );

        Image image =
            go.GetComponent<Image>();

        image.sprite =
            sprite;

        image.color =
            Color.white;

        image.preserveAspect =
            true;

        image.raycastTarget =
            false;

        return go;
    }

    // =========================================================
    // CREATE RECT
    // =========================================================

    private static GameObject CreateRect(
        string name,
        Transform parent
    )
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        go.transform.SetParent(
            parent,
            false
        );

        return go;
    }

    // =========================================================
    // CREATE PAGE
    // =========================================================

    private static GameObject CreatePage(
        string name,
        Transform parent
    )
    {
        GameObject go =
            CreateRect(
                name,
                parent
            );

        Stretch(
            go.GetComponent<RectTransform>()
        );

        return go;
    }

    // =========================================================
    // CREATE TEXT
    // =========================================================

    private static GameObject CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        Vector2 position,
        Vector2 size
    )
    {
        return CreateText(
            name,
            parent,
            content,
            fontSize,
            position,
            size,
            TextAlignmentOptions.Center
        );
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment
    )
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

        go.transform.SetParent(
            parent,
            false
        );

        SetRect(
            go.GetComponent<RectTransform>(),
            position,
            size
        );

        TextMeshProUGUI text =
            go.GetComponent<TextMeshProUGUI>();

        text.text =
            content;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            new Color32(
                66,
                74,
                77,
                255
            );

        text.raycastTarget =
            false;

        return go;
    }

    // =========================================================
    // RECT HELPERS
    // =========================================================

    private static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;
    }

    private static void Stretch(
        RectTransform rect
    )
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    // =========================================================
    // DELETE OLD
    // =========================================================

    private static void DeleteOldMenu()
    {
        GameObject old =
            GameObject.Find(
                "PocketMapMenu"
            );

        if (old != null)
        {
            Object.DestroyImmediate(
                old
            );
        }
    }
}