using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;
using System.IO;
using System.Linq;

public class SoloLearningManager : MonoBehaviour
{
    private List<CurriculumItem> vocabData = new List<CurriculumItem>();
    private int currentIdx = 0;
    private string state = "READING";

    [Header("UI References")]
    public TextMeshProUGUI wordLabel;
    public TextMeshProUGUI romajiLabel;
    public TextMeshProUGUI drawPrompt;
    public Button btnNext;
    public Button btnSkip;
    public Button btnBack;
    public Transform drawCanvas;

    [Header("Settings")]
    public GameObject lineRendererPrefab;
    private float traceTolerance = 45.0f;
    private float currentStrokeWidth = 16.0f;

    private Dictionary<string, List<string>> kanjivgData = new Dictionary<string, List<string>>();
    private Dictionary<string, List<List<Vector2>>> strokeCache = new Dictionary<string, List<List<Vector2>>>();
    private List<List<Vector2>> bgStrokes = new List<List<Vector2>>();
    private List<Vector2> activeStrokePoints = new List<Vector2>();

    private List<List<Vector2>> GetStrokesForCharacter(char c)
    {
        // 0. Normalize Character: Handle full-width characters (like 'Ａ') by converting them to standard ASCII
        if (c >= '！' && c <= '～') c = (char)(c - 0xFEE0);
        else if (c == '　') c = ' ';

        string s = c.ToString();
        if (strokeCache.TryGetValue(s, out var cached)) return cached;

        List<List<Vector2>> strokes = new List<List<Vector2>>();

        // 1. Try to load from SVG file in StreamingAssets/kanji/
        string hex = ((int)c).ToString("x5");
        string path = Path.Combine(Application.streamingAssetsPath, "kanji", hex + ".svg");

        if (File.Exists(path))
        {
            try {
                string xml = File.ReadAllText(path);
                var matches = System.Text.RegularExpressions.Regex.Matches(xml, @"<path[^>]*d=""([^""]+)""");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    strokes.Add(ParseSVGPath(m.Groups[1].Value));
                }
                Debug.Log("SoloLearningManager: Loaded strokes for '" + c + "' from SVG: " + hex + ".svg");
            } catch (System.Exception e) {
                Debug.LogError("SoloLearningManager: SVG Parse Error for " + c + ": " + e.Message);
            }
        }
        else if (c == '○') // Circle fallback
        {
            List<Vector2> circle = new List<Vector2>();
            for(int i=0; i<=360; i+=15) {
                float rad = i * Mathf.Deg2Rad;
                circle.Add(new Vector2(54.5f + Mathf.Cos(rad) * 45f, 54.5f + Mathf.Sin(rad) * 45f));
            }
            strokes.Add(circle);
        }
        else if (c == '〃' || (int)c == 0x3003) // Procedural fallback for the "ditto" / iteration mark
        {
            strokes.Add(new List<Vector2> { new Vector2(35, 75), new Vector2(45, 65) });
            strokes.Add(new List<Vector2> { new Vector2(55, 75), new Vector2(65, 65) });
        }

        // 2. Fallback to pre-loaded JSON data
        if (strokes.Count == 0 && kanjivgData != null && kanjivgData.TryGetValue(s, out var paths))
        {
            foreach (var p in paths) strokes.Add(ParseSVGPath(p));
            Debug.Log("SoloLearningManager: Loaded strokes for '" + c + "' from JSON fallback.");
        }

        if (strokes.Count > 0)
        {
            strokeCache[s] = strokes;
            return strokes;
        }

        Debug.LogWarning("SoloLearningManager: No stroke data found for character '" + c + "' (Hex: " + hex + ")");
        return null;
    }
    private Dictionary<string, LineRenderer> lrCache = new Dictionary<string, LineRenderer>();
    
    private int currentStrokeIdx = 0;
    private int currentStrokeProgress = 0;
    private bool isTracing = false;
    private int canvasSortingLayer;
    private int canvasSortingOrder;
    private GameObject strokesContainer;

    private void Awake()
    {
        Debug.Log("SoloLearningManager: Awake - Global Cleanup...");

        // Ensure we only have ONE manager
        SoloLearningManager[] managers = FindObjectsOfType<SoloLearningManager>(true);
        foreach (var m in managers) {
            if (m != this) DestroyImmediate(m.gameObject);
        }

        // Ensure EventSystem exists
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null) {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    void Start()
    {
        if (Global.Instance != null) Global.Instance.EnsureCamera();
        
        // Camera adjustments removed to avoid breaking UI layout

        FullUIReset();
        
        btnBack.onClick.RemoveAllListeners();
        btnBack.onClick.AddListener(OnBackPressed);

        btnSkip.onClick.RemoveAllListeners();
        btnSkip.onClick.AddListener(SkipVocab);

        btnNext.onClick.RemoveAllListeners();
        btnNext.onClick.AddListener(OnNextPressed);

        LoadKanjiVG();
        
        int targetLevel = Global.SoloTargetLevel;
        if (targetLevel <= 0) targetLevel = 1;
        StartCoroutine(FetchVocabFromServer(targetLevel));
    }

    void FullUIReset()
    {
        if (wordLabel != null) { wordLabel.text = "Loading..."; wordLabel.gameObject.SetActive(true); wordLabel.raycastTarget = false; }
        if (romajiLabel != null) { romajiLabel.gameObject.SetActive(false); romajiLabel.raycastTarget = false; }
        if (drawPrompt != null) { drawPrompt.gameObject.SetActive(false); drawPrompt.raycastTarget = false; }
        if (drawCanvas != null) { drawCanvas.gameObject.SetActive(false); }

        if (btnBack != null) { btnBack.gameObject.SetActive(true); btnBack.GetComponent<Image>().raycastTarget = true; }
        if (btnSkip != null) { btnSkip.gameObject.SetActive(true); btnSkip.GetComponent<Image>().raycastTarget = true; }
        if (btnNext != null) { btnNext.gameObject.SetActive(false); btnNext.GetComponent<Image>().raycastTarget = true; }
        
        ClearTraceVisuals();
    }

    IEnumerator FetchVocabFromServer(int level)
    {
        Debug.Log("SoloLearningManager: Loading level " + level);
        List<CurriculumItem> rawList = Curriculum.GetLevel(level);
        if (rawList == null) rawList = new List<CurriculumItem>();
        
        // Populate vocabData from rawList (offline) with duplication logic for safety
        vocabData.Clear();
        foreach (var item in rawList)
        {
            if (string.IsNullOrEmpty(item.wordJp)) item.wordJp = item.kana;
            if (string.IsNullOrEmpty(item.kana)) item.kana = item.wordJp;

            if (HasKanji(item.wordJp))
            {
                vocabData.Add(new CurriculumItem { wordJp = item.wordJp, kana = item.kana, romaji = item.romaji, meaning = item.meaning });
                vocabData.Add(new CurriculumItem { wordJp = item.kana, kana = item.kana, romaji = item.romaji, meaning = item.meaning });
            }
            else vocabData.Add(item);
        }

        if (NetworkManager.Instance != null)
        {
            string url = NetworkManager.Instance.BASE_URL + $"/api/player/vocabularies?level={level}";
            if (Global.UserId != -1) url += $"&userId={Global.UserId}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                    if (list != null && list.Count > 0)
                    {
                        Debug.Log("SoloLearningManager: Found " + list.Count + " words from server.");
                        // Backend already handles duplication, so we just clear and add
                        vocabData.Clear();
                        foreach (var item in list)
                        {
                            vocabData.Add(new CurriculumItem {
                                wordJp = item.ContainsKey("wordJp") ? item["wordJp"].ToString() : "",
                                kana = item.ContainsKey("kana") ? item["kana"].ToString() : "",
                                romaji = item.ContainsKey("romaji") ? item["romaji"].ToString() : "",
                                meaning = item.ContainsKey("meaning") ? item["meaning"].ToString() : ""
                            });
                        }
                    }
                }
            }
        }

        if (vocabData.Count == 0) {
            Debug.LogWarning("SoloLearningManager: No data found! Using fallback.");
            vocabData = new List<CurriculumItem> { new CurriculumItem { wordJp = "日本語", kana = "日本語", romaji = "nihongo", meaning = "Japanese" } };
        }

        currentIdx = 0;
        ShowReadingPhase();
    }



    bool HasKanji(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s)
        {
            if (c >= '\u4e00' && c <= '\u9faf') return true;
        }
        return false;
    }

    void ShowReadingPhase()
    {
        state = "READING";
        FullUIReset();
        
        if (wordLabel != null) wordLabel.gameObject.SetActive(true); // Ensure GameObject is active

        var vocab = vocabData[currentIdx];
        if (wordLabel != null) {
            wordLabel.enabled = true; // Ensure component is enabled
            wordLabel.text = vocab.wordJp; 
            wordLabel.enableAutoSizing = true;
            wordLabel.fontSizeMin = 20;
            wordLabel.fontSizeMax = 120; 
            wordLabel.color = Color.white;
            wordLabel.gameObject.SetActive(true);
            
            // Ensure centered
            var rt = wordLabel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 50);
        }

        if (romajiLabel != null) {
            romajiLabel.text = "Reading: " + vocab.romaji + (string.IsNullOrEmpty(vocab.meaning) ? "" : "\nMeaning: " + vocab.meaning);
            // If we are currently showing Kanji version, show the Kana reading in the label too
            if (vocab.wordJp != vocab.kana && !string.IsNullOrEmpty(vocab.kana)) {
                romajiLabel.text = "Reading: " + vocab.kana + " (" + vocab.romaji + ")" + (string.IsNullOrEmpty(vocab.meaning) ? "" : "\nMeaning: " + vocab.meaning);
            }
            
            romajiLabel.gameObject.SetActive(true);
            romajiLabel.color = new Color(0.6f, 0.8f, 1.0f);
            romajiLabel.enableWordWrapping = true;
            romajiLabel.enableAutoSizing = false;
            romajiLabel.fontSize = 28; 
            
            // Move to Top-Left, close to Back button
            var rt = romajiLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -70); 
            rt.sizeDelta = new Vector2(800, 300); 
            romajiLabel.alignment = TextAlignmentOptions.TopLeft;
        }
        
        if (btnNext != null) {
            btnNext.gameObject.SetActive(true);
            var t = btnNext.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = "Start Tracing";
        }
        
        Debug.Log("SoloLearning: Displaying vocab: " + vocab.wordJp);
    }


    void ShowWritingPhase()
    {
        state = "WRITING";
        // COMPLETELY DEACTIVATE wordLabel to hide its "T" gizmo and text
        if (wordLabel != null) wordLabel.gameObject.SetActive(false);
        
        if (romajiLabel != null) romajiLabel.gameObject.SetActive(false);
        if (drawCanvas != null) drawCanvas.gameObject.SetActive(true);
        if (drawPrompt != null) {
            drawPrompt.gameObject.SetActive(true);
            drawPrompt.text = "Trace the word";
            drawPrompt.color = Color.white;
        }
        
        if (btnNext != null) btnNext.gameObject.SetActive(false);
        StartTracingMode();
    }

    void ShowSummary()
    {
        state = "SUMMARY";
        ClearTraceVisuals();
        drawCanvas.gameObject.SetActive(false);
        drawPrompt.gameObject.SetActive(false);
        btnBack.gameObject.SetActive(false);
        btnSkip.gameObject.SetActive(false);
        
        if (wordLabel != null) {
            wordLabel.enableAutoSizing = true;
            wordLabel.fontSizeMin = 20;
            wordLabel.fontSizeMax = 50; // Slightly smaller summary header
            wordLabel.gameObject.SetActive(true);
            wordLabel.text = "Completed Level " + Global.SoloTargetLevel + "!";
            wordLabel.color = Color.white;
            
            var rt = wordLabel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 50);
        }

        if (romajiLabel != null) {
            romajiLabel.text = "Take the Level-up Test to unlock the next level.";
            romajiLabel.gameObject.SetActive(true);
            romajiLabel.enableWordWrapping = true;
            romajiLabel.enableAutoSizing = false;
            romajiLabel.fontSize = 28; // Keep consistency
            
            // Reset to Center for summary
            var rt = romajiLabel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -50);
            rt.sizeDelta = new Vector2(800, 100);
            romajiLabel.alignment = TextAlignmentOptions.Center;
        }

        if (btnNext != null) {
            btnNext.gameObject.SetActive(true);
            var t = btnNext.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = "Main Menu";
        }
    }

    void SkipVocab()
    {
        currentIdx++;
        if (currentIdx >= vocabData.Count) ShowSummary();
        else ShowReadingPhase();
    }

    void OnBackPressed()
    {
        if (state == "SUMMARY" || (state == "READING" && currentIdx <= 0)) 
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        else if (state == "WRITING") 
            ShowReadingPhase();
        else { 
            currentIdx--; 
            ShowReadingPhase(); 
        }
    }

    void OnNextPressed()
    {
        if (state == "READING") ShowWritingPhase();
        else if (state == "WRITING") SkipVocab();
        else if (state == "SUMMARY") UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }

    void LoadKanjiVG()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "kanjivg_paths.json");
        if (File.Exists(path)) kanjivgData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(path));
    }

    void StartTracingMode()
    {
        currentStrokeIdx = 0; currentStrokeProgress = 0;
        activeStrokePoints.Clear(); isTracing = false; bgStrokes.Clear();

        string vocab = vocabData[currentIdx].wordJp;
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        
        // Ensure Camera is positioned to see the world-space strokes (which will be at z=0)
        cam.transform.position = new Vector3(0, 0, -10f);
        cam.orthographic = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        // Ensure Canvas doesn't block the world-space strokes
        Canvas cv = FindObjectOfType<Canvas>();
        if (cv != null) {
            cv.renderMode = RenderMode.ScreenSpaceCamera;
            cv.worldCamera = cam;
            cv.planeDistance = 15f; // Place UI behind the strokes (at z=0)
        }

        float worldHeight = cam.orthographicSize * 2.0f;
        float worldWidth = worldHeight * cam.aspect;
        
        // Target larger size: ~55% of the screen height
        float charTargetHeight = worldHeight * 0.55f; 
        float scaleFactor = charTargetHeight / 109.0f;
        
        float charSize = 109.0f * scaleFactor;
        float spacing = 15.0f * scaleFactor;
        float totalWidth = vocab.Length * charSize + (vocab.Length - 1) * spacing;

        // Scale down only if it exceeds 90% of screen width
        float maxAllowedWidth = worldWidth * 0.9f;
        if (totalWidth > maxAllowedWidth) {
            float r = maxAllowedWidth / totalWidth;
            scaleFactor *= r;
            charSize *= r;
            spacing *= r;
            totalWidth = maxAllowedWidth;
        }

        // Set stroke width to be more elegant and less "bulky"
        currentStrokeWidth = Mathf.Max(0.05f, charSize * 0.08f);
        // Drastically tighter tolerance for absolute precision
        traceTolerance = charSize * 0.05f;

        // Position characters centered in world space at z=0
        float startX = -totalWidth / 2.0f;
        float startY = -charSize / 2.0f;

        for (int i = 0; i < vocab.Length; i++) {
            char character = vocab[i];
            Vector2 offset = new Vector2(startX + i * (charSize + spacing), startY);
            var strokes = GetStrokesForCharacter(character);
            if (strokes != null) {
                foreach (var pts in strokes) {
                    List<Vector2> final = new List<Vector2>();
                    // Flip Y from SVG (top-down) to Unity World (bottom-up)
                    foreach(var pt in pts) final.Add(new Vector2(pt.x, 109.0f - pt.y) * scaleFactor + offset);
                    bgStrokes.Add(final);
                }
            }
        }
        
        if (bgStrokes.Count == 0) { 
            drawPrompt.text = "No tracing data."; 
            btnNext.gameObject.SetActive(true); 
            var t = btnNext.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = "Continue"; 
        }

        // Adjust prompt position to top center
        if (drawPrompt != null) {
            var prt = drawPrompt.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 1);
            prt.anchorMax = new Vector2(0.5f, 1);
            prt.anchoredPosition = new Vector2(0, -80);
        }

        UpdateTraceVisuals();
    }

    void ClearTraceVisuals() { if (strokesContainer != null) DestroyImmediate(strokesContainer); strokesContainer = null; lrCache.Clear(); }

    void UpdateTraceVisuals()
    {
        if (strokesContainer == null) { 
            strokesContainer = new GameObject("StrokesContainer"); 
            strokesContainer.transform.position = Vector3.zero; 
        }
        for (int i = 0; i < bgStrokes.Count; i++) {
            // Further dimmed colors for better focus on the active line:
            // Finished: Muted Sage Green
            // Target: Darker Gold
            // Guide (Future): Very faint gray
            Color c = i < currentStrokeIdx ? new Color(0.2f, 0.5f, 0.2f) : (i == currentStrokeIdx ? new Color(0.6f, 0.5f, 0.2f) : new Color(0.1f, 0.1f, 0.1f, 0.2f));
            GetOrCreateLR("BgStroke_" + i, c, 100 + i, bgStrokes[i]);
        }
        
        if (currentStrokeIdx < bgStrokes.Count) {
            // Active stroke is pure white for maximum focus
            if (activeStrokePoints.Count > 0) GetOrCreateLR("ActiveStroke", Color.white, 200, activeStrokePoints);
            else if (lrCache.ContainsKey("ActiveStroke")) lrCache["ActiveStroke"].positionCount = 0;
        } else {
            // Word finished: Clear the active stroke so it doesn't overlap the green finished strokes
            if (lrCache.ContainsKey("ActiveStroke")) lrCache["ActiveStroke"].positionCount = 0;
            
            if (bgStrokes.Count > 0) { 
                drawPrompt.text = "Perfect!"; 
                drawPrompt.color = new Color(0.4f, 1.0f, 0.4f); 
                btnNext.gameObject.SetActive(true); 
                var t = btnNext.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = "Next Word";
            }
        }

        // Fallback: If no stroke data was found, show the wordLabel as a faint guide so the user isn't stuck
        if (bgStrokes.Count == 0 && wordLabel != null) {
            wordLabel.enabled = true;
            wordLabel.color = new Color(1f, 1f, 1f, 0.15f);
            wordLabel.gameObject.SetActive(true);
        }
    }

    LineRenderer GetOrCreateLR(string id, Color color, int order, List<Vector2> points)
    {
        if (!lrCache.TryGetValue(id, out LineRenderer lr)) {
            GameObject go = lineRendererPrefab != null ? Instantiate(lineRendererPrefab, strokesContainer.transform) : new GameObject(id);
            go.name = id; 
            go.transform.SetParent(strokesContainer.transform);
            go.SetActive(true); // CRITICAL: Prefab is inactive in scene, so we MUST activate the instance
            
            lr = go.GetComponent<LineRenderer>() ?? go.AddComponent<LineRenderer>();
            
            // Ensure we have a visible material that supports vertex colors
            if (lr.sharedMaterial == null || lr.sharedMaterial.name == "Default-Material" || lr.sharedMaterial.name == "None") {
                Shader s = Shader.Find("Sprites/Default");
                if (s == null) s = Shader.Find("Unlit/Color");
                if (s != null) lr.material = new Material(s);
            }
            
            lr.useWorldSpace = true; 
            lr.numCapVertices = 8; 
            lr.numCornerVertices = 8;
            lr.sortingOrder = order;
            lrCache[id] = lr;
        }
        lr.startWidth = lr.endWidth = currentStrokeWidth; 
        lr.startColor = lr.endColor = color;
        lr.positionCount = points.Count;
        // Render slightly in front of UI (z=0)
        for (int i = 0; i < points.Count; i++) lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        return lr;
    }

    void Update()
    {
        if (state != "WRITING") return;
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;
        
        Vector3 mPos = Input.mousePosition;
        // Use depth based on camera distance to z=0
        mPos.z = Mathf.Abs(cam.transform.position.z);
        Vector2 worldPos = cam.ScreenToWorldPoint(mPos);

        if (Input.GetMouseButtonDown(0)) { isTracing = true; ProcessTracing(worldPos); }
        if (Input.GetMouseButtonUp(0)) isTracing = false;
        if (isTracing) ProcessTracing(worldPos);
    }

    void ProcessTracing(Vector2 pos)
    {
        if (currentStrokeIdx >= bgStrokes.Count) return;
        var stroke = bgStrokes[currentStrokeIdx];
        bool advanced = false;
        // Very small look-ahead to keep the line strictly following the mouse
        int lookAhead = 3; 
        for (int i = 0; i < lookAhead; i++) {
            int idx = currentStrokeProgress + i;
            if (idx >= stroke.Count) break;
            
            // Only advance if the mouse is truly on top of the path
            if (Vector2.Distance(pos, stroke[idx]) < traceTolerance) {
                while (currentStrokeProgress <= idx) { 
                    activeStrokePoints.Add(stroke[currentStrokeProgress]); 
                    currentStrokeProgress++; 
                }
                advanced = true; 
                break;
            }
        }
        if (advanced) {
            UpdateTraceVisuals();
            if (currentStrokeProgress >= stroke.Count) { 
                currentStrokeIdx++; 
                currentStrokeProgress = 0; 
                activeStrokePoints.Clear(); 
                isTracing = false; 
                UpdateTraceVisuals(); 
            }
        }
    }

    private List<Vector2> ParseSVGPath(string pathStr)
    {
        List<Vector2> points = new List<Vector2>();
        var matches = System.Text.RegularExpressions.Regex.Matches(pathStr, @"([A-Za-z])|(-?[0-9]*\.?[0-9]+)");
        int i = 0; string cmd = ""; Vector2 cur = Vector2.zero; Vector2 prevIn = Vector2.zero;
        while (i < matches.Count) {
            string m = matches[i].Value;
            if (m.Length == 1 && char.IsLetter(m[0])) { cmd = m; i++; }
            if (i >= matches.Count) break;
            float next() => float.Parse(matches[i++].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (cmd == "M" || cmd == "m") { Vector2 v = new Vector2(next(), next()); cur = (cmd == "m" && points.Count > 0) ? cur + v : v; points.Add(cur); prevIn = Vector2.zero; }
            else if (cmd == "L" || cmd == "l") { Vector2 v = new Vector2(next(), next()); cur = (cmd == "l") ? cur + v : v; points.Add(cur); }
            else if (cmd == "H" || cmd == "h") { float x = next(); cur = new Vector2(cmd == "h" ? cur.x + x : x, cur.y); points.Add(cur); }
            else if (cmd == "V" || cmd == "v") { float y = next(); cur = new Vector2(cur.x, cmd == "v" ? cur.y + y : y); points.Add(cur); }
            else if (cmd == "C" || cmd == "c") {
                Vector2 p1 = cur + (cmd == "c" ? new Vector2(next(), next()) : new Vector2(next() - cur.x, next() - cur.y));
                Vector2 p2 = cur + (cmd == "c" ? new Vector2(next(), next()) : new Vector2(next() - cur.x, next() - cur.y));
                Vector2 p3 = cur + (cmd == "c" ? new Vector2(next(), next()) : new Vector2(next() - cur.x, next() - cur.y));
                points.AddRange(Tessellate(cur, p1, p2, p3)); prevIn = p2 - p3; cur = p3;
            } else if (cmd == "S" || cmd == "s") {
                Vector2 p1 = cur - prevIn;
                Vector2 p2 = cur + (cmd == "s" ? new Vector2(next(), next()) : new Vector2(next() - cur.x, next() - cur.y));
                Vector2 p3 = cur + (cmd == "s" ? new Vector2(next(), next()) : new Vector2(next() - cur.x, next() - cur.y));
                points.AddRange(Tessellate(cur, p1, p2, p3)); prevIn = p2 - p3; cur = p3;
            } else i++;
        }
        return points;
    }

    private List<Vector2> Tessellate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        List<Vector2> pts = new List<Vector2>();
        for (int i = 1; i <= 20; i++) { float t = i / 20f; float u = 1 - t; pts.Add((u * u * u * p0) + (3 * u * u * t * p1) + (3 * u * t * t * p2) + (t * t * t * p3)); }
        return pts;
    }
}
