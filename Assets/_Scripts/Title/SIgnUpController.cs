using System;
using UnityEngine;

// 회원가입 비즈니스 로직
public class SignUpController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private SignUpView _signUpView;
    [SerializeField] private AcceptUI _acceptUI;
    [SerializeField] private AnimUI _loginSelectPanel;

    [Header("Validation")]
    //[SerializeField] private int _minPasswordLength = 8;
    //[SerializeField] private int _maxPasswordLength = 18;
    [SerializeField] private int _minNicknameLength = 2;
    [SerializeField] private int _maxNicknameLength = 8;

    private Action<SignUpData> _onSignUpComplete;

    private AuthService _authService;
    private UserDataStore _userDataStore;
    private bool _isProcessing;
    private bool _isNicknameVerified;

    public void Initialize(AuthService authService, UserDataStore userDataStore, Action<SignUpData> onSignUpComplete)
    {
        this._authService = authService;
        this._userDataStore = userDataStore;
        this._onSignUpComplete = onSignUpComplete;
    }
    private void Start()
    {
        // 닉네임 필드 수정되면 바로 이벤트
        _signUpView.NicknameInput.onValueChanged.AddListener(_ => OnNicknameChanged());
    }

    private void OnNicknameChanged()
    {
        if (_isNicknameVerified)
        {
            _isNicknameVerified = false;
            _signUpView.ShowError("닉네임 중복 확인을 다시 해주세요.");
        }
    }

    public void OnClickCancel()
    {   
        if (AuthService.Instance.CurrentUser != null)
        {
            AuthService.Instance.Logout();
            _loginSelectPanel.Open();
        }
        _signUpView.gameObject.SetActive(false);
    }

    public void OnClickCheckNickname()
    {
        HandleCheckNickname();
    }

    public void OnClickSignUp()
    {
        if(!_isNicknameVerified)
        {
            _signUpView.ShowError("닉네임 중복 체크를 해주세요.");
            return;
        }
        _acceptUI.ShowPanel(() =>
        {
            HandleSignUp();
        });
    }

    private async void HandleCheckNickname()
    {
        if (_isProcessing) return;

        string nickname = _signUpView.GetNickname();

        // 닉네임 형식 검증
        var validationError = ValidateNickname(nickname);
        if (validationError != null)
        {
            _signUpView.ShowError(validationError);
            _isNicknameVerified = false;
            return;
        }

        _isProcessing = true;
        _signUpView.SetCheckButtonInteractable(false);

        try
        {
            bool isDuplicate = await _userDataStore.IsNicknameDuplicateAsync(nickname);

            if (isDuplicate)
            {
                _signUpView.ShowError("already used nickname.");
                _isNicknameVerified = false;
            }
            else
            {
                _signUpView.ShowSuccess("available nickname.");
                _isNicknameVerified = true;
            }
        }
        catch (Exception ex)
        {
            _signUpView.ShowError("Nickname check error");
            Debug.LogError($"[SignUp] Nickname check error: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            _signUpView.SetCheckButtonInteractable(true);
        }
    }

    private async void HandleSignUp()
    {
        if (_isProcessing) return;

        var input = _signUpView.GetSignUpData();

        // 입력값 검증
        var validationError = ValidateSignUpInput(input);
        if (validationError != null)
        {
            _signUpView.ShowError(validationError);
            return;
        }

        _isProcessing = true;
        _signUpView.SetInteractable(false);

        try
        {
            if(input.isGoogle)
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    await _authService.UpdateProfileAsync(currentUser, input.nickname);
                    if (UserDataManager.Instance.IsLink)
                    {
                        await _authService.UpdateProfileAsync(currentUser, input.nickname);
                        string guestGuid = PlayerPrefs.GetString("Guest_Email").Split('@')[0];
                        bool isSuccess = await UserDataManager.Instance.LinkDataAsync(guestGuid, currentUser.UserId);

                        if (!isSuccess) throw new Exception("Data Migration Failed");
                    }
                    else
                    {
                        // 기존 구글 회원가입 플로우
                        Debug.Log("[New] 구글 신규 유저 데이터를 생성합니다.");
                        await _userDataStore.CreateUserDataAsync(currentUser.UserId, input.nickname);
                    }
                }
            }
            else
            {
                // GUID 생성
                string guestGuid = Guid.NewGuid().ToString("N");
                string guestEmail = $"{guestGuid}@guest.com";
                string guestPassword = guestGuid;

                // Firebase Auth 계정 생성
                var newUser = await _authService.SignUpAsync(guestEmail, guestPassword);
                
                // 기기에 GUID 저장
                PlayerPrefs.SetString("Guest_Email", guestEmail);
                PlayerPrefs.SetString("Guest_PW", guestPassword);
                PlayerPrefs.Save();
                
                // 사용자 프로필 업데이트 (닉네임)
                await _authService.UpdateProfileAsync(newUser, input.nickname);

                // Firestore에 유저 데이터 생성
                await _userDataStore.CreateUserDataAsync(newUser.UserId, input.nickname);
            }

            // 4단계: 성공 메시지 표시
            _signUpView.ShowSignUpSuccess(input.nickname);

            // 4.5단계 : 글 읽을 시간 주기
            await System.Threading.Tasks.Task.Delay(1000);

            // 5단계 : 바로 로그인으로 이어주기
            _onSignUpComplete?.Invoke(input);
        }
        catch (Firebase.FirebaseException firebaseEx)
        {
            _signUpView.ShowError(GetFirebaseErrorMessage(firebaseEx));
        }
        catch (Exception ex)
        {
            _signUpView.ShowError("Sign Up Error.");
            Debug.LogError($"[SignUp] Error: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            _signUpView.SetInteractable(true);
        }
    }

    private string ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return "please input nickname.";

        if (nickname.Length < _minNicknameLength || nickname.Length > _maxNicknameLength)
            return $"nickname is {_minNicknameLength}~{_maxNicknameLength} characters.";

        try
        {
            InputValidator.ValidateOrThrow(nickname);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }

    private string ValidateSignUpInput(SignUpData input)
    {
        // 닉네임 검증
        var nicknameError = ValidateNickname(input.nickname);
        if (nicknameError != null)
            return nicknameError;

        if (!_isNicknameVerified)
            return "닉네임 중복 확인을 해주세요.";

        return null;
    }

    private string GetFirebaseErrorMessage(Firebase.FirebaseException ex)
    {
        return ex.ErrorCode switch
        {
            (int)Firebase.Auth.AuthError.EmailAlreadyInUse => "이미 사용 중인 이메일입니다.",
            (int)Firebase.Auth.AuthError.InvalidEmail => "유효하지 않은 이메일 형식입니다.",
            (int)Firebase.Auth.AuthError.WeakPassword => "비밀번호가 너무 약합니다.",
            _ => "회원가입에 실패했습니다."
        };
    }
}

// 회원가입 입력 데이터
public struct SignUpData
{
    public bool isGoogle;
    public string nickname;
}
