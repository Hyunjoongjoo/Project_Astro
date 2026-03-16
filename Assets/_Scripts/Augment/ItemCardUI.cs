using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCardUI : MonoBehaviour, IAugmentUI
{
    [Header("Main UI")]
    [SerializeField] private Image _ItemIconImg;
    [SerializeField] private TMP_Text _titleTxt;

    [Header("Item Info UI")]
    [SerializeField] private TMP_Text _itemDescription;

    [Header("Button")]
    [SerializeField] private Button _selectBtn;
    [SerializeField] private GameObject _highlightObj;

    private AugmentData _data;

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
    }
}
