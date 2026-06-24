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

public class CustomTestRoom : MonoBehaviour
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
    private Dictionary<string, object> testData;
    private List<Dictionary<string, object>> questionPool = new List<Dictionary<string, object>>();
    private int currentQIdx = 0;
    private int score = 0;
    private int passingScore = 0; 
    private string state = "LOADING";
    private Dictionary<string, object> currentQ = new Dictionary<string, object>();
    private int selectedOption = -1;

    void Awake()
    {
        if (_instanceExists) {
            Destroy(gameObject);
            return;
        }
        _instanceExists = true;
    }

    private void OnDestroy() { if (_instanceExists) _instanceExists = false; }

    void Start()
    {
        if (FindObjectOfType<EventSystem>() == null) {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        FindUIReferences();
        SetupUI();

        testData = Global.CurrentCustomTest;
        if (testData != null && testData.ContainsKey("questions")) {
            string qJson = JsonConvert.SerializeObject(testData["questions"]);
            questionPool = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(qJson);
        }

        if (questionPool == null || questionPool.Count == 0) {
            if (wordLabel != null) wordLabel.text = "This test has no questions!";
            if (btnSubmit != null) btnSubmit.gameObject.SetActive(false);
        } else {
            passingScore = questionPool.Count; // Need 100% to pass
            GenerateQuestions();
        }
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
            }
        }
        
        if (btnSubmit != null) {
            btnSubmit.onClick.RemoveAllListeners();
            btnSubmit.onClick.AddListener(OnSubmitPressed);
            btnSubmit.interactable = false;
        }

        if (wordLabel != null) {
            wordLabel.text = "LOADING...";
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
            feedbackLabel.enableAutoSizing = true;
        }
    }

    void GenerateQuestions()
    {
        currentQIdx = 0;
        score = 0;
        NextQuestion();
    }

    void NextQuestion()
    {
        if (currentQIdx >= questionPool.Count) { ShowSummary(); return; }

        state = "PLAYING";
        selectedOption = -1;
        if (progressLabel != null) progressLabel.text = $"Question {currentQIdx + 1} / {questionPool.Count}";
        if (feedbackLabel != null) feedbackLabel.gameObject.SetActive(false);
        if (wordLabel != null) wordLabel.gameObject.SetActive(true);
        if (promptLabel != null) promptLabel.gameObject.SetActive(true);
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(true);

        var q = questionPool[currentQIdx];
        string questionText = q.ContainsKey("questionText") ? q["questionText"].ToString() : "";
        string correctLetter = q.ContainsKey("correctAnswer") ? q["correctAnswer"].ToString() : "A";
        string correctAns = q.ContainsKey("answer" + correctLetter) ? q["answer" + correctLetter].ToString() : "";

        if (wordLabel != null) wordLabel.text = questionText;
        if (promptLabel != null) promptLabel.text = "Choose the correct answer:";

        List<string> choices = new List<string>();
        if (q.ContainsKey("answerA") && !string.IsNullOrEmpty(q["answerA"].ToString())) choices.Add(q["answerA"].ToString());
        if (q.ContainsKey("answerB") && !string.IsNullOrEmpty(q["answerB"].ToString())) choices.Add(q["answerB"].ToString());
        if (q.ContainsKey("answerC") && !string.IsNullOrEmpty(q["answerC"].ToString())) choices.Add(q["answerC"].ToString());
        if (q.ContainsKey("answerD") && !string.IsNullOrEmpty(q["answerD"].ToString())) choices.Add(q["answerD"].ToString());

        if (choices.Count <= 1) {
            choices = new List<string> { correctAns };
            var fakePool = new List<string>();
            foreach(var other in questionPool) {
                string otherLetter = other.ContainsKey("correctAnswer") ? other["correctAnswer"].ToString() : "A";
                string otherAns = other.ContainsKey("answer" + otherLetter) ? other["answer" + otherLetter].ToString() : "";
                if (otherAns != correctAns) fakePool.Add(otherAns);
            }
            fakePool = fakePool.OrderBy(a => Random.value).ToList();
            for(int i=0; i < Mathf.Min(3, fakePool.Count); i++) choices.Add(fakePool[i]);
        }

        if (!choices.Contains(correctAns)) choices.Add(correctAns);
        choices = choices.OrderBy(a => Random.value).ToList();

        currentQ["correct"] = correctAns;
        currentQ["options"] = choices;

        if (optionsContainer != null) {
            optionsContainer.gameObject.SetActive(true);
            foreach (Transform child in optionsContainer) Destroy(child.gameObject);
            
            for (int i = 0; i < choices.Count; i++) {
                int idx = i;
                if (optionButtonPrefab == null) continue;
                GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
                btnObj.SetActive(true);
                Button btn = btnObj.GetComponent<Button>();
                if (btn == null) continue;
                
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
        if (btnBack != null) btnBack.gameObject.SetActive(false);
        
        bool passed = (score >= passingScore && passingScore > 0);
        int reward = testData.ContainsKey("rewardGcoin") ? System.Convert.ToInt32(testData["rewardGcoin"]) : 0;
        
        if (feedbackLabel != null) {
            if (passed) {
                feedbackLabel.text = $"CONGRATULATIONS!\nYou scored a perfect {score}/{questionPool.Count}.\nSubmitting results to server...";
                feedbackLabel.color = Color.green;
            } else {
                feedbackLabel.text = $"TOO BAD...\nYou scored {score}/{questionPool.Count}.\nRecording attempt...";
                feedbackLabel.color = Color.red;
            }
            feedbackLabel.gameObject.SetActive(true);
        }

        if (btnSubmit != null) {
            btnSubmit.gameObject.SetActive(false);
        }

        StartCoroutine(SubmitScoreCoroutine(passed, reward));
    }

    IEnumerator SubmitScoreCoroutine(bool passed, int reward)
    {
        int userId = Global.UserId;
        string testCode = testData.ContainsKey("testCode") ? testData["testCode"].ToString() : "";
        string url = NetworkManager.Instance.BASE_URL + $"/api/player/{userId}/custom-tests/{testCode}/submit";
        
        var bodyDict = new Dictionary<string, object> {
            { "score", score },
            { "passed", passed }
        };
        string bodyJson = JsonConvert.SerializeObject(bodyDict);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyJson));
            req.downloadHandler = new DownloadHandlerBuffer();
            
            yield return req.SendWebRequest();
            
            if (btnSubmit != null) {
                btnSubmit.gameObject.SetActive(true);
                var t = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(true);
                if (t != null) t.text = "Main Menu";
                btnSubmit.interactable = true;
            }

            if (req.result == UnityWebRequest.Result.Success) {
                if (passed && reward > 0) {
                    feedbackLabel.text = $"CONGRATULATIONS!\nYou scored a perfect {score}/{questionPool.Count}.\nYou received {reward} G-Coins!";
                    try {
                        var resData = JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                        if (resData != null && resData.ContainsKey("gcoin")) {
                            Global.Coins = System.Convert.ToInt32(resData["gcoin"]);
                        }
                    } catch {}
                } else if (!passed) {
                    feedbackLabel.text = $"TOO BAD...\nYou scored {score}/{questionPool.Count}.\nYou need 100% ({passingScore} questions) to get the reward.\nBetter luck next time!";
                }
            } else {
                feedbackLabel.text = $"Error saving results! (Code: {req.responseCode})";
            }
        }
    }
}
