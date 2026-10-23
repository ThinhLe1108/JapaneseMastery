using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;
using System;

public class ShopManager : MonoBehaviour
{
    public TextMeshProUGUI statusLabel;
    public Button btnBack;
    
    public TMP_InputField searchInput;
    public Transform itemsContainer;
    public GameObject itemRowPrefab; 

    public TextMeshProUGUI lblPage;
    public Button btnPrev;
    public Button btnNext;

    public Button btnShopTab;
    public Button btnThemeManagerTab;
    public TextMeshProUGUI tabTitle;

    private bool isThemeManagerTab = false;
    private List<Dictionary<string, object>> allItems = new List<Dictionary<string, object>>();
    private int currentPage = 0;
    private int itemsPerPage = 3;

    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        
        btnPrev.onClick.AddListener(() => ChangePage(-1));
        btnNext.onClick.AddListener(() => ChangePage(1));
        
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);

        // Automatically find UI components since Inspector serialization failed
        if (tabTitle == null) {
            var tmps = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach(var t in tmps) {
                if (t.gameObject.scene.isLoaded && t.text != null) {
                    string txt = t.text.ToLower();
                    if (txt.Contains("theme") && txt.Contains("manager")) {
                        // Skip if it's the tab button text
                        if (t.transform.parent != null && t.transform.parent.name.Contains("Tab")) continue;
                        
                        tabTitle = t;
                        break;
                    }
                }
            }
        }
        
        if (btnShopTab == null) {
            var b = GameObject.Find("BtnShopTab");
            if (b != null) btnShopTab = b.GetComponent<Button>();
        }
        
        if (btnThemeManagerTab == null) {
            var b = GameObject.Find("BtnThemeManagerTab");
            if (b != null) btnThemeManagerTab = b.GetComponent<Button>();
        }

        if (btnShopTab != null) btnShopTab.onClick.AddListener(() => SetTab(false));
        if (btnThemeManagerTab != null) btnThemeManagerTab.onClick.AddListener(() => SetTab(true));

        SetTab(false); // Default to Shop tab
        FetchItems();
    }

    void SetTab(bool isManager)
    {
        isThemeManagerTab = isManager;
        currentPage = 0;
        if (tabTitle != null) tabTitle.text = isManager ? "Theme Manager" : "Shop";
        
        if (btnShopTab != null) {
            btnShopTab.GetComponent<Image>().color = isManager ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
        }
        if (btnThemeManagerTab != null) {
            btnThemeManagerTab.GetComponent<Image>().color = isManager ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.2f, 0.2f, 0.2f);
        }

        RenderItems();
    }

    void FetchItems()
    {
        if (statusLabel != null) statusLabel.text = "Loading shop...";
        StartCoroutine(GetRequest("/api/shop/items"));
    }

    void OnSearchChanged(string text)
    {
        currentPage = 0;
        RenderItems();
    }

    void ChangePage(int direction)
    {
        List<Dictionary<string, object>> filtered = GetFilteredItems();
        int maxPage = Mathf.CeilToInt((float)filtered.Count / itemsPerPage) - 1;
        if (maxPage < 0) maxPage = 0;

        currentPage += direction;
        if (currentPage < 0) currentPage = 0;
        if (currentPage > maxPage) currentPage = maxPage;

        RenderItems();
    }

    string GetUserKey(string baseKey)
    {
        return baseKey + "_" + Global.DisplayName;
    }

    List<Dictionary<string, object>> GetFilteredItems()
    {
        string term = searchInput != null ? searchInput.text.ToLower() : "";
        string purchased = PlayerPrefs.GetString(GetUserKey("PurchasedThemes"), "");

        List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
        foreach(var it in allItems) {
            string n = it.ContainsKey("name") ? it["name"].ToString().ToLower() : "";
            string id = it.ContainsKey("id") ? it["id"].ToString() : "";
            
            bool matchesSearch = string.IsNullOrEmpty(term) || n.Contains(term);
            
            // If in ThemeManager, must be purchased (or free/owned)
            bool isPurchased = purchased.Contains("[" + id + "]");
            if (isThemeManagerTab && !isPurchased) {
                continue;
            }

            if (matchesSearch) res.Add(it);
        }
        return res;
    }

    void RenderItems()
    {
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        List<Dictionary<string, object>> filtered = GetFilteredItems();
        int maxPage = Mathf.CeilToInt((float)filtered.Count / itemsPerPage) - 1;
        if (maxPage < 0) maxPage = 0;
        if (currentPage > maxPage) currentPage = maxPage;

        if (lblPage != null) lblPage.text = $"{currentPage + 1} / {maxPage + 1}";

        int startIndex = currentPage * itemsPerPage;
        for (int i = startIndex; i < startIndex + itemsPerPage && i < filtered.Count; i++)
        {
            var item = filtered[i];
            GameObject rowObj = Instantiate(itemRowPrefab, itemsContainer);
            rowObj.SetActive(true);

            TextMeshProUGUI[] texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            string name = item.ContainsKey("name") ? item["name"].ToString() : "";
            string type = item.ContainsKey("type") ? item["type"].ToString() : "";
            string price = item.ContainsKey("price") ? item["price"].ToString() : "0";
            
            // Assume texts[0] = name, texts[1] = type, texts[2] = price, texts[3] = status
            if (texts.Length > 0) texts[0].text = name;
            if (texts.Length > 1) texts[1].text = type;
            if (texts.Length > 2) texts[2].text = ""; // Hide price to avoid overlap with button
            
            if (texts.Length > 3) {
                texts[3].text = ""; // status doesn't matter here
            }

            // Image Thumbnail
            if (item.ContainsKey("secureUrl"))
            {
                string url = item["secureUrl"].ToString();
                Image img = rowObj.GetComponent<Image>();
                if (img != null)
                {
                    StartCoroutine(LoadImageCoroutine(url, img, texts.Length > 0 ? texts[0].GetComponent<RectTransform>() : null));
                }
            }

            // Setup Buy / Apply Buttons
            Button[] buttons = rowObj.GetComponentsInChildren<Button>();
            if (buttons.Length > 0) {
                Button primaryBtn = buttons[0]; 
                primaryBtn.onClick.AddListener(() => OnItemClick(item));
                
                string currentTheme = PlayerPrefs.GetString(GetUserKey("MenuThemeUrl"), "");
                string purchased = PlayerPrefs.GetString(GetUserKey("PurchasedThemes"), "");
                string id = item.ContainsKey("id") ? item["id"].ToString() : "";
                bool isPurchased = purchased.Contains("[" + id + "]");

                if (item.ContainsKey("secureUrl") && item["secureUrl"].ToString() == currentTheme) {
                    primaryBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Remove";
                    primaryBtn.interactable = true;
                } else if (isPurchased) {
                    primaryBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Apply";
                    primaryBtn.interactable = true;
                } else {
                    primaryBtn.GetComponentInChildren<TextMeshProUGUI>().text = $"Buy ({price}G)";
                    primaryBtn.interactable = true;
                }
            }
        }
    }

    void OnItemClick(Dictionary<string, object> item)
    {
        string id = item["id"].ToString();
        string url = item["secureUrl"].ToString();
        string purchased = PlayerPrefs.GetString(GetUserKey("PurchasedThemes"), "");
        string currentTheme = PlayerPrefs.GetString(GetUserKey("MenuThemeUrl"), "");
        
        bool isPurchased = purchased.Contains("[" + id + "]");

        if (url == currentTheme) {
            // Remove the theme
            PlayerPrefs.SetString(GetUserKey("MenuThemeUrl"), "");
            PlayerPrefs.Save();
            if (statusLabel != null) statusLabel.text = "Theme Removed!";
            RenderItems();
        } else if (!isPurchased) {
            // Buy: check coins first
            int price = item.ContainsKey("price") ? Convert.ToInt32(item["price"]) : 0;
            if (Global.Coins < price) {
                if (statusLabel != null) statusLabel.text = "Not enough G-Coins!";
                return;
            }
            // Call backend to deduct coins
            StartCoroutine(BuyItemCoroutine(id, price, purchased));
        } else {
            // Apply the item
            PlayerPrefs.SetString(GetUserKey("MenuThemeUrl"), url);
            PlayerPrefs.Save();
            if (statusLabel != null) statusLabel.text = "Applied Theme!";
            RenderItems();
        }
    }

    IEnumerator BuyItemCoroutine(string itemId, int price, string currentPurchased)
    {
        string buyUrl = NetworkManager.Instance.BASE_URL + $"/api/shop/items/{itemId}/buy?userId={Global.UserId}";
        
        using (UnityWebRequest uwr = UnityWebRequest.PostWwwForm(buyUrl, ""))
        {
            uwr.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            uwr.SetRequestHeader("Content-Type", "application/json");
            yield return uwr.SendWebRequest();
            
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                // Backend returns updated User object - update coins
                string json = uwr.downloadHandler.text;
                try {
                    var userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (userData.ContainsKey("gCoin")) {
                        Global.Coins = Convert.ToInt32(userData["gCoin"]);
                    }
                } catch (Exception) {
                    // Fallback: deduct locally
                    Global.Coins -= price;
                }
                
                // Mark as purchased locally
                PlayerPrefs.SetString(GetUserKey("PurchasedThemes"), currentPurchased + "[" + itemId + "]");
                PlayerPrefs.Save();
                
                if (statusLabel != null) statusLabel.text = $"Purchased! G-Coins: {Global.Coins}";
                RenderItems();
            }
            else
            {
                string errorMsg = uwr.downloadHandler != null ? uwr.downloadHandler.text : uwr.error;
                if (statusLabel != null) statusLabel.text = "Purchase failed: " + errorMsg;
            }
        }
    }

    IEnumerator LoadImageCoroutine(string url, Image targetImage, RectTransform nameRt = null)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        if (url.EndsWith(".webp")) url = url.Substring(0, url.Length - 5) + ".png";
        else if (url.EndsWith(".mp4")) url = url.Substring(0, url.Length - 4) + ".jpg";
        else if (url.EndsWith(".webm")) url = url.Substring(0, url.Length - 5) + ".jpg";

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                if (texture != null && targetImage != null)
                {
                    targetImage.sprite = null;
                    targetImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

                    GameObject thumbGo = new GameObject("Thumbnail");
                    thumbGo.transform.SetParent(targetImage.transform, false);
                    RectTransform rt = thumbGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.sizeDelta = new Vector2(140, 80);
                    rt.anchoredPosition = new Vector2(80, 0); 

                    Image thumbImg = thumbGo.AddComponent<Image>();
                    thumbImg.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    thumbImg.preserveAspect = true;

                    if (nameRt != null)
                    {
                        nameRt.anchoredPosition = new Vector2(nameRt.anchoredPosition.x + 150, nameRt.anchoredPosition.y);
                        nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x - 150, nameRt.sizeDelta.y);
                    }
                }
            }
        }
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(NetworkManager.Instance.BASE_URL + uri))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                if (statusLabel != null) statusLabel.text = "Error: " + webRequest.error;
            }
            else
            {
                string json = webRequest.downloadHandler.text;
                try
                {
                    // Backend returns an object with "value" array, or just an array? 
                    // Wait, the API curl returned: { "value": [...], "Count": 1 } because I used Invoke-RestMethod which sometimes wraps it or maybe not?
                    // Let's parse as JObject first to be safe, or check if it starts with '{' or '['
                    if (json.TrimStart().StartsWith("{")) {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        if (dict.ContainsKey("content")) {
                            allItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dict["content"].ToString());
                        } else if (dict.ContainsKey("value")) {
                            allItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dict["value"].ToString());
                        } else {
                            allItems = new List<Dictionary<string, object>>();
                        }
                    } else {
                        allItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    }
                    
                    List<Dictionary<string, object>> filtered = new List<Dictionary<string, object>>();
                    foreach(var item in allItems) {
                        bool isApproved = false;
                        if (item.ContainsKey("approved")) {
                            isApproved = item["approved"].ToString().ToLower() == "true" || item["approved"].ToString() == "1";
                        } else if (item.ContainsKey("is_approved")) {
                            isApproved = item["is_approved"].ToString().ToLower() == "true" || item["is_approved"].ToString() == "1";
                        }
                        
                        if (isApproved && item.ContainsKey("type") && item["type"].ToString() == "THEME") {
                            filtered.Add(item);
                        }
                    }
                    allItems = filtered;

                    if (statusLabel != null) statusLabel.text = "";
                    RenderItems();
                }
                catch (Exception e)
                {
                    if (statusLabel != null) statusLabel.text = "Parse Error: " + e.Message;
                }
            }
        }
    }
}
