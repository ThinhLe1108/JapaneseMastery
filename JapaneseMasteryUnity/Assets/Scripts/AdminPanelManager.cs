using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class UserDto
{
    public long id;
    public string username;
    public string displayName;
    public string role;
    public int level;
    public int gCoin;
}

[Serializable]
public class UserListDto
{
    public List<UserDto> users;
}

public class AdminPanelManager : MonoBehaviour
{
    public Button btnBack;
    public Button btnRefreshUsers;
    public TextMeshProUGUI statusText;
    public Transform userListContent;
    
    public Button btnUserTab;
    public Button btnVocabTab;
    public GameObject userPanel;
    public GameObject vocabPanel;
    
    public TMP_InputField userSearchInput;
    public Button btnUserPrev;
    public Button btnUserNext;
    public TextMeshProUGUI txtUserPage;

    public TMP_InputField vocabLevelInput;
    public Button btnLoadVocab;
    public TMP_InputField vocabSearchInput;
    public Button btnVocabPrev;
    public Button btnVocabNext;
    public TextMeshProUGUI txtVocabPage;
    public Transform vocabListContent;

    private GameObject userRowTemplate;
    private GameObject vocabRowTemplate;

    public TMPro.TMP_FontAsset jpFont;

    // Pagination State
    private List<UserDto> allUsers = new List<UserDto>();
    private List<UserDto> filteredUsers = new List<UserDto>();
    private int currentUserPage = 0;
    private int itemsPerPage = 6;

    private List<VocabDto> allVocabs = new List<VocabDto>();
    private List<VocabDto> filteredVocabs = new List<VocabDto>();
    private int currentVocabPage = 0;

    void Start()
    {
        if (jpFont == null)
        {
#if UNITY_EDITOR
            jpFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/NotoSansCJKjp-Regular SDF.asset");
#endif
        }

        if (jpFont != null)
        {
            foreach (var txt in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                txt.font = jpFont;
            }
        }

        // --- DYNAMIC UI FIXES ---
        GameObject topBar = GameObject.Find("TopBar");
        if (topBar != null)
        {
            RectTransform topBarRt = topBar.GetComponent<RectTransform>();
            if (topBarRt != null)
            {
                topBarRt.anchorMin = new Vector2(0.5f, 1f);
                topBarRt.anchorMax = new Vector2(0.5f, 1f);
                topBarRt.anchoredPosition = new Vector2(0, -90);
            }
        }

        GameObject btnBackObj = GameObject.Find("BtnBack");
        if (btnBackObj != null)
        {
            RectTransform btnBackRt = btnBackObj.GetComponent<RectTransform>();
            if (btnBackRt != null)
            {
                btnBackRt.anchorMin = new Vector2(0f, 1f);
                btnBackRt.anchorMax = new Vector2(0f, 1f);
                btnBackRt.anchoredPosition = new Vector2(100, -60);
            }
        }

        if (statusText != null) statusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 295);
        
        if (vocabLevelInput != null) { vocabLevelInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180, 200); vocabLevelInput.transform.SetAsLastSibling(); }
        if (btnLoadVocab != null) { btnLoadVocab.GetComponent<RectTransform>().anchoredPosition = new Vector2(90, 200); btnLoadVocab.transform.SetAsLastSibling(); }
        if (vocabSearchInput != null) { vocabSearchInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140); vocabSearchInput.transform.SetAsLastSibling(); }
        if (vocabListContent != null) vocabListContent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);

        if (userSearchInput != null) { userSearchInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200); userSearchInput.transform.SetAsLastSibling(); }
        if (btnRefreshUsers != null) { btnRefreshUsers.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140); btnRefreshUsers.transform.SetAsLastSibling(); }
        if (userListContent != null) userListContent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);

        if (btnBack != null) btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        if (btnRefreshUsers != null) btnRefreshUsers.onClick.AddListener(FetchUsers);
        if (btnUserTab != null) btnUserTab.onClick.AddListener(() => SetTab("user"));
        if (btnVocabTab != null) btnVocabTab.onClick.AddListener(() => SetTab("vocab"));
        
        if (btnUserPrev != null) btnUserPrev.onClick.AddListener(() => ChangeUserPage(-1));
        if (btnUserNext != null) btnUserNext.onClick.AddListener(() => ChangeUserPage(1));
        if (userSearchInput != null) userSearchInput.onValueChanged.AddListener(OnUserSearchChanged);

        if (btnLoadVocab != null) btnLoadVocab.onClick.AddListener(() => FetchVocabularies());
        if (btnVocabPrev != null) btnVocabPrev.onClick.AddListener(() => ChangeVocabPage(-1));
        if (btnVocabNext != null) btnVocabNext.onClick.AddListener(() => ChangeVocabPage(1));
        if (vocabSearchInput != null) vocabSearchInput.onValueChanged.AddListener(OnVocabSearchChanged);
            
        // Find templates
        if (userListContent != null && userListContent.childCount > 0)
        {
            userRowTemplate = userListContent.GetChild(0).gameObject;
            userRowTemplate.SetActive(false);
            for (int i = 1; i < userListContent.childCount; i++) Destroy(userListContent.GetChild(i).gameObject);
        }
        
        if (vocabListContent != null && vocabListContent.childCount > 0)
        {
            vocabRowTemplate = vocabListContent.GetChild(0).gameObject;
            vocabRowTemplate.SetActive(false);
            for (int i = 1; i < vocabListContent.childCount; i++) Destroy(vocabListContent.GetChild(i).gameObject);
        }

        if (vocabListContent != null)
        {
            RectTransform contentRt = vocabListContent.GetComponent<RectTransform>();
            if (contentRt != null) {
                contentRt.anchorMin = new Vector2(0.5f, 0.5f);
                contentRt.anchorMax = new Vector2(0.5f, 0.5f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = new Vector2(0, 110);
            }
        }
        if (userListContent != null)
        {
            RectTransform contentRt = userListContent.GetComponent<RectTransform>();
            if (contentRt != null) {
                contentRt.anchorMin = new Vector2(0.5f, 0.5f);
                contentRt.anchorMax = new Vector2(0.5f, 0.5f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = new Vector2(0, 110);
            }
        }

        SetTab("user");
        FetchUsers();
    }

    private void SetTab(string tabName)
    {
        if (userPanel != null) userPanel.SetActive(tabName == "user");
        if (vocabPanel != null) vocabPanel.SetActive(tabName == "vocab");
        if (btnUserTab != null) btnUserTab.GetComponent<Image>().color = (tabName == "user") ? new Color(0.06f, 0.22f, 0.06f) : new Color(0.12f, 0.12f, 0.12f);
        if (btnVocabTab != null) btnVocabTab.GetComponent<Image>().color = (tabName == "vocab") ? new Color(0.06f, 0.22f, 0.06f) : new Color(0.12f, 0.12f, 0.12f);
        if (statusText != null) statusText.text = "";
    }

    // =================================== USERS ===================================
    public void FetchUsers()
    {
        if (statusText != null) statusText.text = "Loading user list...";
        StartCoroutine(ApiRequestCoroutine("/api/admin/users", "GET", null, (code, body) => {
            if (code == 200)
            {
                if (statusText != null) statusText.text = "Users loaded successfully!";
                UserListDto listDto = JsonUtility.FromJson<UserListDto>("{\"users\":" + body + "}");
                allUsers = new List<UserDto>(listDto.users);
                allUsers.Sort((a, b) => b.id.CompareTo(a.id));
                currentUserPage = 0;
                UpdateUsersUI();
            }
            else
            {
                if (statusText != null) statusText.text = "Error loading Users: " + code;
            }
        }));
    }

    private void OnUserSearchChanged(string text)
    {
        currentUserPage = 0;
        UpdateUsersUI();
    }

    private void ChangeUserPage(int dir)
    {
        currentUserPage += dir;
        UpdateUsersUI();
    }

    private void UpdateUsersUI()
    {
        for (int i = 1; i < userListContent.childCount; i++) Destroy(userListContent.GetChild(i).gameObject);

        string q = userSearchInput != null ? userSearchInput.text.Trim().ToLower() : "";
        filteredUsers = string.IsNullOrEmpty(q) ? allUsers : allUsers.FindAll(u => 
            (u.username != null && u.username.ToLower().Contains(q)) || 
            (u.role != null && u.role.ToLower().Contains(q))
        );

        int totalPages = Mathf.CeilToInt((float)filteredUsers.Count / itemsPerPage);
        if (totalPages == 0) totalPages = 1;
        if (currentUserPage >= totalPages) currentUserPage = totalPages - 1;
        if (currentUserPage < 0) currentUserPage = 0;

        if (txtUserPage != null) txtUserPage.text = $"{currentUserPage + 1} / {totalPages}";
        if (btnUserPrev != null) btnUserPrev.interactable = (currentUserPage > 0);
        if (btnUserNext != null) btnUserNext.interactable = (currentUserPage < totalPages - 1);

        int startIdx = currentUserPage * itemsPerPage;
        int endIdx = Mathf.Min(startIdx + itemsPerPage, filteredUsers.Count);

        float yOffset = 0f;
        for (int i = startIdx; i < endIdx; i++)
        {
            UserDto user = filteredUsers[i];
            GameObject newRow = Instantiate(userRowTemplate, userListContent);
            newRow.SetActive(true);
            
            RectTransform rt = newRow.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -yOffset);
            }
            else newRow.transform.localPosition = new Vector3(newRow.transform.localPosition.x, -yOffset, 0);
            yOffset += 45f;

            Transform labelTr = newRow.transform.Find("Label");
            if (labelTr != null) 
            {
                labelTr.GetComponent<TextMeshProUGUI>().text = $"{user.username} (Lv {user.level}.0)";
                labelTr.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 40);
            }
            
            Transform roleTr = newRow.transform.Find("RoleDD/Text");
            if (roleTr != null) roleTr.GetComponent<TextMeshProUGUI>().text = user.role + " v";

            Transform inputLvTr = newRow.transform.Find("InputLv");
            if (inputLvTr != null) inputLvTr.GetComponent<TMP_InputField>().text = user.level.ToString();

            Transform roleBtnTr = newRow.transform.Find("RoleDD");
            if (roleBtnTr != null)
            {
                Button rBtn = roleBtnTr.GetComponent<Button>();
                rBtn.onClick.RemoveAllListeners();
                rBtn.onClick.AddListener(() => {
                    // Destroy existing popup if any
                    Transform existing = userPanel.transform.Find("RolePopup");
                    if (existing != null) { Destroy(existing.gameObject); return; }
                    
                    // Create simple dropdown popup
                    GameObject popup = new GameObject("RolePopup");
                    popup.transform.SetParent(userPanel.transform, false);
                    RectTransform pRt = popup.AddComponent<RectTransform>();
                    pRt.pivot = new Vector2(0.5f, 1f);
                    pRt.sizeDelta = new Vector2(140, 175);
                    popup.transform.position = roleBtnTr.position;
                    pRt.anchoredPosition += new Vector2(0, -17.5f);
                    
                    Image pImg = popup.AddComponent<Image>();
                    pImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                    
                    string[] roles = { "PLAYER", "SENSEI", "MODERATOR", "DESIGNER", "ADMIN" };
                    for (int j = 0; j < roles.Length; j++)
                    {
                        string rName = roles[j];
                        GameObject item = new GameObject("Item_" + rName);
                        item.transform.SetParent(popup.transform, false);
                        RectTransform iRt = item.AddComponent<RectTransform>();
                        iRt.anchorMin = new Vector2(0, 1); iRt.anchorMax = new Vector2(0, 1);
                        iRt.pivot = new Vector2(0, 1);
                        iRt.anchoredPosition = new Vector2(0, -(j * 35));
                        iRt.sizeDelta = new Vector2(140, 35);
                        
                        Image iImg = item.AddComponent<Image>();
                        iImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                        Button iBtn = item.AddComponent<Button>();
                        
                        GameObject tObj = new GameObject("Text");
                        tObj.transform.SetParent(item.transform, false);
                        RectTransform tRt = tObj.AddComponent<RectTransform>();
                        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one; tRt.sizeDelta = Vector2.zero;
                        TextMeshProUGUI tmp = tObj.AddComponent<TextMeshProUGUI>();
                        tmp.text = rName; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
                        
                        // Add hover effect
                        ColorBlock cb = iBtn.colors;
                        cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                        cb.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                        iBtn.colors = cb;

                        iBtn.onClick.AddListener(() => {
                            if (roleTr != null) roleTr.GetComponent<TextMeshProUGUI>().text = rName + " v";
                            Destroy(popup);
                        });
                    }
                });
            }

            // Hook up buttons
            Transform btnRole = newRow.transform.Find("BtnChangeRole");
            if (btnRole != null) 
            {
                btnRole.GetComponent<Button>().onClick.RemoveAllListeners();
                btnRole.GetComponent<Button>().onClick.AddListener(() => {
                    string selectedRole = roleTr.GetComponent<TextMeshProUGUI>().text.Replace(" v", "");
                    ChangeUserRole(user.id, selectedRole);
                });
            }

            Transform btnLv = newRow.transform.Find("BtnChangeLv");
            if (btnLv != null && inputLvTr != null) 
            {
                btnLv.GetComponent<Button>().onClick.RemoveAllListeners();
                btnLv.GetComponent<Button>().onClick.AddListener(() => ChangeUserLevel(user.id, int.Parse(inputLvTr.GetComponent<TMP_InputField>().text)));
            }

            Transform btnDel = newRow.transform.Find("BtnDelete");
            if (btnDel != null) 
            {
                btnDel.GetComponent<Button>().onClick.RemoveAllListeners();
                btnDel.GetComponent<Button>().onClick.AddListener(() => DeleteUser(user.id));
            }
        }
        
        RectTransform contentRt = userListContent.GetComponent<RectTransform>();
        if (contentRt != null) contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, yOffset + 50);
    }

    private void ChangeUserRole(long id, string newRole)
    {
        if (statusText != null) statusText.text = "Changing role...";
        StartCoroutine(ApiRequestCoroutine($"/api/admin/users/{id}/role?role={newRole}", "PUT", null, (code, body) => {
            if (statusText != null) statusText.text = code == 200 ? "Role changed successfully!" : "Error changing role";
            FetchUsers();
        }));
    }

    private void ChangeUserLevel(long id, int newLevel)
    {
        if (statusText != null) statusText.text = "Changing level...";
        StartCoroutine(ApiRequestCoroutine($"/api/admin/users/{id}/level?level={newLevel}", "PUT", null, (code, body) => {
            if (statusText != null) statusText.text = code == 200 ? "Level saved successfully!" : "Error changing level";
            FetchUsers();
        }));
    }

    private void DeleteUser(long id)
    {
        if (statusText != null) statusText.text = "Deleting user...";
        StartCoroutine(ApiRequestCoroutine($"/api/admin/users/{id}", "DELETE", null, (code, body) => {
            if (statusText != null) statusText.text = code == 200 ? "User deleted successfully!" : "Error deleting user";
            FetchUsers();
        }));
    }

    // =================================== VOCAB ===================================
    public void FetchVocabularies()
    {
        int lvl = 1;
        if (vocabLevelInput != null && !string.IsNullOrEmpty(vocabLevelInput.text))
            int.TryParse(vocabLevelInput.text, out lvl);
        if (lvl <= 0) lvl = 1;

        if (statusText != null) statusText.text = $"Loading vocabulary list for Level {lvl}...";
        
        StartCoroutine(ApiRequestCoroutine($"/api/admin/vocabularies?level={lvl}", "GET", null, (code, body) => {
            if (code == 200)
            {
                if (statusText != null) statusText.text = "Vocabulary loaded successfully!";
                VocabListDto listDto = JsonUtility.FromJson<VocabListDto>("{\"vocabs\":" + body + "}");
                allVocabs = new List<VocabDto>(listDto.vocabs);
                allVocabs.Sort((a, b) => b.id.CompareTo(a.id));
                
                if (vocabSearchInput != null) vocabSearchInput.SetTextWithoutNotify("");
                filteredVocabs = new List<VocabDto>(allVocabs);
                
                currentVocabPage = 0;
                UpdateVocabUI();
            }
            else
            {
                if (statusText != null) statusText.text = "Error loading vocabulary: " + code;
            }
        }));
    }

    private Coroutine searchCoroutine;

    private void OnVocabSearchChanged(string text)
    {
        if (searchCoroutine != null) StopCoroutine(searchCoroutine);
        searchCoroutine = StartCoroutine(DebouncedSearch(text));
    }

    private IEnumerator DebouncedSearch(string text)
    {
        yield return new WaitForSeconds(0.5f); // Wait half a second
        
        string q = text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            // If empty, just show the locally loaded level's vocabs
            filteredVocabs = allVocabs;
            currentVocabPage = 0;
            UpdateVocabUI();
            yield break;
        }

        if (statusText != null) statusText.text = "Searching globally...";
        
        // Search backend
        StartCoroutine(ApiRequestCoroutine($"/api/admin/vocabularies?q={UnityWebRequest.EscapeURL(q)}", "GET", null, (code, body) => {
            if (code == 200)
            {
                if (statusText != null) statusText.text = "Search completed!";
                VocabListDto listDto = JsonUtility.FromJson<VocabListDto>("{\"vocabs\":" + body + "}");
                // Instead of replacing allVocabs (which holds the currently loaded level),
                // we just replace filteredVocabs and update the UI directly.
                filteredVocabs = new List<VocabDto>(listDto.vocabs);
                currentVocabPage = 0;
                UpdateVocabUI();
            }
            else
            {
                if (statusText != null) statusText.text = "Error searching: " + code;
            }
        }));
    }

    private void ChangeVocabPage(int dir)
    {
        currentVocabPage += dir;
        UpdateVocabUI();
    }

    private void UpdateVocabUI()
    {
        for (int i = 1; i < vocabListContent.childCount; i++) Destroy(vocabListContent.GetChild(i).gameObject);

        if (filteredVocabs == null) filteredVocabs = new List<VocabDto>();

        int totalPages = Mathf.CeilToInt((float)filteredVocabs.Count / itemsPerPage);
        if (totalPages == 0) totalPages = 1;
        if (currentVocabPage >= totalPages) currentVocabPage = totalPages - 1;
        if (currentVocabPage < 0) currentVocabPage = 0;

        if (txtVocabPage != null) txtVocabPage.text = $"{currentVocabPage + 1} / {totalPages}";
        if (btnVocabPrev != null) btnVocabPrev.interactable = (currentVocabPage > 0);
        if (btnVocabNext != null) btnVocabNext.interactable = (currentVocabPage < totalPages - 1);

        int startIdx = currentVocabPage * itemsPerPage;
        int endIdx = Mathf.Min(startIdx + itemsPerPage, filteredVocabs.Count);

        float yOffset = 0f;
        for (int i = startIdx; i < endIdx; i++)
        {
            VocabDto v = filteredVocabs[i];
            GameObject newRow = Instantiate(vocabRowTemplate, vocabListContent);
            newRow.SetActive(true);
            
            int lvlReq = v.levelRequired > 0 ? v.levelRequired : v.level;
            
            // If the word has Kanji (wordJp), display: Kanji (Kana)
            // If the word has NO Kanji, display: Kana (Romaji)
            string displayJp;
            if (!string.IsNullOrEmpty(v.wordJp)) {
                displayJp = $"{v.wordJp} ({v.kana})";
            } else {
                displayJp = $"{v.kana} ({v.romaji})";
            }

            float rowHeight = 45f;
            Transform labelTr = newRow.transform.Find("Label");
            if (labelTr != null) {
                TextMeshProUGUI tmp = labelTr.GetComponent<TextMeshProUGUI>();
                tmp.text = $"[Lv{lvlReq}] {displayJp} - {v.meaning}";
                if (jpFont != null) tmp.font = jpFont;
                
                // Allow word wrapping and calculate dynamic height
                tmp.enableWordWrapping = true;
                tmp.overflowMode = TextOverflowModes.Overflow;
                
                float textHeight = tmp.GetPreferredValues(tmp.text, 900f, 0f).y;
                rowHeight = Mathf.Max(45f, textHeight + 10f); // Minimum 45f, padding 10f
                
                RectTransform lblRt = tmp.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0.5f, 1f);
                lblRt.anchorMax = new Vector2(0.5f, 1f);
                lblRt.pivot = new Vector2(0.5f, 1f);
                lblRt.sizeDelta = new Vector2(900, rowHeight);
                lblRt.anchoredPosition = new Vector2(0, 0);
            }

            RectTransform rt = newRow.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -yOffset);
            }
            else newRow.transform.localPosition = new Vector3(newRow.transform.localPosition.x, -yOffset, 0);
            
            yOffset += rowHeight + 10f; // Add dynamic height and 10px spacing between rows

            Transform inputLvTr = newRow.transform.Find("InputLv");
            Transform btnLvTr = newRow.transform.Find("BtnChangeLv");
            Transform lblDynamicTr = newRow.transform.Find("LabelDynamic");

            if (lblDynamicTr != null) lblDynamicTr.gameObject.SetActive(false);
            if (inputLvTr != null) inputLvTr.gameObject.SetActive(false);
            if (btnLvTr != null) btnLvTr.gameObject.SetActive(false);
        }
        
        RectTransform contentRt = vocabListContent.GetComponent<RectTransform>();
        if (contentRt != null) contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, yOffset + 50);
    }

    private void ChangeVocabLevel(long id, int newLevel)
    {
        if (statusText != null) statusText.text = "Changing vocab level...";
        StartCoroutine(ApiRequestCoroutine($"/api/admin/vocabularies/{id}/level?level={newLevel}", "PUT", null, (code, body) => {
            if (statusText != null) statusText.text = code == 200 ? "Level changed successfully! Reload to see updates." : "Error changing level";
        }));
    }

    // =================================== API HELPER ===================================
    private IEnumerator ApiRequestCoroutine(string endpoint, string method, string bodyStr, System.Action<long, string> callback)
    {
        string url = "http://localhost:8080" + endpoint;
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            if (!string.IsNullOrEmpty(bodyStr))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyStr);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string token = PlayerPrefs.GetString("jwt_token", "");
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                PlayerPrefs.DeleteKey("jwt_token");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                yield break;
            }

            callback?.Invoke(request.responseCode, request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class VocabDto
{
    public long id;
    public string wordJp;
    public string kana;
    public string romaji;
    public string meaning;
    public int level;
    public int levelRequired;
}

[System.Serializable]
public class VocabListDto
{
    public List<VocabDto> vocabs;
}
