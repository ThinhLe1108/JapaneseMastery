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
    public TMP_Dropdown typeOpt;
    public Button btnAdd;
    public Button btnBack;

    public TMP_InputField searchInput;
    public Transform itemsContainer;
    public GameObject itemRowPrefab; // Prefab with Name, Type, Price, Status Text

    public TextMeshProUGUI lblPage;
    public Button btnPrev;
    public Button btnNext;

    private List<Dictionary<string, object>> allItems = new List<Dictionary<string, object>>();
    private int currentPage = 0;
    private int itemsPerPage = 6;

    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        btnAdd.onClick.AddListener(OnAddPressed);
        
        btnPrev.onClick.AddListener(() => ChangePage(-1));
        btnNext.onClick.AddListener(() => ChangePage(1));
        
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        FetchItems();
    }

    void FetchItems()
    {
        int userId = Global.UserId;
        statusLabel.text = "Loading items...";
        StartCoroutine(GetRequest($"/api/designer/{userId}/items"));
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
        // Delete old UI elements
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        string q = searchInput.text.Trim().ToLower();
        List<Dictionary<string, object>> filtered = new List<Dictionary<string, object>>();

        if (string.IsNullOrEmpty(q))
        {
            filtered = allItems;
        }
        else
        {
            foreach (var itm in allItems)
            {
                string n = itm["name"].ToString().ToLower();
                string typ = itm["type"].ToString().ToLower();
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

        lblPage.text = $"{currentPage + 1} / {totalPages}";
        btnPrev.interactable = (currentPage > 0);
        btnNext.interactable = (currentPage < totalPages - 1);

        int startIdx = currentPage * itemsPerPage;
        int endIdx = Mathf.Min(startIdx + itemsPerPage, filtered.Count);

        for (int i = startIdx; i < endIdx; i++)
        {
            var itm = filtered[i];
            GameObject row = Instantiate(itemRowPrefab, itemsContainer);
            // Assuming the prefab has a script 'DesignerItemRow' to hold references
            // DesignerItemRow rowUI = row.GetComponent<DesignerItemRow>();
            // rowUI.lblName.text = itm["name"].ToString();
            // ... (assign variables)
        }
    }

    void OnAddPressed()
    {
        string n = nameInput.text.Trim();
        string pStr = priceInput.text.Trim();

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

        int userId = Global.UserId;
        var dict = new Dictionary<string, object>
        {
            { "name", n },
            { "price", priceVal },
            { "type", typeOpt.options[typeOpt.value].text }
        };

        statusLabel.text = "Selling...";
        string json = JsonConvert.SerializeObject(dict);
        StartCoroutine(PostRequest($"/api/designer/{userId}/items", json));
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(NetworkManager.Instance.BASE_URL + uri))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                statusLabel.text = "Error: " + webRequest.error;
            }
            else
            {
                allItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                currentPage = 0;
                UpdateListUI();
                statusLabel.text = "Loaded items successfully.";
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
