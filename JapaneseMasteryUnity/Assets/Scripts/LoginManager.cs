using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Assuming TextMeshPro for UI text

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button btnLogin;
    public Button btnRegister;
    public TextMeshProUGUI statusLabel;

    private void Start()
    {
        if (Global.Instance != null) Global.Instance.EnsureCamera();

        // In Unity, UI is typically built in the Editor (Canvas, Panels, etc.)
        // rather than via code like in Godot, but we attach listeners here.
        
        btnLogin.onClick.AddListener(OnLoginPressed);
        btnRegister.onClick.AddListener(OnRegisterPressed);

        // Assuming you have a NetworkManager script that handles API calls
        NetworkManager.Instance.OnLoginSuccess += OnLoginSuccess;
        NetworkManager.Instance.OnLoginFailed += OnError;
        NetworkManager.Instance.OnRegisterSuccess += OnRegisterSuccess;
        NetworkManager.Instance.OnRegisterFailed += OnError;
    }

    private void OnDestroy()
    {
        // Clean up events when scene changes or object is destroyed
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnLoginSuccess -= OnLoginSuccess;
            NetworkManager.Instance.OnLoginFailed -= OnError;
            NetworkManager.Instance.OnRegisterSuccess -= OnRegisterSuccess;
            NetworkManager.Instance.OnRegisterFailed -= OnError;
        }
    }

    private void OnLoginPressed()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            statusLabel.text = "Please fill in all fields";
            statusLabel.color = new Color(1.0f, 0.4f, 0.4f); // Red
            return;
        }

        statusLabel.color = new Color(0.8f, 0.8f, 0.8f); // Light Gray
        statusLabel.text = "Connecting...";
        
        NetworkManager.Instance.Login(usernameInput.text, passwordInput.text);
    }

    private void OnRegisterPressed()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            statusLabel.text = "Please fill in all fields";
            statusLabel.color = new Color(1.0f, 0.4f, 0.4f); // Red
            return;
        }

        statusLabel.color = new Color(0.8f, 0.8f, 0.8f); // Light Gray
        statusLabel.text = "Connecting...";
        
        NetworkManager.Instance.Register(usernameInput.text, passwordInput.text);
    }

    private void OnLoginSuccess(string data) // Modify parameter based on your Network script
    {
        statusLabel.color = new Color(0.4f, 1.0f, 0.4f); // Green
        statusLabel.text = "Login successful!";
        StartCoroutine(LoadMainSceneDelay(0.5f));
    }

    private void OnRegisterSuccess(string data)
    {
        statusLabel.color = new Color(0.4f, 1.0f, 0.4f); // Green
        statusLabel.text = "Registration successful!";
        StartCoroutine(LoadMainSceneDelay(0.5f));
    }

    private void OnError(string msg)
    {
        statusLabel.color = new Color(1.0f, 0.4f, 0.4f); // Red
        statusLabel.text = msg;
    }

    private IEnumerator LoadMainSceneDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Equivalent to Global.goto_scene("res://scenes/Main.tscn")
        SceneManager.LoadScene("Main");
    }
}
