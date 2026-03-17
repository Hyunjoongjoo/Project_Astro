using System;
using UnityEngine;

// 로그인 관련 비즈니스 로직
public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private LoginView _loginView;
    [SerializeField] private SignUpView _signUpView;
    [SerializeField] private AcceptUI _acceptUI;

    [SerializeField] private AuthService _authService;
    [SerializeField] private UserDataStore _userDataStore;

    private Action<string> _onLoginSuccess;
    private bool _isProcessing;

    private string _currentUserId;
    private DbModel _currentDb;

    void Awake()
    {
        // AcceptUI로부터 성공 신호를 받으면 ProceedToGame 실행 등록
        if (_acceptUI != null)
        {
            _acceptUI.OnAgreementComplete += () => {
                ProceedToGame(_currentUserId, _currentDb);
            };
        }
    }

    public void Initialize(AuthService authService, UserDataStore userDataStore, Action<string> onLoginSuccess)
    {
        this._authService = authService;
        this._userDataStore = userDataStore;
        this._onLoginSuccess = onLoginSuccess;
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
            Debug.Log("파이어베이스 Auth 로그인 ");
            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);

            Debug.Log("파이어베이스 유저 데이터 조회");
            if (userData == null)
            {
                // 유저 데이터가 없으면 닉네임 생성 유도
                _loginView.ShowNicknameCreationRequired(credentials.email);
                return;
            }

            _currentUserId = user.UserId;
            _currentDb = userData;

            var userHeroData = await _userDataStore.GetUserHeroDataAsync(user.UserId);
            UserDataManager.Instance.SetAllUserData(userData.profile, userData.record, userData.wallet, userHeroData);

            // 약관 동의된 상태인지 체크
            if (!_currentDb.profile.isAgreed)
            {
                _acceptUI.ShowPanel();
                return;
            }

            ProceedToGame(_currentUserId, _currentDb);
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

    public void OnClickGoogleLogIn()
    {
        GoogleHandleLogin();
    }

    private async void GoogleHandleLogin()
    {
        if (_isProcessing) return;

        _isProcessing = true;
        _loginView.SetInteractable(false);

        try
        {
            // 1단계: Firebase Auth 로그인
            var user = await _authService.SignInWithGoogleAsync();
            Debug.Log("파이어베이스 Auth 로그인 ");

            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);

            Debug.Log("파이어베이스 유저 데이터 조회");
            if (userData == null)
            {
                // 유저 데이터가 없으면 닉네임 생성 유도
                _loginView.ShowNicknameCreationRequired(user.UserId);
                _signUpView.SetNicknameOnlyMode(user.Email);
                _signUpView.gameObject.SetActive(true);
                return;
            }

            _currentUserId = user.UserId;
            _currentDb = userData;

            var userHeroData = await _userDataStore.GetUserHeroDataAsync(user.UserId);
            UserDataManager.Instance.SetAllUserData(userData.profile, userData.record, userData.wallet, userHeroData);

            // 약관 동의된 상태인지 체크
            if (!userData.profile.isAgreed)
            {
                _acceptUI.ShowPanel();
                return;
            }
            ProceedToGame(_currentUserId, _currentDb);
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

    private async void ProceedToGame(string userId, DbModel Db)
    {
        try
        {
            await UserDataManager.Instance.SyncHeroDataAsync();

            // 최종 콜백 (메인 씬 전환 등)
            _loginView.ShowWelcomeMessage(Db.profile.nickName);
            _onLoginSuccess?.Invoke(Db.profile.nickName);
            Debug.Log($"[Final] {Db.profile.nickName}님 게임 진입 프로세스 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"진입 프로세스 중 오류: {ex.Message}");
        }
    }
}
