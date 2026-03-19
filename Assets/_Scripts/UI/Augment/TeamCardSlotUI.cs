using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamCardSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private Transform _cardSlot;
    [SerializeField] private TeamHeroCardUI _cardPrefab;

    [Header("Item UI")]
    [SerializeField] private Image[] _itemSlots;
    [SerializeField] private ItemIconDataSO _itemIconData;
    //3.15 추가
    public PlayerRef AllyPlayerRef { get; private set; }

    //처음 슬룻 만들기
    public void Initialize(string nickname, PlayerRef allyRef) //매개변수 추가
    {
        _nameTxt.text = nickname;
        AllyPlayerRef = allyRef;
    }

    //데이터 변경시 호출
    public void Refresh(SlotData_5 heroData)
    {
        // 1. 기존 카드 전부 삭제
        foreach (Transform child in _cardSlot)
        {
            Destroy(child.gameObject);
        }

        // 2. 현재 보유한 영웅 개수만큼만 새로 생성
        for (int i = 0; i < heroData.Count; i++)
        {
            string id = heroData.Get(i); // 구조체 내부의 Get() 사용

            // 데이터가 유효한 경우에만 생성
            if (!string.IsNullOrEmpty(id))
            {
                var go = Instantiate(_cardPrefab, _cardSlot);
                if (go.TryGetComponent(out TeamHeroCardUI card))
                {
                    card.Setup(id);
                }
            }
        }
    }

   //3.18
   //매니저가 아이템 데이터를 던져주면 화면에 그리는 함수
   public void UpdateItems(SlotData_3 inventoryData)
   {
       if (_itemSlots == null || _itemIconData == null) return;

       for (int i = 0; i < SlotData_3.Length; i++)
       {
           if (i < _itemSlots.Length)
           {
               string itemId = inventoryData.Get(i).Replace("\0", "").Trim();
               
               if (!string.IsNullOrEmpty(itemId))
               {
                   _itemSlots[i].sprite = _itemIconData.GetIcon(itemId);
                   _itemSlots[i].gameObject.SetActive(true);
               }
               else
               {
                   _itemSlots[i].gameObject.SetActive(false); //빈칸이면 끄기
               }
           }
       }
   }

}
