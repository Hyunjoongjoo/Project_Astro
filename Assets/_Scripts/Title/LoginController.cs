using System;
using System.Threading.Tasks;
using UnityEngine;

// 로그인 관련 비즈니스 로직
public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private LoginView _loginView;
    [SerializeField] private SignUpView _signUpView;
    [SerializeField] private AcceptUI _acceptUI;
    [SerializeField] private AnimUI _loginSelectUI;

    [SerializeField] private SignUpController _signUpController;

    private AuthService _authService;
    private UserDataStore _userDataStore;
    private Action<string> _onLoginSuccess;
    private bool _isProcessing;

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

        // 로그인 기반 정보 입력 읽어오기
        var credentials = _loginView.GetCredentials();
        string email = credentials.email;
        string password = credentials.password;

        // 회원가입 기반 로그인하기 정보 입력 읽어오기
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            var signUpData = _signUpView.GetSignUpData();
            email = signUpData.email;
            password = signUpData.password;
        }

        // 입력값 검증
        if (!ValidateInput(email, password))
            return;

        _isProcessing = true;
        _loginView.SetInteractable(false);

        try
        {
            // 1단계: Firebase Auth 로그인
            var user = await _authService.LoginAsync(email, password);
            Debug.Log("파이어베이스 Auth 로그인 ");
            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);

            Debug.Log("파이어베이스 유저 데이터 조회");
            if (userData == null)
            {
                // 유저 데이터가 없으면 닉네임 생성 유도하여 DB 최신화
                _loginView.ShowNicknameCreationRequired(email);
                return;
            }

            // 약관 동의된 상태인지 체크
            if (!userData.profile.isAgreed)
            {
                _acceptUI.ShowPanel(async () =>
                {
                    await FinalizeLogin(user.UserId);
                });
                return;
            }

            await FinalizeLogin(user.UserId);
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

            if (_loginSelectUI.gameObject.activeSelf)
            {
                _loginSelectUI.DeActivate();
            }

            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);

            Debug.Log("파이어베이스 유저 데이터 조회");
            if (userData == null)
            {
                // 약관 동의 먼저띄우고 난 뒤에
                _acceptUI.ShowPanel(() =>
                {
                    // 약관 동의 성공 시에만 닉네임 설정 창 오픈
                    _loginView.ShowNicknameCreationRequired(user.UserId);
                    _signUpView.SetNicknameOnlyMode(user.Email);
                    _signUpView.gameObject.SetActive(true);

                    _signUpController.Initialize(_authService, _userDataStore, async (data) =>
                    {
                        await FinalizeLogin(user.UserId);
                    });
                });
                return;
            }

            // 약관 동의된 상태인지 체크
            if (!userData.profile.isAgreed)
            {
                _acceptUI.ShowPanel(async () =>
                {
                    await FinalizeLogin(user.UserId);
                });
                return;
            }
            await FinalizeLogin(user.UserId);
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

    private async Task FinalizeLogin(string userId)
    {
        try
        {
            var userData = await _userDataStore.GetUserDataAsync(userId);
            var userHeroData = await _userDataStore.GetUserHeroDataAsync(userId);

            // 데이터 캐싱
            UserDataManager.Instance.SetAllUserData(userData.profile, userData.record, userData.wallet, userHeroData);
            await UserDataManager.Instance.SyncHeroDataAsync();

            // 씬 전환 콜백 호출
            _loginView.ShowWelcomeMessage(userData.profile.nickName);
            _onLoginSuccess?.Invoke(userData.profile.nickName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"진입 프로세스 중 오류: {ex.Message}");
        }
    }
}
