using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AcceptUI : MonoBehaviour
{
    [Header("UI Panels & Text")]
    [SerializeField] private GameObject _acceptPanel;
    [SerializeField] private TextMeshProUGUI _noticeText;
    [SerializeField] private AnimUI _loginSelectPanel;

    [Header("Toggles")]
    [SerializeField] private Toggle _allToggle;
    // 0: 이용약관, 1: 개인정보 추가되면 적어줘요!
    [SerializeField] private Toggle[] _subToggles;

    [Header("Buttons")]
    [SerializeField] private Button _startBtn;

    [Header("Settings")]
    [SerializeField] private string[] _linkURL;
    private const string DEFAULT_MSG = "게임 이용을 위해 필수 약관 동의가 필요합니다.";
    private bool _isIgnoreEvent = false;

    private Action _currentSuccessCallback;

    private void Start()
    {
        _allToggle.onValueChanged.AddListener(OnAllToggleChanged);

        foreach (var toggle in _subToggles)
        {
            toggle.onValueChanged.AddListener((_) => OnSubToggleChanged());
        }
        ResetUI();
    }

    // 조건 처리 시 완료 안됐을때 동의창 띄울 메서드
    public void ShowPanel(Action onSuccess)
    {
        ResetUI();
        _currentSuccessCallback = onSuccess;
        _acceptPanel.SetActive(true);
    }

    #region 체크박스 로직
    private void OnAllToggleChanged(bool check)
    {
        if (_isIgnoreEvent) return;

        _isIgnoreEvent = true;
        foreach (var toggle in _subToggles)
        {
            toggle.isOn = check;
        }
        _isIgnoreEvent = false;
    }

    private void OnSubToggleChanged()
    {
        if (_isIgnoreEvent) return;

        _isIgnoreEvent = true;
        // 하위 토글이 모두 체크되어 있다면 상위 토글도 체크
        _allToggle.isOn = _subToggles.All(t => t.isOn);
        _isIgnoreEvent = false;
    }
    #endregion

    #region 버튼 액션
    public void OnClickStart()
    {
        // 필수 약관 처리
        bool isEssentialAgreed = _subToggles[0].isOn && _subToggles[1].isOn;

        if (isEssentialAgreed)
        {
            _startBtn.interactable = false;
            Debug.Log("필수 약관 동의 완료. 로그인 플로우 진행");

            _startBtn.interactable = true;
            _acceptPanel.SetActive(false);

            _currentSuccessCallback?.Invoke();
            _currentSuccessCallback = null;            
        }
        else
        {
            ShowText("[필수 항목]을 전부 확인해주세요.");
            _startBtn.interactable = true;
        }
    }

    public void OnClickCancel()
    {
        // 구글 버튼을 통한 패널일 경우
        if (AuthService.Instance.CurrentUser != null)
        {
            AuthService.Instance.Logout();
            _loginSelectPanel.Open();
        }
        ResetUI();
        _acceptPanel.SetActive(false);
    }

    public void OnClickURL(int value)
    {
        if (value < _linkURL.Length)
        {
            Application.OpenURL(_linkURL[value]);
        }
    }
    #endregion

    #region 메시지 및 초기화
    public void ShowText(string message, float delay = 2f)
    {
        StopAllCoroutines();
        StartCoroutine(Co_ShowMessage(message, delay));
    }

    IEnumerator Co_ShowMessage(string message, float delay)
    {
        _noticeText.text = message;
        _noticeText.color = Color.red;
        yield return new WaitForSeconds(delay);
        ResetMessage();
    }

    public void ResetMessage()
    {
        _noticeText.text = DEFAULT_MSG;
        _noticeText.color = Color.white;
    }

    public void ResetUI()
    {
        _isIgnoreEvent = true;
        _allToggle.isOn = false;
        foreach (var t in _subToggles) t.isOn = false;
        _isIgnoreEvent = false;

        ResetMessage();
    }
    #endregion
}