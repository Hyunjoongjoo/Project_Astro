using UnityEngine;
using UnityEngine.SceneManagement;

public class Options : MonoBehaviour
{
    public void OnClickLogOut() 
    {
        AuthService.Instance.Logout();
        GameManager.Instance.SetSceneState(SceneState.Title);
        UserDataManager.Instance.ClearCache();
        SceneManager.LoadScene("Title");
    }
    
    public void OnClickCloseGame() 
    { 
        Application.Quit();
    }

    public async void OnClickDeleteUser()
    {
        string uid = AuthService.Instance.CurrentUser.UserId;

        await UserDataStore.Instance.DeleteUserId(uid);

        bool authDeleted = await AuthService.Instance.DeleteUserAuth();

        if (authDeleted)
        {
            UserDataManager.Instance.ClearCache();
            GameManager.Instance.SetSceneState(SceneState.Title);
            SceneManager.LoadScene("Title");
        }
    }
}
