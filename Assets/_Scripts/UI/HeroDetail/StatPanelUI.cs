using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _statNameTxt;
    [SerializeField] private TMP_Text _statValueTxt;
    [SerializeField] private Image _statIcon;
    [SerializeField] private Image _panel;
    [SerializeField] private Button _toastButton;

    public void SetStat(string name,string value, Sprite icon,Color color = default)
    {
        _statNameTxt.text = name;
        _statValueTxt.text = value;
        _statIcon.sprite = icon;
        _panel.color = (color == default) ? Color.white : color;

        if (_toastButton != null) _toastButton.gameObject.SetActive(false);
    }

    public void EnableToastButton(System.Action onClickAction)
    {
        if (_toastButton == null) return;

        _toastButton.gameObject.SetActive(true);
        _toastButton.onClick.RemoveAllListeners();
        _toastButton.onClick.AddListener(() => onClickAction?.Invoke());
    }

}
