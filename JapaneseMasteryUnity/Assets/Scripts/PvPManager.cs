using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;

public class PvPManager : MonoBehaviour
{
    private static PvPManager _instance;
    public static PvPManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<PvPManager>();
            return _instance;
        }
    }

    [Header("UI Panels")]
    public GameObject pvpCanvas;
    public GameObject lobbyPanel;
    public GameObject lobbyWaitPanel;
    public GameObject queuePanel;
    public GameObject matchPanel;
    public GameObject resultPanel;

    [Header("Lobby UI")]
    public TMP_InputField lobbyCodeInput;
    public TMP_Text lobbyCodeText;
    public Button btnRandomMatch;
    public Button btnCreateLobby;
    public Button btnJoinLobby;
    public Button btnBackToMain;
    public Button btnCancelLobby;

    [Header("Text Components")]
    public TMP_Text paragraphText;
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text resultText;
    public TMP_Text oppNameText;

    [Header("Progress")]
    public RectTransform myProgressFill;
    public RectTransform oppProgressFill;

    [Header("Buttons")]
    public Button btnCancel;
    public Button btnQuitMatch;
    public Button btnReturn;

    private bool isPolling = false;
    private bool inMatch = false;
    private string currentMatchId = "";
    private string currentParagraph = "";
    private string currentRomaji = "";
    private string currentInput = "";
    private string lastRenderedText = "";

    private float matchStartTime = 0f;
    private bool isTyping = false;
    private bool isCountdown = false;
    private float countdownEndTime = 0f;
    private float syncTimer = 0f;

    private string currentLobbyCode = "";
    private bool isLobbyHost = false;

    private TMP_InputField hiddenInput;

    private void Awake()
    {
        if (_instance == null || _instance == this) _instance = this;

        if (btnCancel != null) btnCancel.onClick.AddListener(() => { LeaveQueue(); Global.GotoScene("Main"); });
        if (btnQuitMatch != null) btnQuitMatch.onClick.AddListener(QuitMatch);
        if (btnReturn != null) btnReturn.onClick.AddListener(() => { inMatch = false; HideAllPanels(); Global.GotoScene("Main"); });

        // Lobby buttons
        if (btnRandomMatch != null) btnRandomMatch.onClick.AddListener(OnClickRandomMatch);
        if (btnCreateLobby != null) btnCreateLobby.onClick.AddListener(OnClickCreateLobby);
        if (btnJoinLobby != null) btnJoinLobby.onClick.AddListener(OnClickJoinLobby);
        if (btnBackToMain != null) btnBackToMain.onClick.AddListener(() => { HideAllPanels(); Global.GotoScene("Main"); });
        if (btnCancelLobby != null) btnCancelLobby.onClick.AddListener(OnClickCancelLobby);
    }

    private void Start()
    {
        HideAllPanels();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PvP")
        {
            ShowLobby();
        }
    }

    // ============ LOBBY ============

    private void ShowLobby()
    {
        HideAllPanels();
        if (pvpCanvas != null) pvpCanvas.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    private void OnClickRandomMatch()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        ShowPanel(queuePanel);
        StartCoroutine(JoinQueueCoroutine());
    }

    private void OnClickCreateLobby()
    {
        StartCoroutine(CreateLobbyCoroutine());
    }

    private void OnClickJoinLobby()
    {
        string code = lobbyCodeInput != null ? lobbyCodeInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Please enter a lobby code!");
            return;
        }
        StartCoroutine(JoinLobbyCoroutine(code));
    }

    private void OnClickCancelLobby()
    {
        isPolling = false;
        if (!string.IsNullOrEmpty(currentLobbyCode))
        {
            StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/lobby/cancel/{currentLobbyCode}/{Global.UserId}"));
        }
        currentLobbyCode = "";
        isLobbyHost = false;
        ShowLobby();
    }

    private IEnumerator CreateLobbyCoroutine()
    {
        string url = NetworkManager.Instance.BASE_URL + $"/api/pvp/lobby/create/{Global.UserId}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                currentLobbyCode = data.ContainsKey("lobbyCode") ? data["lobbyCode"].ToString() : "ERROR";
                isLobbyHost = true;

                // Show lobby wait panel
                HideAllPanels();
                if (pvpCanvas != null) pvpCanvas.SetActive(true);
                if (lobbyWaitPanel != null) lobbyWaitPanel.SetActive(true);
                if (lobbyCodeText != null) lobbyCodeText.text = "Code: " + currentLobbyCode;

                // Start polling for opponent
                isPolling = true;
                StartCoroutine(PollStatusCoroutine());
            }
            else
            {
                Debug.LogError("Failed to create lobby: " + req.downloadHandler.text);
            }
        }
    }

    private IEnumerator JoinLobbyCoroutine(string code)
    {
        string url = NetworkManager.Instance.BASE_URL + $"/api/pvp/lobby/join/{code}/{Global.UserId}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                currentLobbyCode = code;
                isLobbyHost = false;

                // Show queue panel while waiting for match to start
                HideAllPanels();
                if (pvpCanvas != null) pvpCanvas.SetActive(true);
                if (queuePanel != null) queuePanel.SetActive(true);

                isPolling = true;
                StartCoroutine(PollStatusCoroutine());
            }
            else
            {
                Debug.LogWarning("Failed to join lobby: " + req.downloadHandler.text);
                // Show error feedback - stay on lobby panel
            }
        }
    }

    // ============ ORIGINAL PVP LOGIC ============

    public void OnClickPvPButton()
    {
        ShowLobby();
    }

    private IEnumerator JoinQueueCoroutine()
    {
        string url = NetworkManager.Instance.BASE_URL + $"/api/pvp/queue/join/{Global.UserId}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ShowPanel(queuePanel);
                isPolling = true;
                StartCoroutine(PollStatusCoroutine());
            }
            else
            {
                if (req.downloadHandler.text.Contains("BANNED"))
                {
                    Debug.LogWarning("Banned from PvP.");
                    Global.GotoScene("Main");
                }
            }
        }
    }

    public void LeaveQueue()
    {
        isPolling = false;
        StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/queue/leave/{Global.UserId}"));
        HideAllPanels();
    }

    public void QuitMatch()
    {
        isPolling = false;
        inMatch = false;
        StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/match/quit/{Global.UserId}"));
        HideAllPanels();
        Global.GotoScene("Main");
    }

    private IEnumerator SimplePost(string url)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return req.SendWebRequest();
        }
    }

    private IEnumerator PollStatusCoroutine()
    {
        while (isPolling)
        {
            string url = NetworkManager.Instance.BASE_URL + $"/api/pvp/status/{Global.UserId}";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                    string status = data.ContainsKey("status") ? data["status"].ToString() : "NONE";

                    if (status == "MATCHED") HandleMatchState(data);
                    else if (status == "NONE" && inMatch)
                    {
                        inMatch = false;
                        isPolling = false;
                        HideAllPanels();
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void HandleMatchState(Dictionary<string, object> data)
    {
        currentMatchId = data.ContainsKey("matchId") ? data["matchId"]?.ToString() : "";
        currentParagraph = data.ContainsKey("paragraph") ? data["paragraph"]?.ToString() : "No Text";
        
        string romaji = data.ContainsKey("romajiParagraph") ? data["romajiParagraph"]?.ToString() : null;
        currentRomaji = !string.IsNullOrEmpty(romaji) ? romaji.ToLower() : currentParagraph.ToLower();
        
        string state = data.ContainsKey("state") ? data["state"]?.ToString() : "";
        
        if (oppNameText != null && data.ContainsKey("opponentName"))
        {
            string oppName = data["opponentName"]?.ToString();
            Debug.Log($"Received Opponent Name: {oppName}");
            oppNameText.text = oppName;
        }

        if (!inMatch)
        {
            inMatch = true;
            ShowPanel(matchPanel);
            if (statusText != null) statusText.gameObject.SetActive(true);
            currentInput = "";
            lastRenderedText = "";
            if (hiddenInput != null) hiddenInput.text = "";

            // Ensure rich text is enabled
            if (paragraphText != null)
            {
                paragraphText.richText = true;
                paragraphText.enableAutoSizing = false;
                paragraphText.fontSize = 32; 
                
                // Clear dynamic font data to avoid IndexOutOfRangeException when atlas gets full
                if (paragraphText.font != null)
                {
                    paragraphText.font.ClearFontAssetData(false);
                }
// REMOVED: paragraphText.material = paragraphText.font.material;
                // This manual assignment can cause IndexOutOfRangeException in TMP 3.0.9
            }

            Debug.Log($"PvP Match Started: {currentMatchId}. Paragraph length: {currentParagraph.Length}");
            UpdateRichText();
            SetProgress(0, 0);
        }

        if (state == "COUNTDOWN")
        {
            isTyping = false;
            long countdownMs = 3000;
            if (data.ContainsKey("countdown")) long.TryParse(data["countdown"].ToString(), out countdownMs);
            countdownEndTime = Time.time + (countdownMs / 1000f);
            isCountdown = true;
            UpdateRichText();
        }
        else if (state == "PLAYING")
        {
            if (isCountdown || !isTyping)
            {
                isCountdown = false;
                isTyping = true;
                matchStartTime = Time.time;
                UpdateRichText();
            }
            
            float myProg = 0, oppProg = 0;
            if (data.ContainsKey("myProgress")) float.TryParse(data["myProgress"].ToString(), out myProg);
            if (data.ContainsKey("oppProgress")) float.TryParse(data["oppProgress"].ToString(), out oppProg);
            SetProgress(myProg, oppProg);
        }
        else if (state == "FINISHED")
        {
            isTyping = false;
            isCountdown = false;
            long winnerId = -1;
            if (data.ContainsKey("winnerId")) long.TryParse(data["winnerId"].ToString(), out winnerId);
            int reward = 0;
            if (data.ContainsKey("reward")) int.TryParse(data["reward"].ToString(), out reward);
            ShowResultScreen(winnerId, reward);
        }
    }

    private void Update()
    {
        if (isCountdown)
        {
            float remain = countdownEndTime - Time.time;
            if (statusText != null) statusText.text = remain > 0 ? $"Starting in: {Mathf.CeilToInt(remain)}" : "GO!";
            if (timerText != null) timerText.text = "0.00 s";
        }
        else if (isTyping)
        {
            if (statusText != null) statusText.text = "TYPE!";
            if (timerText != null) timerText.text = (Time.time - matchStartTime).ToString("F2") + " s";
            
            HandleTypingInput();
            
            syncTimer += Time.deltaTime;
            if (syncTimer > 1f) {
                syncTimer = 0f;
                StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/match/progress/{Global.UserId}?progress={currentInput.Length}"));
            }
        }
    }

    private void HandleTypingInput()
    {
        if (hiddenInput == null) CreateHiddenInput();
        
        // Optimize: Only activate if not focused and user is not clicking elsewhere
        if (hiddenInput != null && !hiddenInput.isFocused && !Input.GetMouseButton(0))
        {
            hiddenInput.ActivateInputField();
        }
    }

    private void CreateHiddenInput()
    {
        var go = new GameObject("HiddenInput");
        go.transform.SetParent(pvpCanvas.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
        rect.anchoredPosition = new Vector2(-10000, -10000);

        hiddenInput = go.AddComponent<TMP_InputField>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.color = new Color(0,0,0,0);
        if (paragraphText != null) txt.font = paragraphText.font;

        hiddenInput.textComponent = txt;
        hiddenInput.textViewport = rect;
        hiddenInput.onValueChanged.AddListener((val) => {
            currentInput = val.ToLower();
            UpdateRichText();
            CheckCompletion();
        });
    }

    private void UpdateRichText()
    {
        if (paragraphText == null) return;
        
        StringBuilder sb = new StringBuilder();
        // FIX: No color tag on main paragraph to reduce tag count
        sb.Append(EscapeTMP(currentParagraph)).Append("\n\n");

        if (string.IsNullOrEmpty(currentRomaji))
        {
            SetText(sb.ToString());
            return;
        }

        bool hasError = false;
        int lastState = -1;
        StringBuilder chunk = new StringBuilder();

        for (int i = 0; i < currentRomaji.Length; i++)
        {
            int state;
            if (i < currentInput.Length)
            {
                if (currentInput[i] == currentRomaji[i]) state = 0;
                else { state = 1; hasError = true; }
            }
            else if (i == currentInput.Length && !hasError) state = 2;
            else state = 3;

            if (state != lastState)
            {
                if (lastState != -1) AppendTo(sb, lastState, chunk.ToString());
                chunk.Clear();
                lastState = state;
            }
            chunk.Append(currentRomaji[i]);
        }
        if (chunk.Length > 0) AppendTo(sb, lastState, chunk.ToString());

        SetText(sb.ToString());
    }

    private void AppendTo(StringBuilder sb, int state, string text)
    {
        string safe = EscapeTMP(text);
        string color = "white"; // default white
        if (state == 0) color = "#00FF00";
        else if (state == 1) color = "#FF0000";
        else if (state == 2) color = "#FFFF00";

        sb.Append("<color=").Append(color).Append(">").Append(safe).Append("</color>");
    }

    private string EscapeTMP(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Basic cleaning to remove potential problematic characters
        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c == '<') sb.Append("\uFF1C");
            else if (c == '>') sb.Append("\uFF1E");
            else if (c == '\r') continue;
            else if (c == '\n') sb.Append(" ");
            else if (char.IsControl(c)) continue; // Strip control characters
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private void SetText(string t)
    {
        if (lastRenderedText == t) return;
        lastRenderedText = t;
        paragraphText.text = t;
    }

    private void CheckCompletion()
    {
        if (currentInput == currentRomaji && currentRomaji.Length > 0)
        {
            isTyping = false;
            long timeMs = (long)((Time.time - matchStartTime) * 1000);
            StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/match/finish/{Global.UserId}?timeMs={timeMs}"));
        }
    }

    private void SetProgress(float my, float opp)
    {
        if (currentRomaji.Length == 0) return;
        if (myProgressFill != null) myProgressFill.anchorMax = new Vector2(Mathf.Clamp01(my / currentRomaji.Length), 1);
        if (oppProgressFill != null) oppProgressFill.anchorMax = new Vector2(Mathf.Clamp01(opp / currentRomaji.Length), 1);
    }

    private void ShowResultScreen(long winnerId, int reward)
    {
        isPolling = false;
        inMatch = false;
        HideAllPanels();
        if (pvpCanvas != null) pvpCanvas.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            if (winnerId == Global.UserId)
            {
                resultText.text = $"You Won!\n\nReward: +{reward} g-coin";
                Global.Coins += reward;
            }
            else resultText.text = $"You Lost!\n\nPenalty: -100 g-coin";
        }
        StartCoroutine(SimplePost(NetworkManager.Instance.BASE_URL + $"/api/pvp/match/ack/{Global.UserId}"));
    }

    private void HideAllPanels()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (lobbyWaitPanel != null) lobbyWaitPanel.SetActive(false);
        if (queuePanel != null) queuePanel.SetActive(false);
        if (matchPanel != null) matchPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (pvpCanvas != null) pvpCanvas.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        HideAllPanels();
        if (pvpCanvas != null) pvpCanvas.SetActive(true);
        if (panelToShow != null) panelToShow.SetActive(true);
    }
}
