using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;

public class PortGodotScenes : EditorWindow
{
    [MenuItem("Japanese Mastery/Port Godot Scenes")]
    public static void PortScenes()
    {
        CreateLevelTest();
        CreateQuantumTest();
        CreateSoloLearning();
        Debug.Log("All scenes ported successfully!");
    }

    private static void CreateLevelTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LevelTest.unity");
        SetupCommonUI(out var canvasGo, out var btnBack);
        
        var managerGo = new GameObject("LevelTestManager");
        var manager = managerGo.AddComponent<LevelTestManager>();
        manager.btnBack = btnBack;
        
        manager.progressLabel = CreateText(canvasGo.transform, "ProgressLabel", "Question 1/10", new Vector2(-200, -50), new Vector2(300, 50));
        manager.progressLabel.alignment = TextAlignmentOptions.TopRight;
        manager.progressLabel.rectTransform.anchorMin = new Vector2(1, 1);
        manager.progressLabel.rectTransform.anchorMax = new Vector2(1, 1);
        manager.progressLabel.rectTransform.pivot = new Vector2(1, 1);
        
        manager.wordLabel = CreateText(canvasGo.transform, "WordLabel", "Loading data...", new Vector2(0, -100), new Vector2(800, 150), 100);
        manager.wordLabel.rectTransform.anchorMin = new Vector2(0.5f, 1);
        manager.wordLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
        
        manager.promptLabel = CreateText(canvasGo.transform, "PromptLabel", "Choose the meaning:", new Vector2(0, -250), new Vector2(600, 50), 30);
        manager.promptLabel.rectTransform.anchorMin = new Vector2(0.5f, 1);
        manager.promptLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
        
        var optionsContainer = new GameObject("OptionsContainer");
        optionsContainer.transform.SetParent(canvasGo.transform, false);
        var glg = optionsContainer.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(400, 100);
        glg.spacing = new Vector2(30, 30);
        optionsContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        manager.optionsContainer = optionsContainer.transform;
        
        manager.btnSubmit = CreateButton(canvasGo.transform, "BtnSubmit", "Confirm", new Vector2(0, 100), new Vector2(200, 60));
        manager.btnSubmit.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
        manager.btnSubmit.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
        
        manager.feedbackLabel = CreateText(canvasGo.transform, "FeedbackLabel", "Correct!", new Vector2(0, 50), new Vector2(900, 100), 30);
        
        manager.optionButtonPrefab = CreateOptionButtonPrefab();
        
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateQuantumTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/QuantumTest.unity");
        SetupCommonUI(out var canvasGo, out var btnBack);
        
        var managerGo = new GameObject("QuantumTestManager");
        var manager = managerGo.AddComponent<QuantumTestManager>();
        manager.btnBack = btnBack;
        
        manager.progressLabel = CreateText(canvasGo.transform, "ProgressLabel", "Question 1/10", new Vector2(-200, -50), new Vector2(300, 50));
        manager.wordLabel = CreateText(canvasGo.transform, "WordLabel", "Loading data...", new Vector2(0, -100), new Vector2(800, 150), 100);
        manager.promptLabel = CreateText(canvasGo.transform, "PromptLabel", "Choose the meaning:", new Vector2(0, -250), new Vector2(600, 50), 30);
        
        var optionsContainer = new GameObject("OptionsContainer");
        optionsContainer.transform.SetParent(canvasGo.transform, false);
        var glg = optionsContainer.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(400, 100);
        glg.spacing = new Vector2(30, 30);
        optionsContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        manager.optionsContainer = optionsContainer.transform;
        
        manager.btnSubmit = CreateButton(canvasGo.transform, "BtnSubmit", "Confirm", new Vector2(0, 100), new Vector2(200, 60));
        manager.btnSubmit.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
        manager.btnSubmit.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
        
        manager.feedbackLabel = CreateText(canvasGo.transform, "FeedbackLabel", "Correct!", new Vector2(0, 50), new Vector2(900, 100), 30);
        
        manager.optionButtonPrefab = CreateOptionButtonPrefab();
        
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateSoloLearning()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SoloLearning.unity");
        SetupCommonUI(out var canvasGo, out var btnBack);
        
        var managerGo = new GameObject("SoloLearningManager");
        var manager = managerGo.AddComponent<SoloLearningManager>();
        manager.btnBack = btnBack;
        
        manager.btnSkip = CreateButton(canvasGo.transform, "BtnSkip", "Skip >>", new Vector2(-100, -50), new Vector2(150, 40));
        manager.btnSkip.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
        manager.btnSkip.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        
        manager.wordLabel = CreateText(canvasGo.transform, "WordLabel", "Loading...", new Vector2(0, 50), new Vector2(800, 300), 150);
        
        manager.romajiLabel = CreateText(canvasGo.transform, "RomajiLabel", "Reading", new Vector2(0, -150), new Vector2(800, 100), 30);
        manager.romajiLabel.rectTransform.anchorMin = new Vector2(0.5f, 1);
        manager.romajiLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
        
        manager.drawPrompt = CreateText(canvasGo.transform, "DrawPrompt", "Trace the character", new Vector2(0, -250), new Vector2(800, 50), 36);
        manager.drawPrompt.rectTransform.anchorMin = new Vector2(0.5f, 1);
        manager.drawPrompt.rectTransform.anchorMax = new Vector2(0.5f, 1);
        
        var drawCanvasGo = new GameObject("DrawCanvas");
        drawCanvasGo.transform.SetParent(canvasGo.transform, false);
        var dcRect = drawCanvasGo.AddComponent<RectTransform>();
        dcRect.anchorMin = Vector2.zero; dcRect.anchorMax = Vector2.one;
        dcRect.sizeDelta = Vector2.zero;
        var img = drawCanvasGo.AddComponent<Image>(); // To catch raycasts
        img.color = new Color(0,0,0,0); 
        manager.drawCanvas = drawCanvasGo.transform;
        
        manager.btnNext = CreateButton(canvasGo.transform, "BtnNext", "Continue", new Vector2(0, 100), new Vector2(250, 60));
        manager.btnNext.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
        manager.btnNext.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
        
        // Setup LineRenderer prefab
        var lrPrefabGo = new GameObject("LinePrefab");
        var lr = lrPrefabGo.AddComponent<LineRenderer>();
        lr.startWidth = 0.5f;
        lr.endWidth = 0.5f;
        lr.numCapVertices = 5;
        lr.numCornerVertices = 5;
        manager.lineRendererPrefab = lrPrefabGo; // Normally we'd save this as an actual prefab asset, but direct ref works if it's disabled in scene.
        lrPrefabGo.SetActive(false);
        lrPrefabGo.transform.SetParent(managerGo.transform);

        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupCommonUI(out GameObject canvasGo, out Button btnBack)
    {
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.16f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        btnBack = CreateButton(canvasGo.transform, "BtnBack", "Back", new Vector2(120, -40), new Vector2(200, 40));
        btnBack.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        btnBack.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize = 24)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return t;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        
        var txt = CreateText(go.transform, "Text", text, Vector2.zero, size);
        txt.color = Color.black;
        
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return btn;
    }

    private static GameObject CreateOptionButtonPrefab()
    {
        // Actually, we don't need a real prefab in the Project. We can just keep a hidden object in the scene.
        var go = new GameObject("OptionButtonPrefab");
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        var txt = CreateText(go.transform, "Text", "Option", Vector2.zero, new Vector2(400, 100));
        txt.color = Color.black;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);
        go.SetActive(false);
        return go;
    }
}
