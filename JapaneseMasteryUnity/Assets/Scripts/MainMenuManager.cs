using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

    private float timeElapsed = 0f;

    void Start()
    {
        if (Global.Instance != null) Global.Instance.EnsureCamera();
        
        RefreshData();
        
        btnPlacement.onClick.AddListener(() => SceneManager.LoadScene("LevelTest"));
        btnQuantum.onClick.AddListener(OnQuantumPressed);
        btnSolo.onClick.AddListener(OnSoloPressed);
        btnPvP.onClick.AddListener(OnPvPPressed);
        btnAdmin.onClick.AddListener(() => SceneManager.LoadScene("AdminPanel"));
        btnMod.onClick.AddListener(() => SceneManager.LoadScene("ModeratorPanel"));
        btnDesigner.onClick.AddListener(() => SceneManager.LoadScene("DesignerPanel"));
        btnCreateTest.onClick.AddListener(() => SceneManager.LoadScene("SenseiTestManager"));
        btnEnterTestCode.onClick.AddListener(OnEnterTestCodePressed);
        btnLogout.onClick.AddListener(OnLogoutPressed);
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
        // Need to add UI modal logic here
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
