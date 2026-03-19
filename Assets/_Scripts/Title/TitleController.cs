using Firebase;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private LoginController _loginController;
    [SerializeField] private SignUpController _signUpController;

    private UserDataStore _userDataStore;
    private AuthService _authService;

    void Start()
    {
        InitializeFirebase();
        GameManager.Instance.SetSceneState(SceneState.Title);
    }

    private void InitializeFirebase()
    {
        // 이미 Firebase가 준비되어 있고 AuthService가 살아있다면 굳이 다시 체크할 필요 없습니다.
        if (AuthService.Instance != null && AuthService.Instance.Auth != null)
        {
            Debug.Log("[Title] 이미 Firebase가 초기화되어 있습니다. 참조만 재연결합니다.");
            _authService = AuthService.Instance;
            _userDataStore = UserDataStore.Instance;

            SetupControllers();
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().
            ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"Firebase Depenedency Error : {task.Result}");
                    return;
                }
                _authService = AuthService.Instance;
                _userDataStore = UserDataStore.Instance;

                if (_authService == null) _authService = FindFirstObjectByType<AuthService>();
                if (_userDataStore == null) _userDataStore = FindFirstObjectByType<UserDataStore>();

                if (_authService != null)
                {
                    _authService.Initialize();
                    _userDataStore.Initialize();

                    SetupControllers();

                    Debug.Log("[Title] Firebase 및 서비스 주입 완료");
                }
                else
                {
                    Debug.LogError("[Title] AuthService를 찾을 수 없음");
                }
            });
    }

    private void SetupControllers()
    {
        // 공통 초기화 로직
        _loginController.Initialize(_authService, _userDataStore, OnLoginComplete);
        _signUpController.Initialize(_authService, _userDataStore, (data) =>
        {
            if (data.isGoogle)
            {
                _loginController.OnClickGoogleLogIn();
            }
            else
            {
                _loginController.OnClickLogin();
            }
        });
    }

    private void OnLoginComplete(string nickname)
    {
        Debug.Log("[TitleController] 모든 로그인 로직 완료");
        GameManager.Instance.SetSceneState(SceneState.Lobby);
        SceneManager.LoadScene("Lobby");
    }
}
