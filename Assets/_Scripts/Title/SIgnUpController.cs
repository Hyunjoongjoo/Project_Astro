using Firebase.Firestore;
using System;
using UnityEngine;

// 회원가입 비즈니스 로직
public class SignUpController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private SignUpView _signUpView;

    [Header("Validation")]
    [SerializeField] private int _minPasswordLength = 8;
    [SerializeField] private int _maxPasswordLength = 18;
    [SerializeField] private int _minNicknameLength = 2;
    [SerializeField] private int _maxNicknameLength = 8;

    private AuthService _authService;
    private UserDataStore _userDataStore;
    private bool _isProcessing;
    private bool _isNicknameVerified;

    public void Initialize(AuthService authService, UserDataStore userDataStore)
    {
        _authService = authService;
        _userDataStore = userDataStore;
    }

    public void OnClickActivePanel(bool active)
    {
        _signUpView.gameObject.SetActive(active);
    }

    public void OnClickCheckNickname()
    {
        HandleCheckNickname();
    }

    public void OnClickSignUp()
    {
        HandleSignUp();
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
            // 1단계: Firebase Auth 계정 생성
            var user = await _authService.SignUpAsync(input.email, input.password);

            // 2단계: 사용자 프로필 업데이트 (닉네임)
            await _authService.UpdateProfileAsync(user, input.nickname);

            // 3단계: Firestore에 유저 데이터 생성
            await _userDataStore.CreateUserDataAsync(user.UserId, input.nickname);

            // 4단계: 성공 메시지 표시
            _signUpView.ShowSignUpSuccess(input.nickname);
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

        return null;
    }

    private string ValidateSignUpInput(SignUpData input)
    {
        // 이메일 검증
        if (string.IsNullOrWhiteSpace(input.email))
            return "이메일을 입력해주세요.";

        if (!input.email.Contains("@"))
            return "올바른 이메일 형식이 아닙니다.";

        // 비밀번호 검증
        if (string.IsNullOrWhiteSpace(input.password))
            return "비밀번호를 입력해주세요.";

        if (input.password.Length < _minPasswordLength || input.password.Length > _maxPasswordLength)
            return $"비밀번호는 {_minPasswordLength}~{_maxPasswordLength}자여야 합니다.";

        if (input.password != input.passwordConfirm)
            return "비밀번호가 일치하지 않습니다.";

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
    public string email;
    public string password;
    public string passwordConfirm;
    public string nickname;
}
