using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Video;
using System;

public class MainMenuManager : MonoBehaviour
{
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI jlptLabel;
    public TextMeshProUGUI coinLabel;
    public TextMeshProUGUI titleLabel;

    public Button btnPlacement;
    public Button btnQuantum;
    public Button btnSolo;
    public Button btnPvP;
    public Button btnShop;
    public Button btnAdmin;
    public Button btnMod;
    public Button btnCreateTest;
    public Button btnEnterTestCode;
    public Button btnDesigner;
    public Button btnLogout;

    [Header("Test Code Modal")]
    public GameObject enterTestDialog;
    public TMPro.TMP_InputField testCodeInput;
    public Button btnSubmitTestCode;
    public Button btnCloseTestCode;
    public TextMeshProUGUI testCodeErrorLabel;

    private float timeElapsed = 0f;

    // Cache theme texture so it loads instantly on subsequent visits
    private static Texture2D cachedThemeTexture;
    private static string cachedThemeUrl = "";
    private static RenderTexture videoRenderTexture;

    void Start()
    {
        if (Global.Instance != null) Global.Instance.EnsureCamera();
        
        RefreshData();
        
        btnPlacement.onClick.AddListener(() => SceneManager.LoadScene("LevelTest"));
        btnQuantum.onClick.AddListener(OnQuantumPressed);
        btnSolo.onClick.AddListener(OnSoloPressed);
        btnPvP.onClick.AddListener(OnPvPPressed);
        btnShop.onClick.AddListener(() => SceneManager.LoadScene("Shop"));
        btnAdmin.onClick.AddListener(() => SceneManager.LoadScene("AdminPanel"));
        btnMod.onClick.AddListener(() => SceneManager.LoadScene("ModeratorPanel"));
        btnDesigner.onClick.AddListener(() => SceneManager.LoadScene("DesignerPanel"));
        btnCreateTest.onClick.AddListener(() => SceneManager.LoadScene("SenseiTestManager"));
        btnEnterTestCode.onClick.AddListener(OnEnterTestCodePressed);
        btnLogout.onClick.AddListener(OnLogoutPressed);

        if (enterTestDialog != null) enterTestDialog.SetActive(false);
        if (btnSubmitTestCode != null) btnSubmitTestCode.onClick.AddListener(OnSubmitTestCodePressed);
        if (btnCloseTestCode != null) btnCloseTestCode.onClick.AddListener(() => enterTestDialog.SetActive(false));

        ApplyThemeBackground();
    }

    private string GetUserKey(string baseKey)
    {
        return baseKey + "_" + Global.DisplayName;
    }

    private void ApplyThemeBackground()
    {
        string themeUrl = PlayerPrefs.GetString(GetUserKey("MenuThemeUrl"), "");
        if (!string.IsNullOrEmpty(themeUrl))
        {
            // If it's a video, stream it directly with VideoPlayer
            if (themeUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || themeUrl.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
            {
                ApplyVideoToBackground(themeUrl);
                return;
            }

            // Tier 1: Static memory cache (instant, same session)
            if (cachedThemeTexture != null && cachedThemeUrl == themeUrl)
            {
                ApplyTextureToBackground(cachedThemeTexture);
                return;
            }

            // Tier 2: Local disk cache (instant, cross-session)
            string cacheFile = GetDiskCachePath(themeUrl);
            if (System.IO.File.Exists(cacheFile))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(cacheFile);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    cachedThemeTexture = tex;
                    cachedThemeUrl = themeUrl;
                    ApplyTextureToBackground(tex);
                    return;
                }
            }

            // Tier 3: Download from Cloudinary (first time only)
            StartCoroutine(LoadThemeCoroutine(themeUrl));
        }
        else
        {
            cachedThemeTexture = null;
            cachedThemeUrl = "";
            UpdateBackgroundVisibility();
        }
    }

    private void ApplyVideoToBackground(string videoUrl)
    {
        GameObject bgObj = GameObject.Find("ThemeBackground");
        RawImage bgImage = null;
        VideoPlayer vp = null;
        if (bgObj == null)
        {
            var canvas = GameObject.FindObjectOfType<Canvas>();
            bgObj = new GameObject("ThemeBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(VideoPlayer));
            bgObj.transform.SetParent(canvas.transform, false);
            bgObj.transform.SetAsFirstSibling();
            bgObj.layer = LayerMask.NameToLayer("UI");
            
            RectTransform rt = bgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            
            bgImage = bgObj.GetComponent<RawImage>();
            vp = bgObj.GetComponent<VideoPlayer>();
        }
        else
        {
            bgImage = bgObj.GetComponent<RawImage>();
            vp = bgObj.GetComponent<VideoPlayer>();
            if (vp == null) vp = bgObj.AddComponent<VideoPlayer>();
        }

        bgImage.color = Color.white;
        
        if (videoRenderTexture == null)
        {
            videoRenderTexture = new RenderTexture(1920, 1080, 0);
        }
        
        vp.playOnAwake = true;
        vp.isLooping = true;
        vp.url = videoUrl;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = videoRenderTexture;
        vp.audioOutputMode = VideoAudioOutputMode.None; // Mute video backgrounds
        
        bgImage.texture = videoRenderTexture;
        vp.Play();
        Debug.Log("[ThemeBG] SUCCESS - Video background streaming!");
    }

    private string GetDiskCachePath(string url)
    {
        // Create a unique filename from the URL hash
        int hash = url.GetHashCode();
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "ThemeCache");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, "theme_" + Mathf.Abs(hash) + ".jpg");
    }

    private IEnumerator LoadThemeCoroutine(string url)
    {
        string downloadUrl = url;
        if (downloadUrl.EndsWith(".webp")) downloadUrl = downloadUrl.Substring(0, downloadUrl.Length - 5) + ".png";
        
        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(downloadUrl))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                if (texture != null)
                {
                    // Save to memory cache
                    cachedThemeTexture = texture;
                    cachedThemeUrl = url;

                    // Save to disk cache for instant loading next time
                    try {
                        byte[] jpgData = texture.EncodeToJPG(85);
                        System.IO.File.WriteAllBytes(GetDiskCachePath(url), jpgData);
                    } catch (System.Exception) { }

                    ApplyTextureToBackground(texture);
                }
            }
        }
    }
    
    private void ApplyTextureToBackground(Texture2D texture)
    {
        GameObject bgObj = GameObject.Find("ThemeBackground");
        RawImage bgImage = null;
        if (bgObj == null)
        {
            var canvas = GameObject.FindObjectOfType<Canvas>();
            bgObj = new GameObject("ThemeBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            bgObj.transform.SetParent(canvas.transform, false);
            bgObj.transform.SetAsFirstSibling();
            bgObj.layer = LayerMask.NameToLayer("UI");
            
            RectTransform rt = bgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            
            bgImage = bgObj.GetComponent<RawImage>();
        }
        else
        {
            bgImage = bgObj.GetComponent<RawImage>();
        }

        // If a video player exists, stop it since we are applying a static image
        VideoPlayer vp = bgObj.GetComponent<VideoPlayer>();
        if (vp != null) {
            vp.Stop();
            vp.targetTexture = null;
        }

        if (bgImage != null)
        {
            bgImage.texture = texture;
            bgImage.color = Color.white;
            Debug.Log("[ThemeBG] SUCCESS - Theme background applied!");
        }
    }
    
    private void UpdateBackgroundVisibility() {
        string themeUrl = PlayerPrefs.GetString(GetUserKey("MenuThemeUrl"), "");
        GameObject bgObj = GameObject.Find("ThemeBackground");
        if (string.IsNullOrEmpty(themeUrl)) {
            if (bgObj != null) Destroy(bgObj);
        }
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (titleLabel != null)
        {
            // Hover effect - Anchor to top (0.5, 1) and position at -50 from top
            RectTransform rt = titleLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -50 + Mathf.Sin(timeElapsed * 1.5f) * 10f);
        }
    }

    private void RefreshData()
    {
        // Example logic
        int level = Global.CurrentLevel;
        nameLabel.text = "Player: " + Global.DisplayName;
        jlptLabel.text = "Level: " + level;
        coinLabel.text = "G-Coins: " + Global.Coins;

        btnPlacement.GetComponentInChildren<TextMeshProUGUI>().text = $"Level-Up Test (Lv {level} -> {level + 1})";
        btnSolo.GetComponentInChildren<TextMeshProUGUI>().text = $"Learn (Level {level})";

        string role = Global.Role;

        // Hide all first
        btnAdmin.gameObject.SetActive(false);
        btnMod.gameObject.SetActive(false);
        btnDesigner.gameObject.SetActive(false);
        btnCreateTest.gameObject.SetActive(false);
        btnEnterTestCode.gameObject.SetActive(false);
        jlptLabel.gameObject.SetActive(false);
        coinLabel.gameObject.SetActive(false);
        btnPlacement.gameObject.SetActive(false);
        btnSolo.gameObject.SetActive(false);
        btnPvP.gameObject.SetActive(false);
        btnShop.gameObject.SetActive(false);
        btnQuantum.gameObject.SetActive(false);

        if (role == "ADMIN")
        {
            btnAdmin.gameObject.SetActive(true);
        }
        else if (role == "MODERATOR")
        {
            btnMod.gameObject.SetActive(true);
        }
        else if (role == "DESIGNER")
        {
            btnDesigner.gameObject.SetActive(true);
        }
        else if (role == "SENSEI")
        {
            btnCreateTest.gameObject.SetActive(true);
        }
        else // PLAYER
        {
            jlptLabel.gameObject.SetActive(true);
            coinLabel.gameObject.SetActive(true);
            btnPlacement.gameObject.SetActive(true);
            btnSolo.gameObject.SetActive(true);
            btnPvP.gameObject.SetActive(true);
            btnShop.gameObject.SetActive(true);
            btnQuantum.gameObject.SetActive(true);
            btnEnterTestCode.gameObject.SetActive(true);
        }
    }

    private void OnQuantumPressed()
    {
        // Add UI dialog code here for Unity
        Global.QuantumMode = 1; 
        SceneManager.LoadScene("QuantumTest");
    }

    private void OnSoloPressed()
    {
        Global.SoloTargetLevel = Global.CurrentLevel;
        SceneManager.LoadScene("SoloLearning");
    }

    private void OnPvPPressed()
    {
        if (Global.CurrentLevel <= 24)
        {
            Debug.LogWarning("You must be level 25 or higher to enter PvP.");
            return;
        }
        SceneManager.LoadScene("PvP");
    }

    private void OnEnterTestCodePressed()
    {
        if (enterTestDialog != null)
        {
            testCodeInput.text = "";
            if (testCodeErrorLabel != null) testCodeErrorLabel.gameObject.SetActive(false);
            enterTestDialog.SetActive(true);
        }
    }

    private void OnSubmitTestCodePressed()
    {
        string code = testCodeInput != null ? testCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code)) return;

        if (btnSubmitTestCode != null) btnSubmitTestCode.interactable = false;
        if (testCodeErrorLabel != null)
        {
            testCodeErrorLabel.text = "Searching...";
            testCodeErrorLabel.color = Color.yellow;
            testCodeErrorLabel.gameObject.SetActive(true);
        }

        StartCoroutine(FetchTestCodeCoroutine(code));
    }

    private System.Collections.IEnumerator FetchTestCodeCoroutine(string code)
    {
        int userId = Global.UserId;
        string url = NetworkManager.Instance.BASE_URL + $"/api/player/{userId}/custom-tests/{code}";

        using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            yield return req.SendWebRequest();

            if (btnSubmitTestCode != null) btnSubmitTestCode.interactable = true;

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    var testData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                    int minLvl = testData.ContainsKey("minLevel") ? System.Convert.ToInt32(testData["minLevel"]) : 1;
                    if (Global.CurrentLevel < minLvl)
                    {
                        if (testCodeErrorLabel != null)
                        {
                            testCodeErrorLabel.text = $"Level {minLvl} required!";
                            testCodeErrorLabel.color = Color.red;
                        }
                    }
                    else
                    {
                        Global.CurrentCustomTest = testData;
                        SceneManager.LoadScene("CustomTestRoom");
                    }
                }
                catch
                {
                    if (testCodeErrorLabel != null) { testCodeErrorLabel.text = "Invalid data."; testCodeErrorLabel.color = Color.red; }
                }
            }
            else if (req.responseCode == 400)
            {
                if (testCodeErrorLabel != null) { testCodeErrorLabel.text = req.downloadHandler.text; testCodeErrorLabel.color = Color.red; }
            }
            else
            {
                if (testCodeErrorLabel != null) { testCodeErrorLabel.text = "Test code not found or invalid."; testCodeErrorLabel.color = Color.red; }
            }
        }
    }

    private void OnLogoutPressed()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Logout();
        }
        SceneManager.LoadScene("Login");
    }
}
