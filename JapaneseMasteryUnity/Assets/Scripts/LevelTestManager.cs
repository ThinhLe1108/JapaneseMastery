using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Forced re-compile: 2026-06-16 11:00
public class LevelTestManager : MonoBehaviour
{
    private static bool _instanceExists = false;

    [Header("UI References")]
    public TextMeshProUGUI progressLabel;
    public TextMeshProUGUI wordLabel;
    public TextMeshProUGUI promptLabel;
    public Transform optionsContainer;
    public Button btnSubmit;
    public Button btnBack;
    public TextMeshProUGUI feedbackLabel;
    public GameObject optionButtonPrefab;
    public Image backgroundImage;

    // Logic Data
    private List<CurriculumItem> vocabData = new List<CurriculumItem>();
    private List<Dictionary<string, object>> questionPool = new List<Dictionary<string, object>>();
    private int currentQIdx = 0;
    private int score = 0;
    private int passingScore = 15; 
    private string state = "LOADING";
    private Dictionary<string, object> currentQ = new Dictionary<string, object>();
    private int selectedOption = -1;

    void Awake()
    {
        Debug.Log("LevelTest: Manager Awake");
        if (_instanceExists) {
            Debug.LogWarning("LevelTest: Duplicate manager detected. Destroying.");
            Destroy(gameObject);
            return;
        }
        _instanceExists = true;
    }

    private void OnDestroy() { if (_instanceExists) _instanceExists = false; }

    void Start()
    {
        Debug.Log("LevelTest: Manager Start");
        if (FindObjectOfType<EventSystem>() == null) {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        FindUIReferences();
        SetupUI();
        StartCoroutine(InitializationSequence());
    }

    void FindUIReferences()
    {
        if (wordLabel == null) wordLabel = GameObject.Find("WordLabel")?.GetComponent<TextMeshProUGUI>();
        if (promptLabel == null) promptLabel = GameObject.Find("PromptLabel")?.GetComponent<TextMeshProUGUI>();
        if (progressLabel == null) progressLabel = GameObject.Find("ProgressLabel")?.GetComponent<TextMeshProUGUI>();
        if (feedbackLabel == null) feedbackLabel = GameObject.Find("FeedbackLabel")?.GetComponent<TextMeshProUGUI>();
        if (optionsContainer == null) optionsContainer = GameObject.Find("OptionsContainer")?.transform;
        if (btnSubmit == null) btnSubmit = GameObject.Find("BtnSubmit")?.GetComponent<Button>();
        if (btnBack == null) btnBack = GameObject.Find("BtnBack")?.GetComponent<Button>();
        if (backgroundImage == null) backgroundImage = GameObject.Find("Background")?.GetComponent<Image>();
        
        if (optionButtonPrefab == null) optionButtonPrefab = GameObject.Find("OptionButtonPrefab");
        
        Debug.Log($"LevelTest: Refs - Word:{wordLabel != null}, Prompt:{promptLabel != null}, Progress:{progressLabel != null}, Options:{optionsContainer != null}, Prefab:{optionButtonPrefab != null}");
    }

    void SetupUI()
    {
        Color bgColor = new Color(0.12f, 0.12f, 0.18f);
        if (backgroundImage != null) backgroundImage.color = bgColor;

        if (btnBack != null) {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(() => SceneManager.LoadScene("Main"));
            var t = btnBack.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) { 
                t.gameObject.SetActive(true);
                t.text = "Cancel & Main Menu"; 
                t.color = Color.white;
                t.fontSize = 16;
            }
            if (btnBack.image != null) {
                btnBack.image.color = new Color(0.15f, 0.15f, 0.2f);
                btnBack.image.raycastTarget = true;
            }
            btnBack.transform.SetAsLastSibling();
        }
        
        if (btnSubmit != null) {
            btnSubmit.onClick.RemoveAllListeners();
            btnSubmit.onClick.AddListener(OnSubmitPressed);
            var t = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) { 
                t.gameObject.SetActive(true);
                t.text = "Confirm"; 
                t.color = Color.gray; 
                t.fontSize = 20;
                t.alignment = TextAlignmentOptions.Center;
            }
            btnSubmit.interactable = false;
            btnSubmit.transform.SetAsLastSibling();
        }

        if (wordLabel != null) {
            wordLabel.text = "LOADING...";
            wordLabel.color = Color.white;
            wordLabel.fontSizeMax = 50; 
            wordLabel.fontSizeMin = 18;
            wordLabel.enableAutoSizing = true;
        }

        if (optionsContainer != null) {
            var grid = optionsContainer.GetComponent<GridLayoutGroup>();
            if (grid != null) {
                grid.cellSize = new Vector2(320, 75); 
                grid.spacing = new Vector2(20, 20);
            }
        }

        if (feedbackLabel != null) {
            feedbackLabel.gameObject.SetActive(false);
            feedbackLabel.fontSizeMax = 30;
            feedbackLabel.enableAutoSizing = true;
        }
    }

    IEnumerator InitializationSequence()
    {
        int level = Global.CurrentLevel;
        Debug.Log($"LevelTest: Loading level {level}");
        vocabData = Curriculum.GetLevel(level);

        if (vocabData == null || vocabData.Count == 0) {
            Debug.LogWarning("LevelTest: Local data empty.");
        }

        if (vocabData == null || vocabData.Count == 0) {
            if (wordLabel != null) wordLabel.text = "ERROR: NO DATA";
        } else {
            GenerateQuestions();
        }
        yield return null;
    }

    void GenerateQuestions()
    {
        Debug.Log("LevelTest: Generating 20 questions...");
        questionPool.Clear();
        
        var words = vocabData.OrderBy(a => Random.value).ToList();
        if (words.Count == 0) {
            Debug.LogError("LevelTest: No words in vocabData!");
            return;
        }

        for (int i = 0; i < 20; i++) {
            var v = words[i % words.Count];
            string type = (i % 2 == 0) ? "reading" : "meaning";
            questionPool.Add(new Dictionary<string, object> { { "vocab", v }, { "type", type } });
        }
        
        questionPool = questionPool.OrderBy(a => Random.value).ToList();
        currentQIdx = 0;
        score = 0;
        Debug.Log($"LevelTest: Pool built with {questionPool.Count} questions.");
        NextQuestion();
    }

    void NextQuestion()
    {
        Debug.Log($"LevelTest: NextQuestion index {currentQIdx}");
        if (currentQIdx >= questionPool.Count) { ShowSummary(); return; }

        state = "PLAYING";
        selectedOption = -1;
        if (progressLabel != null) progressLabel.text = $"Question {currentQIdx + 1} / {questionPool.Count}";
        if (feedbackLabel != null) feedbackLabel.gameObject.SetActive(false);
        if (wordLabel != null) wordLabel.gameObject.SetActive(true);
        if (promptLabel != null) promptLabel.gameObject.SetActive(true);
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(true);

        var q = questionPool[currentQIdx];
        if (!q.ContainsKey("vocab") || q["vocab"] == null) {
            Debug.LogError("LevelTest: Question vocab is NULL!");
            currentQIdx++; NextQuestion(); return;
        }

        CurriculumItem vocab = (CurriculumItem)q["vocab"];
        string qType = q.ContainsKey("type") ? q["type"].ToString() : "reading";

        string correctAns = "";
        List<string> choices = new List<string>();

        if (qType == "reading") {
            correctAns = string.IsNullOrEmpty(vocab.romaji) ? vocab.meaning : vocab.romaji;
            if (wordLabel != null) wordLabel.text = vocab.kana; 
            if (promptLabel != null) promptLabel.text = "Choose the reading:";
            foreach (var v in vocabData) { 
                string s = string.IsNullOrEmpty(v.romaji) ? v.meaning : v.romaji; 
                if (s != correctAns && !choices.Contains(s)) choices.Add(s); 
            }
        } else {
            correctAns = string.IsNullOrEmpty(vocab.meaning) ? vocab.romaji : vocab.meaning;
            if (wordLabel != null) wordLabel.text = vocab.kana; 
            if (promptLabel != null) promptLabel.text = "Choose the meaning:";
            foreach (var v in vocabData) { 
                string s = string.IsNullOrEmpty(v.meaning) ? v.romaji : v.meaning; 
                if (s != correctAns && !choices.Contains(s)) choices.Add(s); 
            }
        }

        if (choices.Count < 3) {
            string[] fillers = { "a", "i", "u", "e", "o", "ka", "ki", "ku", "ke", "ko" };
            foreach(var f in fillers) if (f != correctAns && !choices.Contains(f) && choices.Count < 3) choices.Add(f);
        }

        choices = choices.OrderBy(a => Random.value).Take(3).ToList();
        choices.Add(correctAns);
        choices = choices.OrderBy(a => Random.value).ToList();

        currentQ["correct"] = correctAns;
        currentQ["options"] = choices;

        if (optionsContainer != null) {
            optionsContainer.gameObject.SetActive(true);
            Debug.Log("LevelTest: Clearing and building options...");
            foreach (Transform child in optionsContainer) Destroy(child.gameObject);
            
            for (int i = 0; i < choices.Count; i++) {
                int idx = i;
                if (optionButtonPrefab == null) {
                    Debug.LogError("LevelTest: optionButtonPrefab is NULL!");
                    continue;
                }
                GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
                btnObj.SetActive(true);
                Button btn = btnObj.GetComponent<Button>();
                if (btn == null) {
                    Debug.LogError("LevelTest: Button component missing on instantiated option!");
                    continue;
                }
                
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) {
                    txt.gameObject.SetActive(true);
                    txt.text = choices[i];
                    txt.color = Color.white;
                    txt.enableAutoSizing = true;
                    txt.fontSizeMin = 8;
                    txt.fontSizeMax = 18;
                    txt.enableWordWrapping = true;
                    txt.alignment = TextAlignmentOptions.Center;
                    
                    // Force padding via RectTransform if possible
                    var rt = txt.GetComponent<RectTransform>();
                    if (rt != null) {
                        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                        rt.offsetMin = new Vector2(10, 5); rt.offsetMax = new Vector2(-10, -5);
                    }
                }

                if (btn.image != null) btn.image.color = new Color(0.15f, 0.15f, 0.2f);
                btn.onClick.AddListener(() => OnOptionPressed(idx));
            }
        }

        if (btnSubmit != null) {
            var submitText = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
            if (submitText != null) {
                submitText.text = "Confirm";
                submitText.color = Color.gray;
            }
            btnSubmit.interactable = false;
        }
    }

    void OnOptionPressed(int idx)
    {
        if (state != "PLAYING") return;
        selectedOption = idx;
        if (btnSubmit != null) {
            btnSubmit.interactable = true;
            var t = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.color = Color.white;
        }
        
        int i = 0;
        foreach (Transform child in optionsContainer) {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.image.color = (i == idx) ? new Color(0.25f, 0.25f, 0.35f) : new Color(0.15f, 0.15f, 0.2f);
            i++;
        }
    }

    void OnSubmitPressed()
    {
        if (state == "PLAYING") {
            var choicesList = (List<string>)currentQ["options"];
            bool correct = choicesList[selectedOption] == (string)currentQ["correct"];
            
            if (correct) { 
                score++; 
                feedbackLabel.text = "Correct!"; 
                feedbackLabel.color = Color.green; 
            } else { 
                feedbackLabel.text = "Wrong! Answer: " + currentQ["correct"]; 
                feedbackLabel.color = Color.red; 
            }
            
            if (wordLabel != null) wordLabel.gameObject.SetActive(false);
            if (promptLabel != null) promptLabel.gameObject.SetActive(false);
            if (optionsContainer != null) optionsContainer.gameObject.SetActive(false);

            feedbackLabel.gameObject.SetActive(true);
            var t = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = "Next";
            state = "RESULT";
        } else if (state == "RESULT") {
            currentQIdx++;
            NextQuestion();
        } else if (state == "SUMMARY") {
            SceneManager.LoadScene("Main");
        }
    }

    void ShowSummary()
    {
        state = "SUMMARY";
        if (wordLabel != null) wordLabel.gameObject.SetActive(false);
        if (promptLabel != null) promptLabel.gameObject.SetActive(false);
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(false);
        if (progressLabel != null) progressLabel.gameObject.SetActive(false);
        
        bool passed = (score >= passingScore);
        if (feedbackLabel != null) {
            feedbackLabel.text = passed ? "LEVEL PASSED!" : "LEVEL FAILED";
            feedbackLabel.text += $"\nYour Score: {score} / {questionPool.Count}";
            feedbackLabel.color = passed ? Color.green : Color.red;
            feedbackLabel.gameObject.SetActive(true);
        }

        if (passed) {
            Global.CurrentLevel++;
        }

        if (btnSubmit != null) {
            var t = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = "Main Menu";
            btnSubmit.interactable = true;
        }
    }
}
