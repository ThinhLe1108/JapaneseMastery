using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(Image))]
public class ThemeLoader : MonoBehaviour
{
    private Image targetImage;

    void Start()
    {
        targetImage = GetComponent<Image>();
        string currentTheme = PlayerPrefs.GetString("MenuThemeUrl", "");
        if (!string.IsNullOrEmpty(currentTheme))
        {
            StartCoroutine(LoadImageCoroutine(currentTheme));
        }
    }

    IEnumerator LoadImageCoroutine(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                if (texture != null && targetImage != null)
                {
                    targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    targetImage.color = Color.white;
                }
            }
        }
    }
}
