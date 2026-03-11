using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _statNameTxt;
    [SerializeField] private TMP_Text _statValueTxt;
    [SerializeField] private Image _statIcon;
    [SerializeField] private Image _panel;

    public void SetStat(string name,string value, Sprite icon,Color color = default)
    {
        _statNameTxt.text = name;
        _statValueTxt.text = value;
        _statIcon.sprite = icon;
        _panel.color = (color == default) ? Color.white : color;
    }
    
}
