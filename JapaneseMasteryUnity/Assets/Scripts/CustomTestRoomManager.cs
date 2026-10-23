using UnityEngine;
using UnityEngine.UI;

public class CustomTestRoomManager : MonoBehaviour
{
    public Button btnBack;
    
    void Start()
    {
        btnBack.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Main"));
        // TODO: Port Custom Test Room from CustomTestRoom.gd
    }
}
