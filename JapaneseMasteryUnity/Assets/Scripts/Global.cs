using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Global : MonoBehaviour
{
    // Singleton pattern for Global manager
    private static Global _instance;
    public static Global Instance { 
        get {
            if (_instance == null) {
                _instance = UnityEngine.Object.FindObjectOfType<Global>();
                if (_instance == null) {
                    GameObject go = new GameObject("GlobalManager");
                    _instance = go.AddComponent<Global>();
                    Debug.Log("Global: Created new persistent GlobalManager instance.");
                }
            }
            return _instance;
        }
    }

    // User Data Properties
    public static string DisplayName { 
        get { return Database.Instance.GetAccount().ContainsKey("display_name") ? Database.Instance.GetAccount()["display_name"].ToString() : "Guest"; }
        set { Database.Instance.UpdateAccount("display_name", value); }
    }
    public static int CurrentLevel {
        get { return Database.Instance.GetAccount().ContainsKey("current_level") ? System.Convert.ToInt32(Database.Instance.GetAccount()["current_level"]) : 1; }
        set { 
            Database.Instance.UpdateAccount("current_level", value); 
            if (_instance != null) _instance.SyncProgressToServer();
        }
    }
    public static int Coins {
        get { return Database.Instance.GetAccount().ContainsKey("g_coin_balance") ? System.Convert.ToInt32(Database.Instance.GetAccount()["g_coin_balance"]) : 0; }
        set { 
            Database.Instance.UpdateAccount("g_coin_balance", value); 
            if (_instance != null) _instance.SyncCoinsToServer();
        }
    }
    public static string Role {
        get { return Database.Instance.GetAccount().ContainsKey("role") ? Database.Instance.GetAccount()["role"].ToString() : "PLAYER"; }
        set { Database.Instance.UpdateAccount("role", value); }
    }
    public static int UserId {
        get { return Database.Instance.GetAccount().ContainsKey("user_id") ? System.Convert.ToInt32(Database.Instance.GetAccount()["user_id"]) : -1; }
        set { Database.Instance.UpdateAccount("user_id", value); }
    }

    // Cross-scene data transfer variables
    public static int QuantumMode { get; set; } = 1;
    public static int SoloTargetLevel { get; set; } = 1;
    public static Dictionary<string, object> CurrentCustomTest { get; set; } = null;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureCamera();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCamera();
    }

    public void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = UnityEngine.Object.FindObjectOfType<Camera>();
        }

        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            Debug.Log("Global: Created missing Main Camera in scene: " + SceneManager.GetActiveScene().name);
        }

        cam.enabled = true;
        // Standard orthographic setup
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = -1; // Render everything
        cam.depth = 100; // Be on top of any other cameras
        
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        // Disable any other cameras that might be interfering
        Camera[] allCameras = UnityEngine.Object.FindObjectsOfType<Camera>();
        foreach (var c in allCameras)
        {
            if (c != cam)
            {
                Debug.Log("Global: Disabling extra camera: " + c.name);
                c.enabled = false;
            }
        }

        // Ensure all Canvases have a camera if they need one
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
        foreach (var cv in canvases)
        {
            if (cv.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cv.renderMode = RenderMode.ScreenSpaceCamera;
                cv.worldCamera = cam;
                cv.planeDistance = 9f;
            }
        }
    }

    // Helper functions
    public static void AddCoins(int amount)
    {
        Coins += amount;
        Debug.Log("Added " + amount + " G-Coins. Total: " + Coins);
    }

    public void SyncProgressToServer()
    {
        if (UserId != -1 && NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
        {
            StartCoroutine(SyncProgressCoroutine());
        }
    }

    private System.Collections.IEnumerator SyncProgressCoroutine()
    {
        string url = NetworkManager.Instance.BASE_URL + $"/api/player/{UserId}/progress?level={CurrentLevel}";
        using (UnityEngine.Networking.UnityWebRequest req = new UnityEngine.Networking.UnityWebRequest(url, "PUT"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("[]"));
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return req.SendWebRequest();
            if (req.responseCode == 401)
            {
                NetworkManager.Instance.Logout();
                SceneManager.LoadScene("Login");
            }
        }
    }

    public void SyncCoinsToServer()
    {
        if (UserId != -1 && NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.jwtToken))
        {
            StartCoroutine(SyncCoinsCoroutine());
        }
    }

    private System.Collections.IEnumerator SyncCoinsCoroutine()
    {
        string url = NetworkManager.Instance.BASE_URL + $"/api/player/{UserId}/coins?coins={Coins}";
        using (UnityEngine.Networking.UnityWebRequest req = new UnityEngine.Networking.UnityWebRequest(url, "PUT"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + NetworkManager.Instance.jwtToken);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return req.SendWebRequest();
            if (req.responseCode == 401)
            {
                NetworkManager.Instance.Logout();
                SceneManager.LoadScene("Login");
            }
        }
    }

    public static void GotoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
