using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DescriptionPanelUI : MonoBehaviour
{
    //[SerializeField] private Image _IconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _desTxt;

    public void SetDes(string name,string description)
    {
        _nameTxt.text = name;
        _desTxt.text = description;
    }
}
