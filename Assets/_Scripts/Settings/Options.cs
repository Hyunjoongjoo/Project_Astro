using UnityEngine;
using UnityEngine.SceneManagement;

public class Options : MonoBehaviour
{
    public void OnClickLogOut() 
    {
        Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        SceneManager.LoadScene("Title");
    }
    public void OnClickCloseGame() 
    { 
        Application.Quit();
    }
}
