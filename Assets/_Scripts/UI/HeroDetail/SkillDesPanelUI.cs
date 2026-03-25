using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDesPanelUI : MonoBehaviour
{
    [SerializeField] private Image _IconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _desTxt;
    [SerializeField] private TMP_Text _coolTimeTxt;

    public void Setup(string name,string description,string cooltime,Sprite icon)
    {
        _nameTxt.text = name;
        _desTxt.text = description;
        _coolTimeTxt.text = cooltime;
        _IconImg.sprite = icon;
    }
}
