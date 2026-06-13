using UnityEngine;
using UnityEngine.UI;

public class SenseiTestManager : MonoBehaviour
{
    public Button btnBack;
    
    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        // TODO: Port Sensei Test creation from SenseiTestManager.gd
    }
}
