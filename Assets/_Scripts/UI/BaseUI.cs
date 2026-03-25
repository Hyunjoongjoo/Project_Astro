using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BaseUI : MonoBehaviour, IPointerEnterHandler
{
    [Header("기본 세팅")]
    [SerializeField] protected string _uiName;
    public string UIName => _uiName;

    [Header("애니메이션 설정")]
    [SerializeField] protected float _fadeDuration = 0.25f;
    [SerializeField] protected float _scaleDuration = 0.25f;
    protected CanvasGroup _canvasGroup;
    private bool _isClosing = false;
    // 현재 UI가 열려 있는지 (토글용 확인 플래그)
    public bool IsOpened => gameObject.activeSelf && !_isClosing;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }
   
    //열릴 때
    public virtual void Open()
    {
        _isClosing = false;

        _canvasGroup.DOKill();
        transform.DOKill();

        gameObject.SetActive(true);

        _canvasGroup.alpha = 0;
        transform.localScale = Vector3.one * 0.9f;

        _canvasGroup.DOFade(1, _fadeDuration).SetEase(Ease.OutCubic);
        transform.DOScale(1, _scaleDuration).SetEase(Ease.OutBack);

        AudioManager.Instance.PlayUISfx(UISfxList.BtnOpen);
    }
    //비활성화 닫기 애니메이션용
    public virtual void DeActivate()
    {
        if (_isClosing) return;
        
        _isClosing = true;

        _canvasGroup.DOFade(0, _fadeDuration).SetEase(Ease.InCubic);
        transform.DOScale(0.9f, _scaleDuration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            gameObject.SetActive(false);
            _isClosing = false;
        });

        AudioManager.Instance.PlayUISfx(UISfxList.BtnClose);
    }

    //팝업창용 닫힐 때
    public virtual void Close()
    {
        if (_isClosing) return;
        _isClosing = true;

        _canvasGroup.DOFade(0, _fadeDuration).SetEase(Ease.InCubic);
        transform.DOScale(0.9f, _scaleDuration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            Destroy(gameObject);
        });

        AudioManager.Instance.PlayUISfx(UISfxList.BtnClose);
    }

    //뒤로가기 버튼용
    public virtual void OnBackButtonPressed()
    {
        UIManager.Instance.CloseTopPopup();
    }

    // 토글 로직
    public virtual void Toggle()
    {
        if (IsOpened)
        {
            //열려 있으면 닫기
            DeActivate();
        }
        else
        {
            // 닫혀 있으면 열기
            Open();
        }
    }
    //오픈 유지(갱신x로직)
    public virtual void MaintainOpen()
    {
        if (IsOpened) return;

        Open();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }
}
