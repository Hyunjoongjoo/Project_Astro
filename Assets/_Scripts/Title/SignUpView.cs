using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpView : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;
    [SerializeField] private TMP_InputField _nicknameInput;

    [Header("Buttons")]
    [SerializeField] private Button _checkNicknameButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _cancelButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI _resultText;

    private void Start()
    {
        HideResultPanel();
    }

    public SignUpData GetSignUpData()
    {
        return new SignUpData
        {
            email = _emailInput.text,
            password = _passwordInput.text,
            passwordConfirm = _passwordConfirmInput.text,
            nickname = _nicknameInput.text
        };
    }

    public string GetNickname()
    {
        return _nicknameInput.text;
    }

    public void SetInteractable(bool interactable)
    {
        _emailInput.interactable = interactable;
        _passwordInput.interactable = interactable;
        _passwordConfirmInput.interactable = interactable;
        _nicknameInput.interactable = interactable;
        _checkNicknameButton.interactable = interactable;
        _signUpButton.interactable = interactable;
        _cancelButton.interactable = interactable;
    }

    public void SetCheckButtonInteractable(bool interactable)
    {
        _checkNicknameButton.interactable = interactable;
    }

    public void ShowError(string message)
    {
        _resultText.text = message;
        _resultText.color = Color.red;
        _resultText.gameObject.SetActive(true);
    }

    public void ShowSuccess(string message)
    {
        _resultText.text = message;
        _resultText.color = Color.green;
        _resultText.gameObject.SetActive(true);
    }

    public void ShowSignUpSuccess(string nickname)
    {
        ShowSuccess($"{nickname}님, 회원가입이 완료되었습니다!");
    }

    public void ClearInputs()
    {
        _emailInput.text = string.Empty;
        _passwordInput.text = string.Empty;
        _passwordConfirmInput.text = string.Empty;
        _nicknameInput.text = string.Empty;
    }

    public void HideResultPanel()
    {
        _resultText.gameObject.SetActive(false);
    }

    // 닉네임만 입력하는 모드
    public void SetNicknameOnlyMode(string email)
    {
        _emailInput.text = email;
        _emailInput.interactable = false;
        _passwordInput.interactable = false;
        _passwordConfirmInput.interactable = false;
    }
}
