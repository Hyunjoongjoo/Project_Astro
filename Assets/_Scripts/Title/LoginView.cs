using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 UI 담당
public class LoginView : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI _resultText;

    void Start()
    {
        _resultText.gameObject.SetActive(false);
    }

    public (string email, string password) GetCredentials()
    {
        return (_emailInput.text, _passwordInput.text);
    }

    public void SetInteractable(bool interactable)
    {
        if (this == null || gameObject == null) return;
        _emailInput.interactable = interactable;
        _passwordInput.interactable = interactable;
        _loginButton.interactable = interactable;
        _signUpButton.interactable = interactable;
    }

    public void ShowWelcomeMessage(string nickname)
    {
        _resultText.text = $"{nickname}님, 환영합니다.";
        _resultText.gameObject.SetActive(true);
    }

    public void ShowError(string message)
    {
        _resultText.text = message;
        _resultText.gameObject.SetActive(true);
    }

    public void ShowNicknameCreationRequired(string email)
    {
        ShowError("유저 데이터가 존재하지 않습니다.\n새로운 닉네임을 입력해주세요.");
    }

    public void ClearInputs()
    {
        _emailInput.text = string.Empty;
        _passwordInput.text = string.Empty;
    }
}
