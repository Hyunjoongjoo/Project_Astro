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

    public void Setup(AugmentData data)
    {
        _data = data;
        _nameTxt.text = _data.name;
        _descTxt.text = _data.description;
        _iconImg.sprite = _data.icon;

        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(() =>
        {
            AugmentManager.Instance.SelectAugment(data);
            GetComponentInParent<AugmentWindowUI>().Close();
        });
    }
}
