using UnityEngine;
using UnityEngine.UI;

public class ModeratorPanelManager : MonoBehaviour
{
    public Button btnBack;
    
    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        // TODO: Port Moderator functionality from ModeratorPanel.gd
    }
}
