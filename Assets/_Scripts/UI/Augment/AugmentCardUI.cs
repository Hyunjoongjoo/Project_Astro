using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentCardUI : MonoBehaviour
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _descTxt;
    [SerializeField] private Button _selectBtn;

    private AugmentData _data;

    //3.4 더블클릭방지용 변수선언
    private bool _isClicked = false;

    public void Setup(AugmentData data)
    {
        _data = data;
        _nameTxt.text = _data.name;
        _descTxt.text = _data.description;
        _iconImg.sprite = _data.icon;

        _isClicked = false;
        _selectBtn.interactable = true;

        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(() =>
        {
            if (_isClicked) return;

            //나중에 풀링 방식으로 바꿔도 쓸 수 있도록 초기화 (지금은 카드 Destroy됨)
            _isClicked = true;
            _selectBtn.interactable = false;

            AugmentManager.Instance.SelectAugment(data);
            GetComponentInParent<AugmentWindowUI>().Close();
        });
    }
}
