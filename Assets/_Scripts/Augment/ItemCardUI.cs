using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemCardUI : MonoBehaviour, IAugmentUI, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Main UI")]
    [SerializeField] private Image _ItemIconImg;
    [SerializeField] private TMP_Text _titleTxt;

    [Header("Item Info UI")]
    [SerializeField] private TMP_Text _itemDescription;

    [Header("Button")]
    [SerializeField] private Button _selectBtn;
    [SerializeField] private GameObject _highlightObj;

    [Header("Animation (DOTween)")]
    [SerializeField] private RectTransform _visualRoot;
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _animDuration = 0.2f;

    private AugmentData _data;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _visualRoot.DOKill();

        _visualRoot.DOScale(_hoverScale, _animDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _visualRoot.DOKill();
        // 원래 크기로 복구
        _visualRoot.DOScale(1f, _animDuration).SetEase(Ease.OutCubic);
    }

    public void Setup(AugmentData data)
    {
        _data = data;
        _selectBtn.interactable = true;

        if (_ItemIconImg != null && data.mainIcon != null)
        {
            _ItemIconImg.sprite = data.mainIcon;
        }

        if (_titleTxt != null) _titleTxt.text = data.titleName;
        if (_itemDescription != null) _itemDescription.text = data.description;

        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(OnSelectClicked);
    }

    public void ToggleHighlight(bool isOn)
    {
        if (_highlightObj != null) _highlightObj.SetActive(isOn);
    }

    private void OnSelectClicked()
    {
        GetComponentInParent<AugmentWindowUI>().OnCardSelected(this, _data);
        _visualRoot.DOPunchScale(new Vector3(-0.05f, -0.05f, 0), 0.1f);
    }
}
