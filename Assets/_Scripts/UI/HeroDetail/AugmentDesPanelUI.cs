using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentDesPanelUI : MonoBehaviour
{
    [SerializeField] private Image _IconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _desTxt;

    public void Setup(string name, string description, Sprite icon)
    {
        _nameTxt.text = name;
        _desTxt.text = description;
        _IconImg.sprite = icon;
    }
}
