using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMPro;

public class DesignerPanelManager : MonoBehaviour
{
    public TextMeshProUGUI statusLabel;
    public TMP_InputField nameInput;
    public TMP_InputField priceInput;
    public TMP_InputField pathInput;
    public TMP_Dropdown typeOpt;
    public Button btnAdd;
    public Button btnBack;

    public TMP_InputField searchInput;
    public Transform itemsContainer;
    public GameObject itemRowPrefab; // Prefab with DesignerItemRow script

    public TextMeshProUGUI lblPage;
    public Button btnPrev;
    public Button btnNext;

    public Button btnManagerTab;
    public Button btnUploadTab;
    public GameObject listPanel;
    public GameObject uploadPanel;

    private List<Dictionary<string, object>> allItems = new List<Dictionary<string, object>>();
    private int currentPage = 0;
    private int itemsPerPage = 3;

    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        btnAdd.onClick.AddListener(OnAddPressed);
        
        btnPrev.onClick.AddListener(() => ChangePage(-1));
        btnNext.onClick.AddListener(() => ChangePage(1));
        
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);

        if (btnManagerTab != null) btnManagerTab.onClick.AddListener(() => SetTab("manager"));
        if (btnUploadTab != null) btnUploadTab.onClick.AddListener(() => SetTab("upload"));
        SetTab("manager");

        FetchItems();
    }

    public void SetTab(string tabName)
    {
        if (uploadPanel != null) uploadPanel.SetActive(tabName == "upload");
        if (listPanel != null) listPanel.SetActive(tabName == "manager");

        if (btnUploadTab != null) btnUploadTab.GetComponent<Image>().color = (tabName == "upload") ? new Color(0.06f, 0.22f, 0.06f) : new Color(0.12f, 0.12f, 0.12f);
        if (btnManagerTab != null) btnManagerTab.GetComponent<Image>().color = (tabName == "manager") ? new Color(0.06f, 0.22f, 0.06f) : new Color(0.12f, 0.12f, 0.12f);
    }

    void FetchItems()
    {
        int userId = Global.UserId;
        if (statusLabel != null) statusLabel.text = "Loading items...";
        StartCoroutine(GetRequest($"/api/shop/items/designer/{userId}"));
    }

    void OnSearchChanged(string text)
    {
        currentPage = 0;
        UpdateListUI();
    }

    void ChangePage(int dir)
    {
        currentPage += dir;
        UpdateListUI();
    }

    void UpdateListUI()
    {
        if (itemsContainer == null) return;

        // Delete old UI elements
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        string q = searchInput != null ? searchInput.text.Trim().ToLower() : "";
        List<Dictionary<string, object>> filtered = new List<Dictionary<string, object>>();

        if (string.IsNullOrEmpty(q))
        {
            filtered = allItems;
        }
        else
        {
            foreach (var itm in allItems)
            {
                string n = itm.ContainsKey("name") ? itm["name"].ToString().ToLower() : "";
                string typ = itm.ContainsKey("type") ? itm["type"].ToString().ToLower() : "";
                if (n.Contains(q) || typ.Contains(q))
                {
                    filtered.Add(itm);
                }
            }
        }

        int totalPages = Mathf.CeilToInt((float)filtered.Count / itemsPerPage);
        if (totalPages == 0) totalPages = 1;
        if (currentPage >= totalPages) currentPage = totalPages - 1;
        if (currentPage < 0) currentPage = 0;

        if (lblPage != null) lblPage.text = $"{currentPage + 1} / {totalPages}";
        if (btnPrev != null) btnPrev.interactable = (currentPage > 0);
        if (btnNext != null) btnNext.interactable = (currentPage < totalPages - 1);

        int startIdx = currentPage * itemsPerPage;
        int endIdx = Mathf.Min(startIdx + itemsPerPage, filtered.Count);

        for (int i = startIdx; i < endIdx; i++)
        {
            var itm = filtered[i];
            GameObject row = Instantiate(itemRowPrefab, itemsContainer);
            row.SetActive(true);
            DesignerItemRow rowUI = row.GetComponent<DesignerItemRow>();
            if (rowUI != null)
            {
                rowUI.lblName.text = itm.ContainsKey("name") ? itm["name"].ToString() : "Unknown";
                rowUI.lblType.text = itm.ContainsKey("type") ? itm["type"].ToString() : "N/A";
                rowUI.lblPrice.text = itm.ContainsKey("price") ? itm["price"].ToString() + "G" : "0G";
                
                bool approved = false;
                if (itm.ContainsKey("approved")) approved = System.Convert.ToBoolean(itm["approved"]);
                else if (itm.ContainsKey("isApproved")) approved = System.Convert.ToBoolean(itm["isApproved"]);

                rowUI.lblStatus.text = approved ? "Approved" : "Pending";
                rowUI.lblStatus.color = approved ? Color.green : Color.yellow;

                string url = "";
                if (itm.ContainsKey("secureUrl")) url = itm["secureUrl"]?.ToString();
                else if (itm.ContainsKey("secure_url")) url = itm["secure_url"]?.ToString();

                if (!string.IsNullOrEmpty(url) && rowUI.imgPreview != null)
                {
                    StartCoroutine(LoadImageCoroutine(url, rowUI.imgPreview, rowUI.lblName.rectTransform));
                }
            }
        }
    }

    IEnumerator LoadImageCoroutine(string url, Image targetImage, RectTransform nameRt = null)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        
        // Unity's UnityWebRequestTexture does not support WebP out of the box in many versions.
        // Since Cloudinary supports auto-conversion via extension, we force .png instead.
        if (url.EndsWith(".webp"))
        {
            url = url.Substring(0, url.Length - 5) + ".png";
        }
        // Cloudinary auto-generates video thumbnails when you request .jpg instead of .mp4
        else if (url.EndsWith(".mp4"))
        {
            url = url.Substring(0, url.Length - 4) + ".jpg";
        }
        else if (url.EndsWith(".webm"))
        {
            url = url.Substring(0, url.Length - 5) + ".jpg";
        }

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                if (texture != null && targetImage != null)
                {
                    // Clear the stretched background
                    targetImage.sprite = null;
                    targetImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

                    // Create a thumbnail child object to preserve aspect ratio
                    GameObject thumbGo = new GameObject("Thumbnail");
                    thumbGo.transform.SetParent(targetImage.transform, false);
                    
                    RectTransform rt = thumbGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.sizeDelta = new Vector2(140, 80);
                    rt.anchoredPosition = new Vector2(80, 0); // padding from left

                    Image thumbImg = thumbGo.AddComponent<Image>();
                    thumbImg.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    thumbImg.preserveAspect = true;

                    // Shift name text to the right to make room for thumbnail
                    if (nameRt != null)
                    {
                        nameRt.anchoredPosition = new Vector2(nameRt.anchoredPosition.x + 150, nameRt.anchoredPosition.y);
                        nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x - 150, nameRt.sizeDelta.y);
                    }
                }
            }
        }
    }

    void OnAddPressed()
    {
        string n = nameInput.text.Trim();
        string pStr = priceInput.text.Trim();
        string path = pathInput != null ? pathInput.text.Trim() : "";

        if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(pStr))
        {
            statusLabel.text = "Please enter a name and price.";
            return;
        }

        if (!int.TryParse(pStr, out int priceVal) || priceVal < 0)
        {
            statusLabel.text = "Price must be a valid positive integer.";
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            statusLabel.text = "Please enter a file path to upload.";
            return;
        }

        if (!System.IO.File.Exists(path))
        {
            statusLabel.text = "File does not exist at path.";
            return;
        }

        int userId = Global.UserId;
        string type = typeOpt.options[typeOpt.value].text.ToUpper();

        statusLabel.text = "Uploading...";
        StartCoroutine(UploadItemRequest(n, type, priceVal, userId, path));
    }

    IEnumerator UploadItemRequest(string name, string type, int price, int designerId, string filePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        form.AddField("type", type);
        form.AddField("price", price);
        form.AddField("designerId", designerId);
        form.AddBinaryData("file", fileData, System.IO.Path.GetFileName(filePath), "image/png");

        using (UnityWebRequest webRequest = UnityWebRequest.Post(NetworkManager.Instance.BASE_URL + "/api/shop/items", form))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                statusLabel.text = "Error uploading item: " + webRequest.error;
            }
            else
            {
                statusLabel.text = "Successfully uploaded! Item is now available in shop.";
                nameInput.text = "";
                priceInput.text = "";
                if (pathInput != null) pathInput.text = "";
                FetchItems();
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
                statusLabel.text = "Error: " + webRequest.error;
            }
            else
            {
                allItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                currentPage = 0;
                UpdateListUI();
                statusLabel.text = $"Loaded {allItems.Count} items successfully.";
            }
        }
    }

    IEnumerator PostRequest(string uri, string jsonStr)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(NetworkManager.Instance.BASE_URL + uri, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonStr);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                statusLabel.text = "Error submitting item: " + webRequest.error;
            }
            else
            {
                statusLabel.text = "Successfully submitted! Please wait for Moderator approval.";
                nameInput.text = "";
                priceInput.text = "";
                FetchItems();
            }
        }
    }
}
