using System;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _instance;
    private static bool _isShuttingDown = false;

    public static NetworkManager Instance { 
        get {
            if (_isShuttingDown) return null;
            if (_instance == null) {
                _instance = UnityEngine.Object.FindObjectOfType<NetworkManager>();
                if (_instance == null) {
                    GameObject go = new GameObject("NetworkManager");
                    _instance = go.AddComponent<NetworkManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
        
        // Unity kills Coroutines on quit, so we must use a synchronous WebRequest 
        // to ensure the backend actually receives the logout command and deletes the access_token.
        if (isLoggedIn && !string.IsNullOrEmpty(jwtToken) && Global.UserId != -1)
        {
            try 
            {
                var request = System.Net.WebRequest.Create(BASE_URL + $"/api/auth/logout/{Global.UserId}");
                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + jwtToken);
                request.ContentType = "application/json";
                request.ContentLength = 0;
                request.GetResponse().Close(); // Synchronous blocking call
                Debug.Log("Synchronous logout to backend successful on quit.");
            } 
            catch (Exception e) 
            {
                Debug.LogWarning("Failed to notify backend of logout on quit: " + e.Message);
            }
        }

        // Log out locally and clear any saved tokens
        Logout(true);
        PlayerPrefs.DeleteKey("jwt_token");
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        // Don't set instance to null here because it might trigger recreation in other scripts' OnDestroy
        // if they access Instance. The _isShuttingDown flag handles it.
    }

    public string BASE_URL = "http://localhost:8080";
    public string jwtToken = "";
    public bool isLoggedIn = false;

    // Events (equivalent to Godot Signals)
    public Action<string> OnLoginSuccess;
    public Action<string> OnLoginFailed;
    public Action<string> OnRegisterSuccess;
    public Action<string> OnRegisterFailed;

    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(CheckHeartbeat), 2f, 2f);
    }

    private void CheckHeartbeat()
    {
        if (isLoggedIn && !string.IsNullOrEmpty(jwtToken) && Global.UserId != -1)
        {
            StartCoroutine(HeartbeatCoroutine());
        }
    }

    private System.Collections.IEnumerator HeartbeatCoroutine()
    {
        string url = BASE_URL + $"/api/player/{Global.UserId}/heartbeat";
        using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            yield return req.SendWebRequest();
            if (req.responseCode == 401)
            {
                Debug.Log("[Heartbeat] Bị đá văng do đăng nhập ở nơi khác!");
                Logout(true);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            }
        }
    }

    public void Login(string username, string password)
    {
        StartCoroutine(LoginCoroutine(username, password));
    }

    private System.Collections.IEnumerator LoginCoroutine(string username, string password)
    {
        string url = BASE_URL + "/api/auth/login";
        string json = $"{{\"username\":\"{username}\", \"password\":\"{password}\"}}";
        
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try 
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(request.downloadHandler.text);
                    jwtToken = response.ContainsKey("token") ? response["token"].ToString() : "";
                    
                    Global.DisplayName = response.ContainsKey("username") ? response["username"].ToString() : username;
                    Global.Role = response.ContainsKey("role") ? response["role"].ToString() : "PLAYER";
                    Global.UserId = response.ContainsKey("id") ? System.Convert.ToInt32(response["id"]) : 1;
                    Global.CurrentLevel = response.ContainsKey("level") ? System.Convert.ToInt32(response["level"]) : 1;
                    if (response.ContainsKey("gCoin")) Global.Coins = System.Convert.ToInt32(response["gCoin"]);

                    isLoggedIn = true;
                    OnLoginSuccess?.Invoke(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing login response: " + e.Message);
                    OnLoginFailed?.Invoke("Parse error: " + e.Message);
                }
            }
            else if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning("Backend offline. Using local mock login.");
                Global.DisplayName = username;
                string u = username.ToLower();
                if (u == "admin") Global.Role = "ADMIN";
                else if (u == "sensei") Global.Role = "SENSEI";
                else if (u == "designer") Global.Role = "DESIGNER";
                else if (u == "mod" || u == "moderator") Global.Role = "MODERATOR";
                else Global.Role = "PLAYER";
                OnLoginSuccess?.Invoke("Mock Success");
            }
            else
            {
                // Protocol Error, like 401 Unauthorized
                OnLoginFailed?.Invoke(request.error);
            }
        }
    }

    public void Register(string username, string password)
    {
        StartCoroutine(RegisterCoroutine(username, password));
    }

    private System.Collections.IEnumerator RegisterCoroutine(string username, string password)
    {
        string url = BASE_URL + "/api/auth/register";
        string json = $"{{\"username\":\"{username}\", \"password\":\"{password}\"}}";
        
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try 
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(request.downloadHandler.text);
                    jwtToken = response.ContainsKey("token") ? response["token"].ToString() : "";
                    
                    Global.DisplayName = response.ContainsKey("username") ? response["username"].ToString() : username;
                    Global.Role = response.ContainsKey("role") ? response["role"].ToString() : "PLAYER";
                    Global.UserId = response.ContainsKey("id") ? System.Convert.ToInt32(response["id"]) : 1;
                    Global.CurrentLevel = response.ContainsKey("level") ? System.Convert.ToInt32(response["level"]) : 1;
                    if (response.ContainsKey("gCoin")) Global.Coins = System.Convert.ToInt32(response["gCoin"]);

                    isLoggedIn = true;
                    OnRegisterSuccess?.Invoke(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing register response: " + e.Message);
                    OnRegisterFailed?.Invoke("Parse error: " + e.Message);
                }
            }
            else if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning("Backend offline. Using local mock register.");
                Global.DisplayName = username;
                Global.Role = "PLAYER";
                OnRegisterSuccess?.Invoke("Mock Success");
            }
            else
            {
                OnRegisterFailed?.Invoke(request.error);
            }
        }
    }

    public void Logout(bool localOnly = false)
    {
        if (!localOnly && Global.UserId != -1 && !string.IsNullOrEmpty(jwtToken))
        {
            StartCoroutine(LogoutCoroutine(Global.UserId, jwtToken));
        }

        jwtToken = "";
        isLoggedIn = false;
        Global.DisplayName = "New Player";
        Global.UserId = -1;
        Global.Role = "PLAYER";
        Global.CurrentLevel = 1;
        Global.Coins = 0;
        Debug.Log("Logged out locally.");
    }

    private System.Collections.IEnumerator LogoutCoroutine(int userId, string token)
    {
        string url = BASE_URL + $"/api/auth/logout/{userId}";
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.SetRequestHeader("Content-Type", "application/json");
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("Logged out from backend successfully.");
            }
            else
            {
                Debug.LogError("Failed to logout from backend: " + request.error);
            }
        }
    }
}
