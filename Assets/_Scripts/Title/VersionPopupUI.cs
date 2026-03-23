using UnityEngine;

public class VersionPopupUI : MonoBehaviour
{
    private string _url;

    public void OnClickCheck()
    {
        Application.OpenURL(_url);
        Application.Quit();

    }
    public void Setup(string url)
    {
        _url = url;
        Debug.Log($"[VersionPopupUI] : Setup - {_url}");
    }
}
