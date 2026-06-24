using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;

public class SenseiTestManager : MonoBehaviour
{
    [Header("Dialogs & Panels")]
    public GameObject listPanel;
    public GameObject editDialog;
    public GameObject historyDialog;

    [Header("List Panel UI")]
    public TMP_InputField searchInput;
    public Transform listContainer;
    public GameObject testItemPrefab;
    public TextMeshProUGUI statusLabel;
    public TextMeshProUGUI lblPage;
    public Button btnPrev;
    public Button btnNext;
    public Button btnBack;
    public Button btnCreate;

    [Header("Edit Dialog UI")]
    public TMP_InputField titleInput;
    public TMP_InputField codeInput;
    public TMP_InputField levelInput;
    public TMP_InputField coinInput;
    public TMP_InputField attemptsInput;
    public Transform questionsContainer;
    public GameObject questionRowPrefab;
    public Button btnSaveTest;
    public Button btnCloseEdit;
    public Button btnAddQuestion;

    [Header("History Dialog UI")]
    public Transform historyContainer;
    public GameObject historyItemPrefab;
    public Button btnCloseHistory;

    private List<Dictionary<string, object>> allTests = new List<Dictionary<string, object>>();
    private int currentPage = 0;
    private int itemsPerPage = 5;
    private long currentEditId = -1;

    void Start()
    {
        if (btnBack != null) btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        if (btnCreate != null) btnCreate.onClick.AddListener(ShowCreateDialog);
        
        if (btnPrev != null) btnPrev.onClick.AddListener(() => ChangePage(-1));
        if (btnNext != null) btnNext.onClick.AddListener(() => ChangePage(1));
        
        if (searchInput != null) searchInput.onValueChanged.AddListener((val) => { currentPage = 0; UpdateListUI(); });

        if (btnCloseEdit != null) btnCloseEdit.onClick.AddListener(() => editDialog.SetActive(false));
        if (btnSaveTest != null) btnSaveTest.onClick.AddListener(SaveTest);
        if (btnAddQuestion != null) btnAddQuestion.onClick.AddListener(() => AddQuestionRow(null));

        if (btnCloseHistory != null) btnCloseHistory.onClick.AddListener(() => historyDialog.SetActive(false));

        editDialog.SetActive(false);
        historyDialog.SetActive(false);

        FetchTests();
    }

    void FetchTests()
    {
        StartCoroutine(FetchTestsCoroutine());
    }

    IEnumerator FetchTestsCoroutine()
    {
        statusLabel.text = "Loading data...";
        long userId = Global.UserId;
        
        string url = NetworkManager.Instance.BASE_URL + $"/api/sensei/{userId}/custom-tests";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
                req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                statusLabel.text = "";
                allTests = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(req.downloadHandler.text);
                if (allTests == null) allTests = new List<Dictionary<string, object>>();
                
                allTests.Sort((a, b) => System.Convert.ToInt64(b["id"]).CompareTo(System.Convert.ToInt64(a["id"])));
                currentPage = 0;
                UpdateListUI();
            }
            else
            {
                statusLabel.text = "Error loading data: " + req.responseCode;
            }
        }
    }

    void UpdateListUI()
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);
        
        List<Dictionary<string, object>> filtered = new List<Dictionary<string, object>>();
        string q = searchInput.text.Trim().ToLower();
        
        if (string.IsNullOrEmpty(q)) filtered = allTests;
        else
        {
            foreach (var test in allTests)
            {
                string code = test.ContainsKey("testCode") ? test["testCode"].ToString().ToLower() : "";
                string title = test.ContainsKey("title") ? test["title"].ToString().ToLower() : "";
                if (code.Contains(q) || title.Contains(q)) filtered.Add(test);
            }
        }
        
        if (filtered.Count == 0)
        {
            statusLabel.text = "No matching tests found.";
            btnPrev.interactable = false;
            btnNext.interactable = false;
            lblPage.text = "0 / 0";
            return;
        }
        
        statusLabel.text = "";
        int totalPages = Mathf.CeilToInt((float)filtered.Count / itemsPerPage);
        if (currentPage >= totalPages) currentPage = totalPages - 1;
        if (currentPage < 0) currentPage = 0;
        
        lblPage.text = $"{currentPage + 1} / {totalPages}";
        btnPrev.interactable = (currentPage > 0);
        btnNext.interactable = (currentPage < totalPages - 1);
        
        int startIdx = currentPage * itemsPerPage;
        int endIdx = Mathf.Min(startIdx + itemsPerPage, filtered.Count);
        
        for (int i = startIdx; i < endIdx; i++)
        {
            AddTestItem(filtered[i]);
        }
    }

    void AddTestItem(Dictionary<string, object> item)
    {
        GameObject panel = Instantiate(testItemPrefab, listContainer);
        panel.SetActive(true);
        
        string code = item.ContainsKey("testCode") ? item["testCode"].ToString() : "N/A";
        string title = item.ContainsKey("title") ? item["title"].ToString() : "";
        string minLv = item.ContainsKey("minLevel") ? item["minLevel"].ToString() : "1";
        string reward = item.ContainsKey("rewardGcoin") ? item["rewardGcoin"].ToString() : "0";
        string maxAtt = item.ContainsKey("maxAttempts") ? item["maxAttempts"].ToString() : "0";
        
        string infoTxt = $"Code: {code} | {title} (Min Lv: {minLv})\nReward: {reward} G-Coin | Max: {maxAtt} attempts";
        
        // Assume prefab has a child TextMeshProUGUI named "InfoText"
        TextMeshProUGUI infoLabel = panel.transform.Find("InfoText").GetComponent<TextMeshProUGUI>();
        infoLabel.text = infoTxt;
        
        // Assume prefab has buttons "BtnHistory", "BtnEdit", "BtnDelete"
        Button btnHist = panel.transform.Find("BtnHistory").GetComponent<Button>();
        btnHist.onClick.AddListener(() => ShowHistory(System.Convert.ToInt64(item["id"])));
        
        Button btnEdit = panel.transform.Find("BtnEdit").GetComponent<Button>();
        btnEdit.onClick.AddListener(() => EditTest(item));
        
        Button btnDel = panel.transform.Find("BtnDelete").GetComponent<Button>();
        btnDel.onClick.AddListener(() => DeleteTest(System.Convert.ToInt64(item["id"])));
    }

    void ChangePage(int dir)
    {
        currentPage += dir;
        UpdateListUI();
    }

    void ShowCreateDialog()
    {
        currentEditId = -1;
        titleInput.text = "";
        codeInput.text = "";
        levelInput.text = "1";
        coinInput.text = "100";
        attemptsInput.text = "1";
        
        foreach(Transform child in questionsContainer) Destroy(child.gameObject);
        AddQuestionRow(null);
        editDialog.SetActive(true);
    }

    void EditTest(Dictionary<string, object> item)
    {
        currentEditId = System.Convert.ToInt64(item["id"]);
        titleInput.text = item.ContainsKey("title") ? item["title"].ToString() : "";
        codeInput.text = item.ContainsKey("testCode") ? item["testCode"].ToString() : "";
        levelInput.text = item.ContainsKey("minLevel") ? item["minLevel"].ToString() : "1";
        coinInput.text = item.ContainsKey("rewardGcoin") ? item["rewardGcoin"].ToString() : "100";
        attemptsInput.text = item.ContainsKey("maxAttempts") ? item["maxAttempts"].ToString() : "1";
        
        foreach(Transform child in questionsContainer) Destroy(child.gameObject);
        
        if (item.ContainsKey("questions"))
        {
            string qJson = JsonConvert.SerializeObject(item["questions"]);
            List<Dictionary<string, object>> qs = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(qJson);
            if (qs == null || qs.Count == 0) AddQuestionRow(null);
            else foreach (var q in qs) AddQuestionRow(q);
        }
        else AddQuestionRow(null);
        
        editDialog.SetActive(true);
    }

    void AddQuestionRow(Dictionary<string, object> data)
    {
        GameObject panel = Instantiate(questionRowPrefab, questionsContainer);
        panel.SetActive(true);
        SenseiQuestionRef qr = panel.GetComponent<SenseiQuestionRef>();

        qr.qInput.text = data != null && data.ContainsKey("questionText") ? data["questionText"].ToString() : "";
        qr.aInput.text = data != null && data.ContainsKey("answerA") ? data["answerA"].ToString() : "";
        qr.bInput.text = data != null && data.ContainsKey("answerB") ? data["answerB"].ToString() : "";
        qr.cInput.text = data != null && data.ContainsKey("answerC") ? data["answerC"].ToString() : "";
        qr.dInput.text = data != null && data.ContainsKey("answerD") ? data["answerD"].ToString() : "";
        qr.correctInput.text = data != null && data.ContainsKey("correctAnswer") ? data["correctAnswer"].ToString() : "A";

        qr.btnDelete.onClick.AddListener(() => Destroy(panel));
    }

    void SaveTest()
    {
        if (string.IsNullOrEmpty(titleInput.text) || string.IsNullOrEmpty(codeInput.text)) return;
        StartCoroutine(SaveTestCoroutine());
    }

    IEnumerator SaveTestCoroutine()
    {
        List<Dictionary<string, string>> qData = new List<Dictionary<string, string>>();
        foreach (Transform child in questionsContainer)
        {
            SenseiQuestionRef qr = child.GetComponent<SenseiQuestionRef>();
            if (qr != null && !string.IsNullOrEmpty(qr.qInput.text))
            {
                var qDict = new Dictionary<string, string>();
                qDict["questionText"] = qr.qInput.text;
                qDict["answerA"] = qr.aInput.text;
                qDict["answerB"] = qr.bInput.text;
                qDict["answerC"] = qr.cInput.text;
                qDict["answerD"] = qr.dInput.text;
                string corr = qr.correctInput.text.ToUpper();
                if (corr != "A" && corr != "B" && corr != "C" && corr != "D") corr = "A";
                qDict["correctAnswer"] = corr;
                qData.Add(qDict);
            }
        }

        var bodyDict = new Dictionary<string, object>();
        bodyDict["title"] = titleInput.text;
        bodyDict["testCode"] = codeInput.text;
        bodyDict["minLevel"] = int.Parse(levelInput.text);
        bodyDict["rewardGcoin"] = int.Parse(coinInput.text);
        bodyDict["maxAttempts"] = int.Parse(attemptsInput.text);
        bodyDict["questions"] = qData;

        string json = JsonConvert.SerializeObject(bodyDict);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        
        long userId = Global.UserId;
        string url = NetworkManager.Instance.BASE_URL + $"/api/sensei/{userId}/custom-tests";
        if (currentEditId != -1) url += $"/{currentEditId}";
        
        using (UnityWebRequest req = new UnityWebRequest(url, currentEditId == -1 ? "POST" : "PUT"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
                req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                editDialog.SetActive(false);
                FetchTests();
            }
            else
            {
                Debug.LogError("Error saving test: " + req.downloadHandler.text);
            }
        }
    }

    void DeleteTest(long id)
    {
        StartCoroutine(DeleteTestCoroutine(id));
    }

    IEnumerator DeleteTestCoroutine(long id)
    {
        long userId = Global.UserId;
        string url = NetworkManager.Instance.BASE_URL + $"/api/sensei/{userId}/custom-tests/{id}";
        using (UnityWebRequest req = UnityWebRequest.Delete(url))
        {
            if (!string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
                req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                
            yield return req.SendWebRequest();
            FetchTests();
        }
    }

    void ShowHistory(long id)
    {
        historyDialog.SetActive(true);
        foreach (Transform child in historyContainer) Destroy(child.gameObject);
        
        // Loading label could be a simple instantiated prefab
        StartCoroutine(FetchHistoryCoroutine(id));
    }

    IEnumerator FetchHistoryCoroutine(long id)
    {
        long userId = Global.UserId;
        string url = NetworkManager.Instance.BASE_URL + $"/api/sensei/{userId}/tests/{id}/attempts";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
                req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                
            yield return req.SendWebRequest();
            
            foreach (Transform child in historyContainer) Destroy(child.gameObject);
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                var attempts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(req.downloadHandler.text);
                if (attempts == null || attempts.Count == 0)
                {
                    // No attempts
                }
                else
                {
                    foreach (var attempt in attempts)
                    {
                        string pName = attempt.ContainsKey("playerName") ? attempt["playerName"].ToString() : "Unknown";
                        string score = attempt.ContainsKey("score") ? attempt["score"].ToString() : "0";
                        bool passed = attempt.ContainsKey("passed") && (bool)attempt["passed"];
                        string date = attempt.ContainsKey("attemptTime") ? attempt["attemptTime"].ToString().Substring(0, 10) : "";
                        string status = passed ? "<color=green>PASS</color>" : "<color=red>FAIL</color>";
                        
                        string txt = $"Player: {pName} | Correct: {score} | {status} | {date}";
                        
                        GameObject go = Instantiate(historyItemPrefab, historyContainer);
                        go.SetActive(true);
                        go.GetComponentInChildren<TextMeshProUGUI>().text = txt;
                    }
                }
            }
        }
    }
}

public class SenseiQuestionRef : MonoBehaviour
{
    public TMP_InputField qInput;
    public TMP_InputField aInput;
    public TMP_InputField bInput;
    public TMP_InputField cInput;
    public TMP_InputField dInput;
    public TMP_InputField correctInput;
    public Button btnDelete;
}
