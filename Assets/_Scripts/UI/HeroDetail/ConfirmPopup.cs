using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : BaseUI
{
    [SerializeField] private TMP_Text _messageTxt;
    [SerializeField] private Button _yesBtn;
    [SerializeField] private TMP_Text _yesBtnTxt;
    [SerializeField] private Button _noBtn;

    public void Setup(string msg,Action onYes, bool canConfirm = true, string denyMsg = "", string yesText = "확인", string noText = "취소")
    {
        //메시지 설정 조건따라 일반 또는 거절
        _messageTxt.text = canConfirm ? msg : denyMsg;

        // 돈부족하면 버튼 못누르게
        _yesBtn.interactable = canConfirm;

        // 버튼 텍스트 변경 
        if (_yesBtnTxt != null)
        {
            _yesBtnTxt.text = yesText;
            _yesBtnTxt.color = canConfirm ? Color.white : Color.gray;
        }

        //취소 버튼 텍스트도 바꾸고 싶으면 추가
        // _noBtnTxt.text = noText;

        // YES 버튼 리스너
        _yesBtn.onClick.RemoveAllListeners();
        if (canConfirm)
        {
            _yesBtn.onClick.AddListener(() => {
                onYes?.Invoke();
                OnBackButtonPressed(false);
            });
        }

        _noBtn.onClick.RemoveAllListeners();
        _noBtn.onClick.AddListener(() => {
            OnBackButtonPressed(); // 그냥 닫기
        });
    }
}
