using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// 로그인 관련 비즈니스 로직
public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private SignUpView _signUpView;
    [SerializeField] private AcceptUI _acceptUI;
    [SerializeField] private AnimUI _loginSelectUI;

    [SerializeField] private SignUpController _signUpController;

    [Header("Screen Button")]
    [SerializeField] private GameObject _screenBtn;

    private AuthService _authService;
    private UserDataStore _userDataStore;
    private Action<string> _onLoginSuccess;
    private bool _isProcessing;
    private bool _isFirebaseReady = false;
    private bool _inputEnabled = false;

    IEnumerator Start()
    {
        // 최소 2~3프레임은 입력 무시
        yield return null;
        yield return null;
        _inputEnabled = true;
    }

    public void Initialize(AuthService authService, UserDataStore userDataStore, Action<string> onLoginSuccess)
    {
        this._authService = authService;
        this._userDataStore = userDataStore;
        this._onLoginSuccess = onLoginSuccess;

        _isFirebaseReady = true;
    }

    public async void OnClickPressScreen()
    {
        if (_inputEnabled == false) return;
        if (_isFirebaseReady == false) return;
        if (_isProcessing) return;
        _isProcessing = true;

        bool isAutoLoginEnabled = PlayerPrefs.GetInt("IsAutoLogin", 0) == 1;

        if (isAutoLoginEnabled && PlayerPrefs.HasKey("Guest_Email"))
        {
            string email = PlayerPrefs.GetString("Guest_Email");
            string pw = PlayerPrefs.GetString("Guest_PW");

            try
            {
                var user = await _authService.LoginAsync(email, pw);
                await FinalizeLogin(user.UserId);
                _isProcessing = false;
                return;
            }
            catch (Exception ex)
            {

                Debug.LogWarning($"[AutoLogin Fail] 서버에 계정이 없거나 오류 발생: {ex.Message}");

                LoginException(ex.Message);
            }
        }

        _isProcessing = false;
        if (!_loginSelectUI.IsOpened)
        {
            _loginSelectUI.Open();
        }
        _screenBtn.SetActive(false);
    }

    public void OnClickGuestLogin()
    {
        if(!PlayerPrefs.HasKey("Guest_Email"))
        {
            _signUpView.SetMode(false);
            _signUpView.Open();

            _signUpController.Initialize(_authService, _userDataStore, async (data) =>
            {
                var user = _authService.CurrentUser;
                if (user != null)
                {
                    await FinalizeLogin(user.UserId);
                }
            });
        }
        else
        {
            string email = PlayerPrefs.GetString("Guest_Email");
            string password = PlayerPrefs.GetString("Guest_PW");

            HandleLogin(email, password);
        }
    }

    private async void HandleLogin(string email, string password)
    {
        if (_isProcessing) return;

        _isProcessing = true;

        try
        {
            // 1단계: Firebase Auth 로그인
            var user = await _authService.LoginAsync(email, password);
            Debug.Log("파이어베이스 Auth 로그인 ");

            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);
            Debug.Log("파이어베이스 유저 데이터 조회");

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
            LoginException(ex.Message);
        }
        finally
        {
            _isProcessing = false;
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

        try
        {
            // 1단계: Firebase Auth 로그인
            var user = await _authService.SignInWithGoogleAsync();
            Debug.Log("파이어베이스 Auth 로그인 ");

            if (user == null)
            {
                Debug.LogWarning("[Login] 구글 로그인 시도가 취소되었거나 실패했습니다.");
                return;
            }

            if (_loginSelectUI.gameObject.activeSelf)
            {
                _loginSelectUI.DeActivate();
            }

            // 2단계: Firestore에서 유저 데이터 조회
            var userData = await _userDataStore.GetUserDataAsync(user.UserId);

            Debug.Log("파이어베이스 유저 데이터 조회");
            if (userData == null)
            {
                // 약관 동의 성공 시에만 닉네임 설정 창 오픈
                _signUpView.Open();
                _signUpView.SetMode(true);

                _signUpController.Initialize(_authService, _userDataStore, async (data) =>
                {
                    await FinalizeLogin(user.UserId);
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
            LoginException(ex.Message);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task FinalizeLogin(string userId)
    {
        try
        {
            // 세션 난수 발행 및 로컬 저장
            string myLocalSessionId = _authService.MyLocalSessionId;

            if (string.IsNullOrEmpty(myLocalSessionId))
            {
                // 처음 로그인할 때만 난수 발행
                myLocalSessionId = Guid.NewGuid().ToString();
                _authService.MyLocalSessionId = myLocalSessionId;
            }

            // 난수 DB에 저장
            await _userDataStore.UpdateSessionIdAsync(userId, myLocalSessionId);

            var userData = await _userDataStore.GetUserDataAsync(userId);
            var userHeroData = await _userDataStore.GetUserHeroDataAsync(userId);

            // 데이터 캐싱
            UserDataManager.Instance.SetAllUserData(userData.profile, userData.record, userData.wallet, userHeroData);
            await UserDataManager.Instance.SyncHeroDataAsync();

            PlayerPrefs.SetInt("IsAutoLogin", 1);
            PlayerPrefs.Save();

            // 씬 전환 콜백 호출
            _onLoginSuccess?.Invoke(userData.profile.nickName);
        }
        catch (Exception ex)
        {
            LoginException(ex.Message);
        }
    }

    private void LoginException(string ex)
    {
        Debug.LogError($"[Login Failed] 사유: {ex}");

        // 에러 = 이 계정 정보가 쓸모없다는 뜻이므로 로컬 데이터 정리
        PlayerPrefs.DeleteKey("Guest_Email");
        PlayerPrefs.DeleteKey("Guest_PW");
        PlayerPrefs.SetInt("IsAutoLogin", 0);
        PlayerPrefs.Save();

        _loginSelectUI.Open();
    }
}
