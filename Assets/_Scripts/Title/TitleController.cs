using Firebase;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private AuthService _authService;
    [SerializeField] private UserDataStore _userDataStore;

    [Header("Controllers")]
    [SerializeField] private LoginController _loginController;
    [SerializeField] private SignUpController _signUpController;

    void Awake()
    {
        InitializeFirebase();
        GameManager.Instance.SetSceneState(SceneState.Title);
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().
            ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"Firebase Depenedency Error : {task.Result}");
                    return;
                }

                _authService.Initialize();
                _userDataStore.Initialize();

                _loginController.Initialize(_authService, _userDataStore, OnLoginComplete);
                _signUpController.Initialize(_authService, _userDataStore);
            });
    }

    private void OnLoginComplete(string nickname)
    {
        Debug.Log("[TitleController] 모든 로그인 로직 완료");
        SceneManager.LoadScene("Lobby");
    }
}
