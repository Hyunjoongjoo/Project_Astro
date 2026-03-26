using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpView : MonoBehaviour
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
    private string _titleText = "회원가입";
    private string _infoText = "게스트 계정의 경우 분실 시 복구가 불가능합니다.\r\n로비 -> 설정에서 구글 연동을 진행해주세요.";


    public TMP_InputField NicknameInput => _nicknameInput;

    private void Awake()
    {
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
        ShowSuccess($"{nickname}님, 회원가입이 완료되었습니다!");
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
        _title.text = _titleText;
        _info.text = _infoText;
    }
}
