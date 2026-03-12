using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatMacroSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _txt;
    [SerializeField] private Image _dotColor;
    [SerializeField] private Image _emoticonImg;
    [SerializeField] private GameObject _checkMark;
    [SerializeField] private GameObject _removeBtn;

    private string _macroId;
    private Action<string> _onClickCallback;

    public void Setup(ChatMacroData data, bool isEquippedSlot, bool isPending, Action<string> onClick)
    {
        _macroId = data.id;
        _onClickCallback = onClick;

        // 타입에 따른 시각화 분기
        if (data.type == MacroType.Text)
        {
            _txt.gameObject.SetActive(true);
            _emoticonImg.gameObject.SetActive(false);
            _txt.text = data.text;
        }
        else
        {
            _txt.gameObject.SetActive(false);
            _emoticonImg.gameObject.SetActive(true);
            _emoticonImg.sprite = data.emoticonSprite;
        }

        //버튼 설정
        Button mainBtn = GetComponent<Button>();
        mainBtn.onClick.RemoveAllListeners();

        if (isEquippedSlot)
        {
            // 상단 슬롯: 체크는 끄고, X 버튼만 활성화
            if (_checkMark) _checkMark.SetActive(false);
            if (_removeBtn)
            {
                _removeBtn.SetActive(true);
                // X 버튼에만 제거 이벤트 할당
                Button xBtn = _removeBtn.GetComponent<Button>();
                xBtn.onClick.RemoveAllListeners();
                xBtn.onClick.AddListener(() => _onClickCallback?.Invoke(_macroId));
            }
            //몸통클릭시에는 반응안하게
            mainBtn.interactable = false;
        }
        else
        {
            // 하단 목록: 편집 리스트에 있으면 체크 활성화, X 버튼은 항상 끔
            if (_checkMark) _checkMark.SetActive(isPending);
            if (_removeBtn) _removeBtn.SetActive(false);

            mainBtn.interactable = true;
            mainBtn.onClick.AddListener(() => _onClickCallback?.Invoke(_macroId));
        }
    }
}
