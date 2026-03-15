using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TeamCardSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private Transform _cardSlot;
    [SerializeField] private TeamHeroCardUI _cardPrefab;

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
}
