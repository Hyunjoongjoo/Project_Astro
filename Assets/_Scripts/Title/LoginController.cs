using System;
using UnityEngine;

// 로그인 관련 비즈니스 로직
public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private LoginView _loginView;

    private AuthService _authService;
    private UserDataStore _userDataStore;
    private Action<string> _onLoginSuccess;
    private bool _isProcessing;

    public void Initialize(AuthService authService, UserDataStore userDataStore, Action<string> onLoginSuccess)
    {
        _authService = authService;
        _userDataStore = userDataStore;
        _onLoginSuccess = onLoginSuccess;
    }

    public void OnClickLogin()
    {
        HandleLogin();
    }

    private async void HandleLogin()
    {
        if (_isProcessing) return;

        var credentials = _loginView.GetCredentials();

        // 입력값 검증
        if (!ValidateInput(credentials.email, credentials.password))
            return;

        _isProcessing = true;
        _loginView.SetInteractable(false);

        try
        {
            // 1단계: Firebase Auth 로그인
            var user = await _authService.LoginAsync(credentials.email, credentials.password);

            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);
            var userHeroData = await _userDataStore.GetUserHeroDataAsync(user.UserId);
            var userRecordData = await _userDataStore.GetUserRecordDataAsync(user.UserId);
            var userWalletData = await _userDataStore.GetUserWalletDataAsync(user.UserId);

            if (userData == null)
            {
                // 유저 데이터가 없으면 닉네임 생성 유도
                _loginView.ShowNicknameCreationRequired(credentials.email);
                return;
            }

            // 3단계: 로그인 성공
            _loginView.ShowWelcomeMessage(userData.nickName);
            _onLoginSuccess?.Invoke(userData.nickName);

            // 4단계: DB 데이터 캐싱
            UserDataManager.Instance.SetAllUserData(userData, userRecordData, userWalletData, userHeroData);
        }
        catch (Exception ex)
        {
            _loginView.ShowError(GetErrorMessage(ex));
        }
        finally
        {
            _isProcessing = false;
            _loginView.SetInteractable(true);
        }
    }
    private bool ValidateInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _loginView.ShowError("아이디와 비밀번호를 입력해주세요.");
            return false;
        }
        return true;
    }

    private string GetErrorMessage(Exception ex)
    {
        // Firebase 예외 처리
        return ex switch
        {
            Firebase.FirebaseException firebaseEx => firebaseEx.ErrorCode switch
            {
                (int)Firebase.Auth.AuthError.InvalidEmail => "잘못된 이메일 형식입니다.",
                (int)Firebase.Auth.AuthError.WrongPassword => "아이디 또는 비밀번호가 틀렸습니다.",
                (int)Firebase.Auth.AuthError.UserNotFound => "아이디 또는 비밀번호가 틀렸습니다.",
                _ => "로그인에 실패했습니다."
            },
            _ => "로그인 중 오류가 발생했습니다."
        };
    }
}
