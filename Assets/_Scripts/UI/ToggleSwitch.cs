using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToggleSwitch : MonoBehaviour
{
    [Header("시각적 요소")]
    [SerializeField] private Image _backgroundImg;
    [SerializeField] private Image _handleImg;

    [Header("애니메이션 설정")]
    [SerializeField] private float _switchDuration = 0.2f;
    [SerializeField] private Sprite _bgOnSprite;
    [SerializeField] private Sprite _bgOffSprite;
    [SerializeField] private Sprite _handleOnSprite;
    [SerializeField] private Sprite _handleOffSprite;

    private Toggle _toggle;
    private RectTransform _handleRect;
    private float _handleOnPos; // On 좌표
    private float _handleOffPos; //off 좌표
    private bool _isInitialized = false;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _handleRect = _handleImg.rectTransform;  
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (_isInitialized) return;

        if (_toggle == null) _toggle = GetComponent<Toggle>();
        if (_handleImg != null && _handleRect == null) _handleRect = _handleImg.rectTransform;

        //핸들 시작위치를 off 위치로 설정
        _handleOffPos = _handleRect.anchoredPosition.x;

        // 배경 너비만큼 오른쪽으로 이동한 곳을 On 위치로 계산
        float bgWidth = _backgroundImg.rectTransform.rect.width;
        float handleWidth = _handleRect.rect.width;
        _handleOnPos = bgWidth - handleWidth;

        // 토글 상태 변화 리스너 연결
        _toggle.onValueChanged.RemoveAllListeners();
        _toggle.onValueChanged.AddListener((isOn) => SetState(isOn, true));

        _isInitialized = true;

        RefreshUI();
    }


    // 상태에 따라 시각적 요소 변경 (animate가 true면 부드럽게)
    private void SetState(bool isOn, bool animate)
    {
        float targetX = isOn ? _handleOnPos : _handleOffPos;

        _backgroundImg.sprite = isOn ? _bgOnSprite : _bgOffSprite;
        _handleImg.sprite = isOn ? _handleOnSprite : _handleOffSprite;

        if (animate)
        {
            // DoTween으로 부드럽게 이동
            _handleRect.DOAnchorPosX(targetX, _switchDuration).SetEase(Ease.OutCubic);
        }
        else
        {
            // 즉시 변경
            _handleRect.anchoredPosition = new Vector2(targetX, _handleRect.anchoredPosition.y);
        }
    }

    public void RefreshUI()
    {
        Init();

        // Toggle 컴포넌트의 현재 isOn 상태에 맞춰 UI를 즉시 갱신
        if (_toggle != null)
        {
            SetState(_toggle.isOn, false);
        }
    }
}
