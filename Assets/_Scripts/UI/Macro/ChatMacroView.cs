using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatMacroView : MonoBehaviour
{
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Transform _equippedContainer;
    [SerializeField] private Transform _allListContainer;
    [SerializeField] private ChatMacroSlot _slotPrefab;

    // 객체 재사용을 위한 리스트
    private List<ChatMacroSlot> _equippedSlots = new List<ChatMacroSlot>();
    private List<ChatMacroSlot> _allListSlots = new List<ChatMacroSlot>();

    public void RefreshList(MacroType currentTab, List<ChatMacroData> allMacros, List<string> editingIds, List<string> equippedIds, int maxCount, Action<string> onClick, Action<string> onRemoveClick)
    {
        // 탭 종류에 따른 타이틀 결정
        string title = (currentTab == MacroType.Text) ? "장착된 매크로" : "장착된 이모티콘";

        // 개수 텍스트 업데이트
        if (_countText != null)
            _countText.text = $"{title} ({equippedIds.Count}/{maxCount})";

        //하단 보유 목록 갱신
        UpdateSlots(_allListContainer, _allListSlots, allMacros.Count, (index) => {
            var macro = allMacros[index];
            bool isPending = editingIds.Contains(macro.id);
            _allListSlots[index].Setup(macro, false, isPending, onClick);
        });

        //상단 장착 목록 갱신
        UpdateSlots(_equippedContainer, _equippedSlots, equippedIds.Count, (index) => {
            var id = equippedIds[index];
            var macro = allMacros.Find(m => m.id == id);
            if (macro != null)
            {
                _equippedSlots[index].Setup(macro, true, false, onRemoveClick);
            }
        });
    }

    // 슬롯을 생성하거나 재사용하는 공통 로직
    private void UpdateSlots(Transform container, List<ChatMacroSlot> slotList, int requiredCount, Action<int> setupAction)
    {
        // 부족하면 새로 생성
        while (slotList.Count < requiredCount)
        {
            var newSlot = Instantiate(_slotPrefab, container);
            slotList.Add(newSlot);
        }

        // 남으면 비활성화 (Destroy 대신 SetActive)
        for (int i = 0; i < slotList.Count; i++)
        {
            if (i < requiredCount)
            {
                slotList[i].gameObject.SetActive(true);
                setupAction?.Invoke(i);
            }
            else
            {
                slotList[i].gameObject.SetActive(false);
            }
        }
    }
}
