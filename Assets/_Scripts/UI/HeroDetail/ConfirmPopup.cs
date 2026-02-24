using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : BaseUI
{
    [SerializeField] private TMP_Text _messageTxt;
    [SerializeField] private Button _yesBtn;
    [SerializeField] private Button _noBtn;

    public void Setup(string msg,Action onYes)
    {
        _messageTxt.text = msg;
        _yesBtn.onClick.RemoveAllListeners();
        _yesBtn.onClick.AddListener(() => { 
            onYes?.Invoke();
            OnBackButtonPressed();
        });

        _noBtn.onClick.RemoveAllListeners();
        _noBtn.onClick.AddListener(() => {
            OnBackButtonPressed(); // 그냥 닫기
        });
    }
}
