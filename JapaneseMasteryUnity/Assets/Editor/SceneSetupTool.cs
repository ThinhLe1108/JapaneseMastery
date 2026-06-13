using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SceneSetupTool : EditorWindow
{
    [MenuItem("Japanese Mastery/Auto Setup Scenes")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupTool>("Scene Setup Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Generate Unity Scenes", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Generate Login Scene"))
        {
            GenerateLoginScene();
        }
        
        if (GUILayout.Button("2. Generate Main Menu Scene"))
        {
            GenerateMainScene();
        }

        if (GUILayout.Button("3. Generate Admin Scene"))
        {
            GenerateAdminScene();
        }
        
        GUILayout.Space(20);
        GUILayout.Label("Note: Ensure TextMeshPro Essentials are imported first!", EditorStyles.helpBox);
    }

    private static void GenerateLoginScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.102f, 0.11f, 0.16f); // #1a1c29

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject managerObj = new GameObject("LoginManager");
        LoginManager manager = managerObj.AddComponent<LoginManager>();

        // Create Global Managers
        GameObject globalManagers = new GameObject("GlobalManagers");
        globalManagers.AddComponent<NetworkManager>();
        globalManagers.AddComponent<Global>();
        globalManagers.AddComponent<Database>();
        globalManagers.AddComponent<SRSManager>();

        CreateText(canvasObj.transform, "Title", "JAPANESE MASTERY LOGIN", 60, new Vector2(0, 200), Color.white, true);
        
        GameObject userObj = CreateInputField(canvasObj.transform, "UsernameInput", "Username", new Vector2(200, -20));
        GameObject passObj = CreateInputField(canvasObj.transform, "PasswordInput", "Password", new Vector2(200, -90));
        
        GameObject loginBtnObj = CreateButton(canvasObj.transform, "LoginButton", "Login", new Vector2(100, -170), new Vector2(180, 50), new Color(0.12f, 0.13f, 0.17f));
        GameObject regBtnObj = CreateButton(canvasObj.transform, "RegisterButton", "Register", new Vector2(300, -170), new Vector2(180, 50), new Color(0.12f, 0.13f, 0.17f));

        GameObject statusObj = CreateText(canvasObj.transform, "StatusText", "", 24, new Vector2(200, -230), Color.red, false);

        manager.usernameInput = userObj.GetComponent<TMP_InputField>();
        manager.passwordInput = passObj.GetComponent<TMP_InputField>();
        manager.btnLogin = loginBtnObj.GetComponent<Button>();
        manager.btnRegister = regBtnObj.GetComponent<Button>();
        manager.statusLabel = statusObj.GetComponent<TextMeshProUGUI>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Login.unity");
        AddSceneToBuildSettings("Assets/Scenes/Login.unity");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        foreach (var s in original)
        {
            if (s.path == scenePath) return; // Already exists
        }
        
        EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[original.Length + 1];
        System.Array.Copy(original, newSettings, original.Length);
        newSettings[newSettings.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newSettings;
    }

    private static void GenerateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.17f, 0.28f); // Dark blue background

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject managerObj = new GameObject("MainMenuManager");
        MainMenuManager manager = managerObj.AddComponent<MainMenuManager>();

        // Top Bar Background
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasObj.transform, false);
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(0.05f, 0.05f, 0.1f);
        RectTransform topBarRt = topBar.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.pivot = new Vector2(0.5f, 1);
        topBarRt.anchoredPosition = Vector2.zero;
        topBarRt.sizeDelta = new Vector2(0, 60);

        GameObject btnLogout = CreateButton(topBar.transform, "BtnLogout", "Logout", new Vector2(-540, -30), new Vector2(120, 40), new Color(0.7f, 0.2f, 0.2f));

        GameObject nameObj = CreateText(topBar.transform, "NameLabel", "Player: Guest", 24, new Vector2(-150, -30), Color.white, false);
        GameObject lvlObj = CreateText(topBar.transform, "LevelLabel", "Level: 1", 24, new Vector2(50, -30), new Color(0.4f, 1.0f, 0.4f), false);
        GameObject coinObj = CreateText(topBar.transform, "CoinLabel", "G-Coins: 0", 24, new Vector2(250, -30), new Color(1.0f, 0.8f, 0.2f), false);

        // Title with Outline
        GameObject titleObj = CreateText(canvasObj.transform, "Title", "JAPANESE MASTERY", 80, new Vector2(0, 220), new Color(1f, 0.85f, 0.4f), true);
        Outline outline = titleObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.1f, 0f, 1f); // Dark brown shadow
        outline.effectDistance = new Vector2(4, -4);

        Color btnColor = new Color(0.18f, 0.22f, 0.33f);
        Vector2 btnSize = new Vector2(450, 60);

        GameObject btnPlacement = CreateButton(canvasObj.transform, "BtnPlacement", "Level-Up Test", new Vector2(0, 60), btnSize, btnColor);
        GameObject btnQuantum = CreateButton(canvasObj.transform, "BtnQuantum", "Quantum Test", new Vector2(0, -10), btnSize, btnColor);
        GameObject btnSolo = CreateButton(canvasObj.transform, "BtnSolo", "Learn", new Vector2(0, -80), btnSize, btnColor);
        GameObject btnPvP = CreateButton(canvasObj.transform, "BtnPvP", "PvP Arena", new Vector2(0, -150), btnSize, btnColor);
        GameObject btnShop = CreateButton(canvasObj.transform, "BtnShop", "Shop", new Vector2(0, -220), btnSize, btnColor);
        GameObject btnEnterTest = CreateButton(canvasObj.transform, "BtnEnterTestCode", "Enter Test Code", new Vector2(0, -290), btnSize, btnColor);
        
        GameObject btnAdmin = CreateButton(canvasObj.transform, "BtnAdmin", "Admin Control Panel", new Vector2(0, 50), btnSize, btnColor);
        GameObject btnMod = CreateButton(canvasObj.transform, "BtnMod", "Moderator Panel", new Vector2(0, 50), btnSize, btnColor);
        GameObject btnDes = CreateButton(canvasObj.transform, "BtnDesigner", "Designer Shop", new Vector2(0, 50), btnSize, btnColor);
        GameObject btnCreateTest = CreateButton(canvasObj.transform, "BtnCreateTest", "Create New Test", new Vector2(0, 50), btnSize, btnColor);

        manager.titleLabel = titleObj.GetComponent<TextMeshProUGUI>();
        manager.nameLabel = nameObj.GetComponent<TextMeshProUGUI>();
        manager.jlptLabel = lvlObj.GetComponent<TextMeshProUGUI>();
        manager.coinLabel = coinObj.GetComponent<TextMeshProUGUI>();
        
        manager.btnPlacement = btnPlacement.GetComponent<Button>();
        manager.btnQuantum = btnQuantum.GetComponent<Button>();
        manager.btnSolo = btnSolo.GetComponent<Button>();
        manager.btnPvP = btnPvP.GetComponent<Button>();
        manager.btnShop = btnShop.GetComponent<Button>();
        manager.btnEnterTestCode = btnEnterTest.GetComponent<Button>();
        manager.btnLogout = btnLogout.GetComponent<Button>();
        
        manager.btnAdmin = btnAdmin.GetComponent<Button>();
        manager.btnMod = btnMod.GetComponent<Button>();
        manager.btnDesigner = btnDes.GetComponent<Button>();
        manager.btnCreateTest = btnCreateTest.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        AddSceneToBuildSettings("Assets/Scenes/Main.unity");
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 pos, Color color, bool bold, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        
        RectTransform rt = go.GetComponent<RectTransform>();
        if (parent.name == "TopBar") {
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
        } else {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1200, 150);
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, int fontSize = 20)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (parent.name == "TopBar") {
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
        } else {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.13f, 0.17f);
        TMP_InputField input = go.AddComponent<TMP_InputField>();

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(go.transform, false);
        RectTransform pRt = placeholderObj.AddComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one; pRt.sizeDelta = new Vector2(-20, -10);
        TextMeshProUGUI pTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        pTmp.text = placeholder;
        pTmp.color = new Color(0.5f, 0.5f, 0.5f);
        pTmp.fontSize = 20;
        pTmp.alignment = TextAlignmentOptions.Left;
        input.placeholder = pTmp;

        GameObject textObj = new GameObject("Text Area");
        textObj.transform.SetParent(go.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.sizeDelta = new Vector2(-20, -10);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.color = Color.white;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Left;

        input.textComponent = tmp;
        input.textViewport = textRt;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 50);
        return go;
    }

    private static void GenerateAdminScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.13f, 0.19f); // Dark blue/purple

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject managerObj = new GameObject("AdminPanelManager");
        AdminPanelManager manager = managerObj.AddComponent<AdminPanelManager>();

        // Menu Button
        GameObject btnBack = CreateButton(canvasObj.transform, "BtnBack", "Back", new Vector2(100, -60), new Vector2(100, 40), new Color(0.2f, 0.2f, 0.25f), 18);
        RectTransform btnBackRt = btnBack.GetComponent<RectTransform>();
        btnBackRt.anchorMin = new Vector2(0, 1);
        btnBackRt.anchorMax = new Vector2(0, 1);
        btnBackRt.anchoredPosition = new Vector2(100, -60);

        // Header
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasObj.transform, false);
        RectTransform topBarRt = topBar.AddComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0.5f, 1f);
        topBarRt.anchorMax = new Vector2(0.5f, 1f);
        topBarRt.anchoredPosition = new Vector2(0, -90);
        
        GameObject titleObj = CreateText(topBar.transform, "Title", "ADMIN CONTROL PANEL", 32, new Vector2(0, 0), Color.white, true);

        // Tab Buttons
        GameObject btnUserTab = CreateButton(canvasObj.transform, "BtnUserTab", "Users Management", new Vector2(-200, 260), new Vector2(300, 40), new Color(0.1f, 0.3f, 0.1f), 18); 
        GameObject btnVocabTab = CreateButton(canvasObj.transform, "BtnVocabTab", "Vocab Management", new Vector2(200, 260), new Vector2(300, 40), new Color(0.3f, 0.3f, 0.3f), 18); 

        // Status Text is actually overlaid on top of tabs in Godot... But we'll put it slightly below the title.
        GameObject statusText = CreateText(canvasObj.transform, "StatusText", "Users loaded successfully!", 18, new Vector2(0, 220), Color.gray, false);

        GameObject userPanel = new GameObject("UserPanel");
        userPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform upRt = userPanel.AddComponent<RectTransform>();
        upRt.anchorMin = Vector2.zero; upRt.anchorMax = Vector2.one; 
        upRt.sizeDelta = Vector2.zero;

        GameObject btnRefreshUsers = CreateButton(userPanel.transform, "BtnRefreshUsers", "Load / Refresh User List", new Vector2(0, 140), new Vector2(800, 40), new Color(0.15f, 0.15f, 0.18f), 18);
        GameObject userSearch = CreateInputField(userPanel.transform, "InputSearchUser", "Search Username / Role...", new Vector2(0, 200));
        userSearch.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 40);

        GameObject userListContent = new GameObject("UserListContent");
        userListContent.transform.SetParent(userPanel.transform, false);
        RectTransform ulcRt = userListContent.AddComponent<RectTransform>();
        ulcRt.anchorMin = new Vector2(0.5f, 0.5f); ulcRt.anchorMax = new Vector2(0.5f, 0.5f);
        ulcRt.anchoredPosition = new Vector2(0, 80);
        
        // Dummy Row 1
        GameObject row = new GameObject("DummyRow");
        row.transform.SetParent(userListContent.transform, false);
        row.AddComponent<RectTransform>();
        CreateText(row.transform, "Label", "designer (Lv 1.0)", 18, new Vector2(-280, -20), Color.white, false, TextAlignmentOptions.Left).GetComponent<RectTransform>().sizeDelta = new Vector2(300, 35);
        CreateButton(row.transform, "RoleDD", "DESIGNER v", new Vector2(-120, -20), new Vector2(140, 35), new Color(0.1f, 0.1f, 0.1f), 16);
        CreateButton(row.transform, "BtnChangeRole", "Change Role", new Vector2(25, -20), new Vector2(80, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateInputField(row.transform, "InputLv", "1.0", new Vector2(105, -20)).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 35);
        CreateButton(row.transform, "BtnChangeLv", "Change Lv", new Vector2(185, -20), new Vector2(80, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateButton(row.transform, "BtnDelete", "Delete", new Vector2(265, -20), new Vector2(60, 35), new Color(0.2f, 0.1f, 0.1f), 16).GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.4f, 0.4f);

        // Dummy Row 2
        GameObject row2 = new GameObject("DummyRow2");
        row2.transform.SetParent(userListContent.transform, false);
        row2.AddComponent<RectTransform>();
        CreateText(row2.transform, "Label", "moderator (Lv 1.0)", 18, new Vector2(-280, -70), Color.white, false, TextAlignmentOptions.Left).GetComponent<RectTransform>().sizeDelta = new Vector2(300, 35);
        CreateButton(row2.transform, "RoleDD", "MODERATOR v", new Vector2(-120, -70), new Vector2(140, 35), new Color(0.1f, 0.1f, 0.1f), 16);
        CreateButton(row2.transform, "BtnChangeRole", "Change Role", new Vector2(25, -70), new Vector2(80, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateInputField(row2.transform, "InputLv", "1.0", new Vector2(105, -70)).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 35);
        GameObject pagination = new GameObject("Pagination");
        pagination.transform.SetParent(userPanel.transform, false);
        CreateButton(pagination.transform, "BtnPrev", "< Prev", new Vector2(-150, -320), new Vector2(100, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateText(pagination.transform, "PageText", "1 / 1", 18, new Vector2(0, -320), Color.gray, false);
        CreateButton(pagination.transform, "BtnNext", "Next >", new Vector2(150, -320), new Vector2(100, 35), new Color(0.15f, 0.15f, 0.18f), 16);

        // --- Vocab Panel ---
        GameObject vocabPanel = new GameObject("VocabPanel");
        vocabPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform vpRt = vocabPanel.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; 
        vpRt.sizeDelta = Vector2.zero;
        vocabPanel.SetActive(false); // Hidden by default

        GameObject vocabLevelInput = CreateInputField(vocabPanel.transform, "InputVocabLevel", "Enter Level (e.g., 25)", new Vector2(-180, 200));
        vocabLevelInput.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 40);
        GameObject btnLoadVocab = CreateButton(vocabPanel.transform, "BtnLoadVocab", "Load Vocab For This Level", new Vector2(90, 200), new Vector2(260, 40), new Color(0.15f, 0.15f, 0.18f), 18);

        GameObject vocabSearch = CreateInputField(vocabPanel.transform, "InputSearchVocab", "Search Japanese / Romaji / Meaning...", new Vector2(0, 140));
        vocabSearch.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 40);

        GameObject vocabListContent = new GameObject("VocabListContent");
        vocabListContent.transform.SetParent(vocabPanel.transform, false);
        RectTransform vlcRt = vocabListContent.AddComponent<RectTransform>();
        vlcRt.anchorMin = new Vector2(0.5f, 0.5f); vlcRt.anchorMax = new Vector2(0.5f, 0.5f);
        vlcRt.anchoredPosition = new Vector2(0, 0);

        GameObject vRow = new GameObject("DummyRow");
        vRow.transform.SetParent(vocabListContent.transform, false);
        vRow.AddComponent<RectTransform>();
        CreateText(vRow.transform, "Label", "[Lv1] あ (a) -                   ", 18, new Vector2(-200, -20), Color.white, false, TextAlignmentOptions.Left).GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
        CreateInputField(vRow.transform, "InputLv", "1", new Vector2(80, -20)).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 35);
        CreateButton(vRow.transform, "BtnChangeLv", "Save Level", new Vector2(170, -20), new Vector2(100, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateText(vRow.transform, "LabelDynamic", "(API Vocab - Level cannot be changed)", 16, new Vector2(120, -20), new Color(0.6f, 0.6f, 0.6f), false, TextAlignmentOptions.Left);
        
        GameObject vPagination = new GameObject("Pagination");
        vPagination.transform.SetParent(vocabPanel.transform, false);
        CreateButton(vPagination.transform, "BtnPrev", "< Prev", new Vector2(-150, -320), new Vector2(100, 35), new Color(0.15f, 0.15f, 0.18f), 16);
        CreateText(vPagination.transform, "PageText", "1 / 1", 18, new Vector2(0, -320), Color.gray, false);
        CreateButton(vPagination.transform, "BtnNext", "Next >", new Vector2(150, -320), new Vector2(100, 35), new Color(0.15f, 0.15f, 0.18f), 16);

        // Bindings
        manager.btnBack = btnBack.GetComponent<Button>();
        manager.btnRefreshUsers = btnRefreshUsers.GetComponent<Button>();
        manager.statusText = statusText.GetComponent<TextMeshProUGUI>();
        manager.userListContent = userListContent.transform;
        
        manager.btnUserTab = btnUserTab.GetComponent<Button>();
        manager.btnVocabTab = btnVocabTab.GetComponent<Button>();
        manager.userPanel = userPanel;
        manager.vocabPanel = vocabPanel;

        TMPro.TMP_FontAsset jpFontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/NotoSansCJKjp-Regular SDF.asset");
        if (jpFontAsset != null) manager.jpFont = jpFontAsset;
        
        manager.userSearchInput = userSearch.GetComponent<TMP_InputField>();
        manager.vocabSearchInput = vocabSearch.GetComponent<TMP_InputField>();
        manager.vocabLevelInput = vocabLevelInput.GetComponent<TMP_InputField>();
        manager.btnLoadVocab = btnLoadVocab.GetComponent<Button>();
        
        manager.btnUserPrev = pagination.transform.Find("BtnPrev").GetComponent<Button>();
        manager.btnUserNext = pagination.transform.Find("BtnNext").GetComponent<Button>();
        manager.txtUserPage = pagination.transform.Find("PageText").GetComponent<TextMeshProUGUI>();

        manager.btnVocabPrev = vPagination.transform.Find("BtnPrev").GetComponent<Button>();
        manager.btnVocabNext = vPagination.transform.Find("BtnNext").GetComponent<Button>();
        manager.txtVocabPage = vPagination.transform.Find("PageText").GetComponent<TextMeshProUGUI>();
        manager.vocabListContent = vocabListContent.transform;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/AdminPanel.unity");
        AddSceneToBuildSettings("Assets/Scenes/AdminPanel.unity");
    }
}
