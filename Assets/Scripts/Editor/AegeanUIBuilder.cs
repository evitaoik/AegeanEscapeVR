using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds the Aegean themed VR UI (World Space menus + timer HUD) in the open scene.
// Menu: Aegean Escape > Build VR UI. Safe to run again, it replaces the previous build.
public static class AegeanUIBuilder
{
    const string SpriteFolder = "Assets/UI/Generated";
    const int UILayer = 5;

    // Theme
    static readonly Color Marble = new Color(0.975f, 0.962f, 0.930f);
    static readonly Color MarbleShade = new Color(0.905f, 0.925f, 0.955f);
    static readonly Color Blue = new Color(0.035f, 0.200f, 0.400f);
    static readonly Color BlueHover = new Color(0.090f, 0.330f, 0.600f);
    static readonly Color BluePressed = new Color(0.020f, 0.130f, 0.280f);
    static readonly Color Gold = new Color(0.810f, 0.650f, 0.270f);
    static readonly Color GoldLight = new Color(0.960f, 0.820f, 0.450f);
    static readonly Color Muted = new Color(0.270f, 0.350f, 0.470f);
    static readonly Color ShadowColor = new Color(0.010f, 0.050f, 0.120f, 0.45f);

    static Sprite rounded, ring, shadow, meander;
    static TMP_FontAsset titleFont, bodyFont;
    static MenuManager menu;

    [MenuItem("Aegean Escape/Build VR UI")]
    public static void Build()
    {
        Undo.SetCurrentGroupName("Build Aegean VR UI");
        int undoGroup = Undo.GetCurrentGroup();

        CreateSprites();

        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/fonts/Cinzel-SemiBold SDF.asset");
        bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        AudioClip click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sounds/click.mp3");

        Camera cam = Camera.main;
        Transform head = cam != null ? cam.transform : null;

        // Remove the old Screen Space UI and any previous build (inactive objects too)
        var oldNames = new System.Collections.Generic.HashSet<string> { "StartCanvas", "GameplayUI", "EndCanvas", "StartMenuManager", "UIManager", "MenuCanvas", "TimerHUD" };

        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (oldNames.Contains(root.name))
                Undo.DestroyObjectImmediate(root);
        }

        // ---------- Managers ----------
        GameObject managerGo = new GameObject("UIManager");
        Undo.RegisterCreatedObjectUndo(managerGo, "Create UIManager");

        GameSettings settings = managerGo.AddComponent<GameSettings>();
        GameTimer timer = managerGo.AddComponent<GameTimer>();
        menu = managerGo.AddComponent<MenuManager>();

        settings.visualModeToggle = Object.FindFirstObjectByType<VisualModeToggle>(FindObjectsInactive.Include);
        settings.snapTurnProvider = Object.FindFirstObjectByType<SnapTurnProvider>(FindObjectsInactive.Include);

        foreach (ControllerInputActionManager input in Object.FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (input.name.Contains("Left"))
                settings.leftControllerInput = input;
            else if (input.name.Contains("Right"))
                settings.rightControllerInput = input;
        }

        if (settings.visualModeToggle != null)
        {
            Undo.RecordObject(settings.visualModeToggle, "Link settings");
            settings.visualModeToggle.settings = settings;
        }

        // Locomotion that is switched off while a menu is open (gravity stays on)
        var locked = new System.Collections.Generic.List<Behaviour>();

        foreach (LocomotionProvider provider in Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (provider.enabled && !(provider is GravityProvider))
                locked.Add(provider);
        }

        menu.lockedWhileInMenu = locked.ToArray();
        menu.gameTimer = timer;
        menu.settings = settings;
        menu.clickClip = click;

        // ---------- Menu canvas ----------
        Canvas menuCanvas = CreateWorldCanvas("MenuCanvas", new Vector2(960, 1120), 0.00072f, cam, true);
        menuCanvas.sortingOrder = 10;
        PlaceInFront(menuCanvas.transform, head, 1.25f, -0.1f);

        VRPanelFollow menuFollow = menuCanvas.gameObject.AddComponent<VRPanelFollow>();
        menuFollow.head = head;
        menuFollow.distance = 1.25f;
        menuFollow.heightOffset = -0.1f;
        menuFollow.angleThreshold = 40f;

        RectTransform content = BuildCard(menuCanvas.transform, new Vector2(960, 1120));

        menu.menuCanvas = menuCanvas.gameObject;
        menu.mainPage = BuildMainPage(content);
        menu.controlsPage = BuildControlsPage(content);
        menu.settingsPage = BuildSettingsPage(content);
        menu.pausePage = BuildPausePage(content);
        menu.endPage = BuildEndPage(content);

        menu.controlsPage.SetActive(false);
        menu.settingsPage.SetActive(false);
        menu.pausePage.SetActive(false);
        menu.endPage.SetActive(false);

        // ---------- Timer HUD ----------
        Canvas hud = BuildTimerHud(cam, head, out TMP_Text timerText);
        timer.timerText = timerText;
        menu.hudCanvas = hud.gameObject;
        hud.gameObject.SetActive(false);

        // ---------- Scene links ----------
        Room2PuzzleManager puzzle = Object.FindFirstObjectByType<Room2PuzzleManager>(FindObjectsInactive.Include);

        if (puzzle != null)
        {
            Undo.RecordObject(puzzle, "Link menu");
            puzzle.menuManager = menu;
            puzzle.gameTimer = timer;
            puzzle.endCanvas = null;
        }

        // Only the XR UI module should drive the UI (it also handles the mouse)
        foreach (InputSystemUIInputModule module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (module.GetComponent<XRUIInputModule>() != null)
            {
                Undo.RecordObject(module, "Disable InputSystemUIInputModule");
                module.enabled = false;
            }
        }

        EditorSceneManager.MarkSceneDirty(managerGo.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("Aegean VR UI built. Save the scene to keep it.");
    }

    // =====================================================================
    // Pages
    // =====================================================================

    static GameObject BuildMainPage(RectTransform parent)
    {
        RectTransform page = Page(parent, "MainPage");

        Kicker(page, "A VR ESCAPE ROOM");
        TMP_Text title = Text(page, "Title", "AEGEAN ESCAPE", titleFont, 88, Blue, 860, 110);
        title.enableAutoSizing = true;
        title.fontSizeMin = 60;
        title.fontSizeMax = 88;
        Divider(page);
        Text(page, "Story", "You are trapped in a mysterious Aegean house.\nFind the key. Solve the puzzle. Escape to the sea.",
            bodyFont, 32, Muted, 820, 96, FontStyles.Italic);
        Spacer(page, 16);

        Button(page, "PLAY", true, menu.Play);
        Button(page, "HOW TO PLAY", false, menu.OpenControls);
        Button(page, "SETTINGS", false, menu.OpenSettings);
        Button(page, "QUIT", false, menu.Quit);

        Spacer(page, 8);
        Hint(page, "Point with a controller and pull the trigger to select");

        return page.gameObject;
    }

    static GameObject BuildControlsPage(RectTransform parent)
    {
        RectTransform page = Page(parent, "ControlsPage");

        Text(page, "Title", "HOW TO PLAY", titleFont, 64, Blue, 860, 80);
        Divider(page);
        Text(page, "Goal", "Find the key that opens the door, then place the three sea treasures\non the puzzle station to escape to the sea.",
            bodyFont, 28, Muted, 860, 76);

        RectTransform columns = Rect("Columns", page);
        Layout(columns, 860, 410);
        HorizontalLayoutGroup row = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 20;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        ControlsColumn(columns, "VR CONTROLLERS", new[,]
        {
            { "Left Stick", "Move" },
            { "Right Stick", "Turn" },
            { "Push Stick Up", "Teleport" },
            { "Grip", "Grab" },
            { "Trigger", "Select / Use" },
            { "Menu Button", "Pause" },
        });

        ControlsColumn(columns, "KEYBOARD", new[,]
        {
            { "Arrow Keys", "Move" },
            { "F", "Teleport" },
            { "G / P", "Grab / Point" },
            { "[  /  ]", "Switch Hand" },
            { "WASD, Q / E", "Move Hand" },
            { "Mouse", "Rotate Hand" },
            { "V", "Hands / Pads" },
            { "Esc", "Pause" },
        });

        Button(page, "BACK", false, menu.Back);

        return page.gameObject;
    }

    static GameObject BuildSettingsPage(RectTransform parent)
    {
        RectTransform page = Page(parent, "SettingsPage");

        Text(page, "Title", "SETTINGS", titleFont, 64, Blue, 860, 80);
        Divider(page);

        menu.volumeValue = OptionRow(page, "MASTER VOLUME", menu.VolumeDown, menu.VolumeUp, true);
        menu.handsValue = OptionRow(page, "HAND VISUALS", menu.ToggleHands, menu.ToggleHands);
        menu.movementValue = OptionRow(page, "MOVEMENT", menu.ToggleMovement, menu.ToggleMovement);
        menu.turningValue = OptionRow(page, "TURNING", menu.ToggleTurning, menu.ToggleTurning);
        menu.snapAngleValue = OptionRow(page, "SNAP ANGLE", menu.SnapAnglePrevious, menu.SnapAngleNext);
        // Value -> ValueBox -> Control -> Row
        menu.snapAngleRow = menu.snapAngleValue.transform.parent.parent.parent.gameObject.AddComponent<CanvasGroup>();
        menu.timerValue = OptionRow(page, "SHOW TIMER", menu.ToggleTimer, menu.ToggleTimer);

        Text(page, "Note", "Teleport movement and snap turning are the most comfortable options.",
            bodyFont, 24, Muted, 820, 40, FontStyles.Italic);

        Button(page, "BACK", false, menu.Back);

        return page.gameObject;
    }

    static GameObject BuildPausePage(RectTransform parent)
    {
        RectTransform page = Page(parent, "PausePage");

        Kicker(page, "GAME PAUSED");
        Text(page, "Title", "PAUSED", titleFont, 88, Blue, 860, 110);
        Divider(page);
        menu.pauseTimeText = Text(page, "Time", "TIME  00:00", titleFont, 40, Gold, 860, 60);
        Spacer(page, 16);

        Button(page, "RESUME", true, menu.Resume);
        Button(page, "RESTART", false, menu.Restart);
        Button(page, "SETTINGS", false, menu.OpenSettings);
        Button(page, "MAIN MENU", false, menu.GoToMainMenu);

        Spacer(page, 8);
        Hint(page, "Press the Menu button to resume");

        return page.gameObject;
    }

    static GameObject BuildEndPage(RectTransform parent)
    {
        RectTransform page = Page(parent, "EndPage");

        Kicker(page, "CONGRATULATIONS");
        Text(page, "Title", "YOU ESCAPED!", titleFont, 84, Blue, 860, 110);
        Divider(page);
        Text(page, "Message", "The Aegean mystery is solved.\nThe sea is yours.", bodyFont, 32, Muted, 820, 90, FontStyles.Italic);
        Text(page, "TimeLabel", "YOUR TIME", titleFont, 28, Gold, 860, 36).characterSpacing = 12;
        menu.endTimeText = Text(page, "Time", "00:00", titleFont, 96, Blue, 860, 110);
        Spacer(page, 8);

        Button(page, "PLAY AGAIN", true, menu.Restart);
        Button(page, "MAIN MENU", false, menu.GoToMainMenu);
        Button(page, "QUIT", false, menu.Quit);

        Spacer(page, 8);
        Hint(page, "Thanks for playing");

        return page.gameObject;
    }

    // =====================================================================
    // Card, HUD and canvases
    // =====================================================================

    static Canvas CreateWorldCanvas(string name, Vector2 size, float scale, Camera cam, bool interactive)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4;
        scaler.referencePixelsPerUnit = 100;

        if (interactive)
        {
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * scale;

        return canvas;
    }

    static void PlaceInFront(Transform t, Transform head, float distance, float height)
    {
        if (head == null)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        t.position = head.position + forward * distance + Vector3.up * height;
        t.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // White marble card with a gold frame and two meander bands. Returns the content area.
    static RectTransform BuildCard(Transform parent, Vector2 size)
    {
        RectTransform shadowRt = Rect("Shadow", parent);
        Stretch(shadowRt, -40, -56, -40, -24);
        Img(shadowRt, shadow, ShadowColor, 1f);

        RectTransform card = Rect("Card", parent);
        Stretch(card);
        Image cardImage = Img(card, rounded, Marble, 1f);
        cardImage.raycastTarget = true;

        RectTransform frame = Rect("GoldFrame", card);
        Stretch(frame, 14, 14, 14, 14);
        Img(frame, ring, Gold, 1.2f);

        MeanderBand(card, "TopBand", true);
        MeanderBand(card, "BottomBand", false);

        RectTransform content = Rect("Content", card);
        Stretch(content, 50, 110, 50, 110);
        return content;
    }

    static void MeanderBand(RectTransform card, string name, bool top)
    {
        RectTransform band = Rect(name, card);
        band.anchorMin = new Vector2(0, top ? 1 : 0);
        band.anchorMax = new Vector2(1, top ? 1 : 0);
        band.pivot = new Vector2(0.5f, top ? 1 : 0);
        band.offsetMin = new Vector2(34, top ? -90 : 34);
        band.offsetMax = new Vector2(-34, top ? -34 : 90);
        Img(band, null, Blue, 1f);

        RectTransform pattern = Rect("Meander", band);
        Stretch(pattern, 6, 8, 6, 8);
        Image image = Img(pattern, meander, Gold, 1f);
        image.type = Image.Type.Tiled;
        // Texture is 80 px tall, band pattern is 40 units tall
        image.pixelsPerUnitMultiplier = 2f;
    }

    static Canvas BuildTimerHud(Camera cam, Transform head, out TMP_Text timerText)
    {
        Canvas hud = CreateWorldCanvas("TimerHUD", new Vector2(320, 104), 0.0008f, cam, false);
        hud.sortingOrder = 5;
        PlaceInFront(hud.transform, head, 1.1f, 0.22f);

        VRPanelFollow follow = hud.gameObject.AddComponent<VRPanelFollow>();
        follow.head = head;
        follow.distance = 1.1f;
        follow.heightOffset = 0.22f;
        follow.followPitch = true;
        follow.angleThreshold = 18f;
        follow.followSpeed = 3f;
        follow.minDistance = 0.5f;

        RectTransform root = (RectTransform)hud.transform;

        RectTransform shadowRt = Rect("Shadow", root);
        Stretch(shadowRt, -18, -24, -18, -12);
        Img(shadowRt, shadow, new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.3f), 1f);

        RectTransform pill = Rect("Pill", root);
        Stretch(pill);
        Img(pill, rounded, new Color(Blue.r, Blue.g, Blue.b, 0.94f), 0.85f);

        RectTransform frame = Rect("GoldFrame", pill);
        Stretch(frame, 6, 6, 6, 6);
        Img(frame, ring, Gold, 1f);

        TMP_Text label = Text(pill, "Label", "TIME", titleFont, 24, Gold, 0, 0);
        label.characterSpacing = 10;
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(0.38f, 1);
        labelRt.offsetMin = new Vector2(24, 0);
        labelRt.offsetMax = Vector2.zero;

        RectTransform line = Rect("Separator", pill);
        line.anchorMin = line.anchorMax = new Vector2(0.38f, 0.5f);
        line.sizeDelta = new Vector2(2, 52);
        Img(line, null, new Color(Gold.r, Gold.g, Gold.b, 0.6f), 1f);

        timerText = Text(pill, "TimerText", "<mspace=0.56em>00:00</mspace>", titleFont, 54, Marble, 0, 0);
        RectTransform timeRt = timerText.rectTransform;
        timeRt.anchorMin = new Vector2(0.38f, 0);
        timeRt.anchorMax = new Vector2(1, 1);
        timeRt.offsetMin = Vector2.zero;
        timeRt.offsetMax = new Vector2(-16, 0);

        return hud;
    }

    // =====================================================================
    // Widgets
    // =====================================================================

    static RectTransform Page(RectTransform parent, string name)
    {
        RectTransform page = Rect(name, parent);
        Stretch(page);

        VerticalLayoutGroup layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return page;
    }

    static void Kicker(RectTransform parent, string text)
    {
        TMP_Text kicker = Text(parent, "Kicker", text, titleFont, 28, Gold, 860, 36);
        kicker.characterSpacing = 18;
    }

    static void Hint(RectTransform parent, string text)
    {
        Text(parent, "Hint", text, bodyFont, 24, new Color(Muted.r, Muted.g, Muted.b, 0.8f), 860, 34, FontStyles.Italic);
    }

    static void Spacer(RectTransform parent, float height)
    {
        Layout(Rect("Spacer", parent), 10, height);
    }

    // Gold line with a small diamond in the middle
    static void Divider(RectTransform parent)
    {
        RectTransform divider = Rect("Divider", parent);
        Layout(divider, 440, 26);

        for (int side = -1; side <= 1; side += 2)
        {
            RectTransform line = Rect(side < 0 ? "LineLeft" : "LineRight", divider);
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.sizeDelta = new Vector2(180, 3);
            line.anchoredPosition = new Vector2(side * 110, 0);
            Img(line, null, Gold, 1f);
        }

        RectTransform diamond = Rect("Diamond", divider);
        diamond.anchorMin = diamond.anchorMax = new Vector2(0.5f, 0.5f);
        diamond.sizeDelta = new Vector2(14, 14);
        diamond.localRotation = Quaternion.Euler(0, 0, 45);
        Img(diamond, null, Gold, 1f);
    }

    static void Button(RectTransform parent, string label, bool primary, UnityAction onClick)
    {
        RectTransform rt = Rect(label.Replace(" ", "") + "Button", parent);
        Layout(rt, 560, primary ? 96 : 80);

        Image fill = Img(rt, rounded, primary ? Blue : Marble, 1f);
        fill.raycastTarget = true;

        RectTransform outlineRt = Rect("Outline", rt);
        Stretch(outlineRt);
        Image outline = Img(outlineRt, ring, Gold, 1f);

        TMP_Text text = Text(rt, "Label", label, titleFont, primary ? 40 : 34, primary ? Marble : Blue, 0, 0);
        Stretch(text.rectTransform);
        text.characterSpacing = 8;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        button.transition = Selectable.Transition.None;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        UIButtonStyle style = rt.gameObject.AddComponent<UIButtonStyle>();
        style.fill = fill;
        style.outline = outline;
        style.label = text;

        if (primary)
        {
            style.normalFill = Blue;
            style.hoverFill = BlueHover;
            style.pressedFill = BluePressed;
            style.normalText = Marble;
            style.hoverText = Color.white;
            style.normalOutline = Gold;
            style.hoverOutline = GoldLight;
        }
        else
        {
            style.normalFill = Marble;
            style.hoverFill = Blue;
            style.pressedFill = BluePressed;
            style.normalText = Blue;
            style.hoverText = Marble;
            style.normalOutline = Gold;
            style.hoverOutline = GoldLight;
        }
    }

    static Button ArrowButton(RectTransform parent, string name, string arrow, UnityAction onClick, bool left)
    {
        RectTransform rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0 : 1, 0.5f);
        rt.pivot = new Vector2(left ? 0 : 1, 0.5f);
        rt.sizeDelta = new Vector2(64, 64);

        Image fill = Img(rt, rounded, Blue, 1.6f);
        fill.raycastTarget = true;

        TMP_Text text = Text(rt, "Arrow", arrow, bodyFont, 36, Marble, 0, 0, FontStyles.Bold);
        Stretch(text.rectTransform, 0, 4, 0, 0);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        UIButtonStyle style = rt.gameObject.AddComponent<UIButtonStyle>();
        style.fill = fill;
        style.label = text;
        style.normalFill = Blue;
        style.hoverFill = BlueHover;
        style.pressedFill = BluePressed;
        style.normalText = Marble;
        style.hoverText = GoldLight;
        style.hoverScale = 1.1f;

        return button;
    }

    // "LABEL      [<]  VALUE  [>]" row. Returns the value text.
    static TMP_Text OptionRow(RectTransform parent, string label, UnityAction left, UnityAction right, bool withBar = false)
    {
        RectTransform row = Rect(label.Replace(" ", "") + "Row", parent);
        Layout(row, 840, 78);

        TMP_Text labelText = Text(row, "Label", label, titleFont, 30, Blue, 0, 0);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.characterSpacing = 4;
        RectTransform labelRt = labelText.rectTransform;
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(0.5f, 1);
        labelRt.offsetMin = new Vector2(10, 0);
        labelRt.offsetMax = Vector2.zero;

        RectTransform control = Rect("Control", row);
        control.anchorMin = control.anchorMax = new Vector2(1, 0.5f);
        control.pivot = new Vector2(1, 0.5f);
        control.sizeDelta = new Vector2(400, 64);
        control.anchoredPosition = new Vector2(-6, 0);

        ArrowButton(control, "Previous", "<", left, true);
        ArrowButton(control, "Next", ">", right, false);

        RectTransform valueBox = Rect("ValueBox", control);
        Stretch(valueBox, 76, 0, 76, 0);
        Img(valueBox, rounded, MarbleShade, 1.6f);

        TMP_Text value = Text(valueBox, "Value", "-", titleFont, 28, Blue, 0, 0);
        Stretch(value.rectTransform);

        if (withBar)
        {
            value.alignment = TextAlignmentOptions.MidlineRight;
            Stretch(value.rectTransform, 0, 0, 16, 0);

            RectTransform bar = Rect("Bar", valueBox);
            bar.anchorMin = new Vector2(0, 0.5f);
            bar.anchorMax = new Vector2(1, 0.5f);
            bar.offsetMin = new Vector2(18, -7);
            bar.offsetMax = new Vector2(-96, 7);
            Img(bar, rounded, new Color(Blue.r, Blue.g, Blue.b, 0.15f), 6f);

            RectTransform fill = Rect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.8f, 1);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Img(fill, rounded, Gold, 6f);

            menu.volumeFill = fill;
        }

        RectTransform separator = Rect("Separator", row);
        separator.anchorMin = new Vector2(0, 0);
        separator.anchorMax = new Vector2(1, 0);
        separator.offsetMin = new Vector2(0, 0);
        separator.offsetMax = new Vector2(0, 2);
        Img(separator, null, new Color(Gold.r, Gold.g, Gold.b, 0.35f), 1f);

        return value;
    }

    static void ControlsColumn(RectTransform parent, string header, string[,] rows)
    {
        RectTransform column = Rect(header.Replace(" ", "") + "Column", parent);
        Img(column, rounded, MarbleShade, 1.4f);

        TMP_Text title = Text(column, "Header", header, titleFont, 28, Blue, 0, 0);
        title.characterSpacing = 6;
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.offsetMin = new Vector2(16, -64);
        titleRt.offsetMax = new Vector2(-16, -16);

        RectTransform line = Rect("Line", column);
        line.anchorMin = line.anchorMax = new Vector2(0.5f, 1);
        line.sizeDelta = new Vector2(120, 2);
        line.anchoredPosition = new Vector2(0, -70);
        Img(line, null, Gold, 1f);

        var sb = new System.Text.StringBuilder();
        string actionColor = "#" + ColorUtility.ToHtmlStringRGB(Muted);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            if (i > 0)
                sb.Append('\n');

            sb.Append("<b>").Append(rows[i, 0]).Append("</b><pos=56%><color=").Append(actionColor).Append('>')
              .Append(rows[i, 1]).Append("</color>");
        }

        TMP_Text list = Text(column, "List", sb.ToString(), bodyFont, 28, Blue, 0, 0);
        list.alignment = TextAlignmentOptions.TopLeft;
        list.textWrappingMode = TextWrappingModes.NoWrap;
        list.lineSpacing = 45;
        Stretch(list.rectTransform, 22, 16, 10, 90);
    }

    // =====================================================================
    // Low level helpers
    // =====================================================================

    static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void Layout(RectTransform rt, float width, float height)
    {
        LayoutElement element = rt.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    static Image Img(RectTransform rt, Sprite sprite, Color color, float pixelsPerUnitMultiplier)
    {
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        if (sprite != null)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }

        return image;
    }

    static TMP_Text Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
        float width, float height, FontStyles style = FontStyles.Normal)
    {
        RectTransform rt = Rect(name, parent);

        if (width > 0)
            Layout(rt, width, height);

        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        return tmp;
    }

    // =====================================================================
    // Generated sprites
    // =====================================================================

    static void CreateSprites()
    {
        Directory.CreateDirectory(SpriteFolder);

        rounded = SaveSprite("Rounded", RoundedRect(128, 40f, 0f, 0f), 44, false);
        ring = SaveSprite("RoundedRing", RoundedRect(128, 40f, 5f, 0f), 44, false);
        shadow = SaveSprite("SoftShadow", RoundedRect(128, 16f, 0f, 22f), 60, false);
        meander = SaveSprite("Meander", Meander(8), 0, true);
    }

    // Signed-distance rounded rectangle: filled, outline (thickness > 0) or soft shadow (blur > 0)
    static Texture2D RoundedRect(int size, float radius, float thickness, float blur)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        float half = size * 0.5f - 1f - blur;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (half - radius);
                float d = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;

                float a;

                if (blur > 0f)
                    a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-blur, blur, d));
                else if (thickness > 0f)
                    a = Mathf.Clamp01(0.5f - (Mathf.Abs(d + thickness * 0.5f) - thickness * 0.5f));
                else
                    a = Mathf.Clamp01(0.5f - d);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        return tex;
    }

    // One tile of a Greek key (meander) border, white on transparent
    static Texture2D Meander(int cell)
    {
        string[] rows =
        {
            "########",
            "........",
            ".######.",
            ".#....#.",
            ".#.##.#.",
            ".#.#..#.",
            ".#.#..#.",
            ".#.####.",
            ".#......",
            "########",
        };

        int w = rows[0].Length * cell;
        int h = rows.Length * cell;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            string row = rows[rows.Length - 1 - y / cell];

            for (int x = 0; x < w; x++)
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(row[x / cell] == '#' ? 255 : 0));
        }

        tex.SetPixels32(pixels);
        return tex;
    }

    static Sprite SaveSprite(string name, Texture2D tex, int border, bool repeat)
    {
        string path = SpriteFolder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
