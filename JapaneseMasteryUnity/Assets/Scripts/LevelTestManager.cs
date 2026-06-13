using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;
using System.Linq;

public class LevelTestManager : MonoBehaviour
{
    private List<CurriculumItem> vocabData = new List<CurriculumItem>();
    private List<Dictionary<string, object>> questionPool = new List<Dictionary<string, object>>();
    private int currentQIdx = 0;
    private int score = 0;
    private int passingScore = 8; // 8/10

    private string state = "PLAYING";
    private Dictionary<string, object> currentQ = new Dictionary<string, object>();
    private int selectedOption = -1;

    public TextMeshProUGUI progressLabel;
    public TextMeshProUGUI wordLabel;
    public TextMeshProUGUI promptLabel;
    public Transform optionsContainer;
    public Button btnSubmit;
    public Button btnBack;
    public TextMeshProUGUI feedbackLabel;

    public GameObject optionButtonPrefab;

    void Start()
    {
        if (Global.Instance != null) Global.Instance.EnsureCamera();

        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        btnSubmit.onClick.AddListener(OnSubmitPressed);

        int level = Global.CurrentLevel;

        wordLabel.text = "Loading data...";
        wordLabel.gameObject.SetActive(true);
        promptLabel.gameObject.SetActive(false);
        optionsContainer.gameObject.SetActive(false);
        btnSubmit.gameObject.SetActive(false);
        progressLabel.gameObject.SetActive(false);

        StartCoroutine(FetchVocabFromServer(level));
    }

    IEnumerator FetchVocabFromServer(int level)
    {
        var levelData = Curriculum.GetLevel(level);
        if (levelData.Count > 0)
        {
            vocabData = levelData;
            SetupQuestionPool();
            yield break;
        }

        int userId = Global.UserId;
        string url = NetworkManager.Instance.BASE_URL + $"/player/vocabularies?level={level}";
        if (userId != -1) url += $"&userId={userId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return webRequest.SendWebRequest();

            bool loaded = false;
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                if (list != null && list.Count > 0)
                {
                    vocabData.Clear();
                    foreach (var item in list)
                    {
                        vocabData.Add(new CurriculumItem {
                            kana = item.ContainsKey("wordJp") ? item["wordJp"].ToString() : "",
                            romaji = item.ContainsKey("romaji") ? item["romaji"].ToString() : "",
                            meaning = item.ContainsKey("meaning") ? item["meaning"].ToString() : ""
                        });
                    }
                    loaded = true;
                }
            }

            if (!loaded)
                vocabData = new List<CurriculumItem> { new CurriculumItem { kana = "Error", romaji = "Error" } };

            SetupQuestionPool();
        }
    }

    void SetupQuestionPool()
    {
        var basePool = new List<CurriculumItem>(vocabData);
        System.Random rng = new System.Random();
        
        // Shuffle base pool
        int n = basePool.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            var value = basePool[k];  
            basePool[k] = basePool[n];  
            basePool[n] = value;  
        }

        if (basePool.Count > 10) basePool = basePool.GetRange(0, 10);

        questionPool.Clear();
        foreach (var v in basePool)
        {
            questionPool.Add(new Dictionary<string, object> { { "vocab", v }, { "type", "kana_romaji" } });
            questionPool.Add(new Dictionary<string, object> { { "vocab", v }, { "type", "meaning" } });
        }

        // Shuffle questions
        n = questionPool.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            var value = questionPool[k];  
            questionPool[k] = questionPool[n];  
            questionPool[n] = value;  
        }

        passingScore = (int)(questionPool.Count * 0.8f);
        progressLabel.gameObject.SetActive(true);
        NextQuestion();
    }

    void NextQuestion()
    {
        if (currentQIdx >= questionPool.Count)
        {
            ShowSummary();
            return;
        }

        state = "PLAYING";
        progressLabel.text = $"Question {currentQIdx + 1} / {questionPool.Count}";

        var q = questionPool[currentQIdx];
        CurriculumItem vocab = (CurriculumItem)q["vocab"];
        string qType = q["type"].ToString();

        string correctAns = "";
        List<string> pool = new List<string>();

        if (qType == "kana_romaji")
        {
            correctAns = vocab.romaji;
            foreach (var v in vocabData) if (v.romaji != correctAns && !string.IsNullOrEmpty(v.romaji)) pool.Add(v.romaji);
            wordLabel.text = vocab.kana;
            promptLabel.text = "Choose the reading:";
        }
        else if (qType == "meaning")
        {
            correctAns = vocab.meaning;
            foreach (var v in vocabData) if (v.meaning != correctAns && !string.IsNullOrEmpty(v.meaning)) pool.Add(v.meaning);
            wordLabel.text = vocab.kana;
            promptLabel.text = "What is the meaning?";
        }

        System.Random r = new System.Random();
        pool = new List<string>(pool.OrderBy(a => r.Next()));
        List<string> options = pool.Take(3).ToList();
        options.Add(correctAns);
        options = options.OrderBy(a => r.Next()).ToList();

        currentQ["correct"] = correctAns;
        currentQ["options"] = options;

        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        selectedOption = -1;

        for (int i = 0; i < options.Count; i++)
        {
            int captureIndex = i;
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            Button btn = btnObj.GetComponent<Button>();
            btn.GetComponentInChildren<TextMeshProUGUI>().text = options[i];
            btn.onClick.AddListener(() => OnOptionPressed(captureIndex));
        }

        wordLabel.gameObject.SetActive(true);
        promptLabel.gameObject.SetActive(true);
        optionsContainer.gameObject.SetActive(true);

        btnSubmit.GetComponentInChildren<TextMeshProUGUI>().text = "Confirm";
        btnSubmit.interactable = false;
        btnSubmit.gameObject.SetActive(true);

        feedbackLabel.gameObject.SetActive(false);
    }

    void OnOptionPressed(int idx)
    {
        if (state != "PLAYING") return;
        selectedOption = idx;
        btnSubmit.interactable = true;

        int i = 0;
        foreach (Transform child in optionsContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.color = (i == idx) ? new Color(0.5f, 0.8f, 1.0f) : Color.white;
            i++;
        }
    }

    void OnSubmitPressed()
    {
        if (state == "PLAYING")
        {
            var optionsList = (List<string>)currentQ["options"];
            bool isCorrect = optionsList[selectedOption] == (string)currentQ["correct"];

            if (isCorrect)
            {
                score += 1;
                feedbackLabel.text = "Correct!";
                feedbackLabel.color = new Color(0.4f, 1.0f, 0.4f);
                Global.Coins += 10;
            }
            else
            {
                feedbackLabel.text = "Wrong! Answer: " + currentQ["correct"];
                feedbackLabel.color = new Color(1.0f, 0.4f, 0.4f);
            }

            state = "RESULT";
            wordLabel.gameObject.SetActive(false);
            promptLabel.gameObject.SetActive(false);
            optionsContainer.gameObject.SetActive(false);

            btnSubmit.GetComponentInChildren<TextMeshProUGUI>().text = "Next";
            feedbackLabel.gameObject.SetActive(true);
        }
        else if (state == "RESULT")
        {
            currentQIdx += 1;
            NextQuestion();
        }
        else if (state == "SUMMARY")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }
    }

    void ShowSummary()
    {
        state = "SUMMARY";
        wordLabel.gameObject.SetActive(false);
        promptLabel.gameObject.SetActive(false);
        optionsContainer.gameObject.SetActive(false);
        progressLabel.gameObject.SetActive(false);
        btnBack.gameObject.SetActive(false);

        feedbackLabel.gameObject.SetActive(true);

        bool passed = score >= passingScore;
        string resultStr = passed ? "LEVEL PASSED!" : "LEVEL FAILED";
        feedbackLabel.text = $"{resultStr}\nScore: {score}/{questionPool.Count}\nRequired: {passingScore}";
        feedbackLabel.color = passed ? new Color(0.4f, 1.0f, 0.4f) : new Color(1.0f, 0.4f, 0.4f);

        if (passed && Global.CurrentLevel == Global.SoloTargetLevel) {
            Global.CurrentLevel += 1;
            // You might want to sync this to server here
        }

        btnSubmit.GetComponentInChildren<TextMeshProUGUI>().text = "Main Menu";
    }
}
