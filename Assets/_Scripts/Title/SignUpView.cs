using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpView : BaseUI
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField _nicknameInput;

    [Header("Buttons")]
    [SerializeField] private Button _checkNicknameButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _cancelButton;

    [Header("text")]
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _info;
    [SerializeField] private TextMeshProUGUI _resultText;

    private bool _isGoogleSign = false;


    public TMP_InputField NicknameInput => _nicknameInput;

    protected override void Awake()
    {
        base.Awake();
        TextSetting();
    }

    private void Start()
    {
        HideResultPanel();
    }

    public SignUpData GetSignUpData()
    {
        return new SignUpData
        {
            isGoogle = _isGoogleSign,
            nickname = _nicknameInput.text
        };
    }

    public string GetNickname()
    {
        return _nicknameInput.text;
    }

    public void SetInteractable(bool interactable)
    {
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
        string welcomeText = TableManager.Instance.GetString("signup_welcome");
        ShowSuccess($"{nickname}{welcomeText}");
    }

    public void ClearInputs()
    {
        _nicknameInput.text = string.Empty;
    }

    public void HideResultPanel()
    {
        _resultText.gameObject.SetActive(false);
    }

    public void SetMode(bool mode)
    {
        _isGoogleSign = mode;
    }

    // 스트링 테이블 기반으로 차후 변경 예정
    public void TextSetting()
    {
        if (_isGoogleSign)
        {
            _title.text = TableManager.Instance.GetString("signup_create_google_account");
        }
        else
        {
            _title.text = TableManager.Instance.GetString("title_guest_create");
        }
        _info.text = TableManager.Instance.GetString("signup_check_link_google");
    }
}
