using UnityEngine;
using UnityEngine.SceneManagement;

public class Options : MonoBehaviour
{
    public void OnClickLogOut() 
    {
        Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        GameManager.Instance.SetSceneState(SceneState.Title);
        SceneManager.LoadScene("Title");
    }
    public void OnClickCloseGame() 
    { 
        Application.Quit();
    }
}
