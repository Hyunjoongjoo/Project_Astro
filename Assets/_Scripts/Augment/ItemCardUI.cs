using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCardUI : MonoBehaviour, IAugmentUI
{
    [Header("Main UI")]
    [SerializeField] private Image _skillIconImg;      
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

        // 아이콘 세팅
        if (_skillIconImg != null) _skillIconImg.sprite = data.mainIcon;

        //3. 증강 이름 세팅 (테이블에서 꺼내온 이름)
        if (_titleTxt != null) _titleTxt.text = data.titleName;

        //버튼 클릭 이벤트 세팅
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
