using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class QuantumTestManager : MonoBehaviour
{
    private List<CurriculumItem> vocabData = new List<CurriculumItem>();
    private List<Dictionary<string, object>> questionPool = new List<Dictionary<string, object>>();
    private int currentQIdx = 0;
    private int score = 0;
    private int passingScore = 40;

    private string state = "PLAYING";
    private Dictionary<string, object> currentQ = new Dictionary<string, object>();
    private int selectedOption = -1;
    private float questionStartTime = 0;
    private List<Dictionary<string, string>> weakWords = new List<Dictionary<string, string>>();
    private List<float> timesTaken = new List<float>();

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

        int mode = Global.QuantumMode != 0 ? Global.QuantumMode : 1;
        
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        btnSubmit.onClick.AddListener(OnSubmitPressed);

        wordLabel.text = "Loading Quantum data...";
        wordLabel.gameObject.SetActive(true);
        promptLabel.gameObject.SetActive(false);
        optionsContainer.gameObject.SetActive(false);
        btnSubmit.gameObject.SetActive(false);
        progressLabel.gameObject.SetActive(false);

        LoadQuantumVocab(mode);
    }

    void LoadQuantumVocab(int mode)
    {
        vocabData.Clear();
        int currentLvl = Global.CurrentLevel;

        if (mode == 1)
        {
            vocabData.AddRange(Curriculum.GetLevel(currentLvl));
        }
        else
        {
            for (int i = 1; i <= currentLvl; i++)
            {
                vocabData.AddRange(Curriculum.GetLevel(i));
            }
        }

        SetupQuestionPool(mode);
    }

    void SetupQuestionPool(int mode)
    {
        questionPool.Clear();
        if (mode == 1)
        {
            foreach (var word in vocabData)
            {
                questionPool.Add(new Dictionary<string, object> { { "word", word }, { "type", "reading" } });
                if (!string.IsNullOrEmpty(word.meaning))
                    questionPool.Add(new Dictionary<string, object> { { "word", word }, { "type", "meaning" } });
                else
                    questionPool.Add(new Dictionary<string, object> { { "word", word }, { "type", "romaji_to_kana" } });
            }
        }
        else
        {
            // Shuffle and pick 50
            System.Random rng = new System.Random();
            var shuffled = vocabData.OrderBy(a => rng.Next()).Take(50).ToList();

            foreach (var word in shuffled)
            {
                if (!string.IsNullOrEmpty(word.meaning))
                    questionPool.Add(new Dictionary<string, object> { { "word", word }, { "type", rng.Next(2) == 0 ? "reading" : "meaning" } });
                else
                    questionPool.Add(new Dictionary<string, object> { { "word", word }, { "type", rng.Next(2) == 0 ? "reading" : "romaji_to_kana" } });
            }
        }

        // Shuffle pool
        System.Random r = new System.Random();
        questionPool = questionPool.OrderBy(a => r.Next()).ToList();
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
        questionStartTime = Time.time;

        var q = questionPool[currentQIdx];
        CurriculumItem vocab = (CurriculumItem)q["word"];
        string qType = q["type"].ToString();

        string correctAns = "";
        List<string> pool = new List<string>();

        if (qType == "reading")
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
        else
        {
            correctAns = vocab.kana;
            foreach (var v in vocabData) if (v.kana != correctAns && !string.IsNullOrEmpty(v.kana)) pool.Add(v.kana);
            wordLabel.text = vocab.romaji;
            promptLabel.text = "Choose the character:";
        }

        System.Random r = new System.Random();
        pool = pool.OrderBy(a => r.Next()).ToList();
        List<string> options = pool.Take(3).ToList();
        options.Add(correctAns);
        options = options.OrderBy(a => r.Next()).ToList();

        currentQ["correct"] = correctAns;
        currentQ["options"] = options;
        currentQ["word_jp"] = vocab.kana;

        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        selectedOption = -1;

        for (int i = 0; i < options.Count; i++)
        {
            int captureIndex = i; // Closure capture
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

        // Reset colors
        int i = 0;
        foreach (Transform child in optionsContainer)
        {
            Image img = child.GetComponent<Image>();
            img.color = (i == idx) ? new Color(0.5f, 0.8f, 1.0f) : Color.white;
            i++;
        }
    }

    void OnSubmitPressed()
    {
        if (state == "PLAYING")
        {
            var optionsList = (List<string>)currentQ["options"];
            bool isCorrect = optionsList[selectedOption] == (string)currentQ["correct"];
            float timeTaken = Time.time - questionStartTime;

            float avgTime = 3.0f; // Default 3s
            if (timesTaken.Count > 0)
                avgTime = timesTaken.Average();

            bool isWeak = false;
            string statusStr = "";

            if (!isCorrect)
            {
                isWeak = true;
                statusStr = "wrong";
            }
            else
            {
                float pKnow = 1.0f;
                if (timeTaken > avgTime)
                {
                    float deltaT = timeTaken - avgTime;
                    float sigma = 2.0f;
                    pKnow = Mathf.Exp(-Mathf.Pow(deltaT, 2) / (2.0f * Mathf.Pow(sigma, 2)));

                    float collapseVal = UnityEngine.Random.value;
                    if (collapseVal > pKnow)
                    {
                        isWeak = true;
                        statusStr = "guessing";
                    }
                    else
                    {
                        isWeak = true;
                        statusStr = "hesitant";
                    }
                }
            }

            if (isWeak)
            {
                string vocabWord = (string)currentQ["word_jp"];
                var wDict = weakWords.FirstOrDefault(w => w["word"] == vocabWord);
                if (wDict != null)
                {
                    string currStatus = wDict["status"];
                    if (statusStr == "wrong") wDict["status"] = "wrong";
                    else if (statusStr == "guessing" && currStatus == "hesitant") wDict["status"] = "guessing";
                }
                else
                {
                    weakWords.Add(new Dictionary<string, string> { { "word", vocabWord }, { "status", statusStr } });
                }
            }

            if (isCorrect)
            {
                timesTaken.Add(timeTaken);
                score += 1;
                feedbackLabel.text = "Correct!";
                feedbackLabel.color = new Color(0.4f, 1.0f, 0.4f);
                Global.Coins += 5;
            }
            else
            {
                feedbackLabel.text = "Wrong! Answer: " + currentQ["correct"];
                feedbackLabel.color = new Color(1.0f, 0.4f, 0.4f);
                questionPool.Add(questionPool[currentQIdx]); // Penalty loop
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

        string summaryText = "";
        if (weakWords.Count == 0)
        {
            summaryText = $"PERFECT!\nYou scored 100% with no hesitation!\nScore: {score}/{questionPool.Count}\n(Bonus 500 G-Coins)";
            feedbackLabel.color = new Color(0.8f, 0.4f, 1.0f);
            Global.Coins += 500;
        }
        else
        {
            summaryText = $"QUANTUM ANALYSIS\nScore: {score}/{questionPool.Count}. Wrong or hesitant words: {weakWords.Count}\nWords to review: ";
            
            List<string> displayWords = new List<string>();
            foreach (var w in weakWords)
            {
                string text = w["word"];
                if (w["status"] == "guessing") text += " (Guessing)";
                else if (w["status"] == "hesitant") text += " (Hesitant)";
                else if (w["status"] == "wrong") text += " (Wrong)";
                displayWords.Add(text);
            }
            summaryText += string.Join(", ", displayWords);

            feedbackLabel.color = new Color(1.0f, 0.7f, 0.2f);
            Global.Coins += 100;
        }

        feedbackLabel.text = summaryText;
        btnSubmit.GetComponentInChildren<TextMeshProUGUI>().text = "Main Menu";
    }
}
