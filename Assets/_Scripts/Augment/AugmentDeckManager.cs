using System.Collections.Generic;
using UnityEngine;

//1. 유저 현재 상태 분석
//2. 예외처리 or 한쪽 선택시 나머지 밴
//3. 증강 3장 띄워주기

public class AugmentDeckManager
{
    //증강 화면에서 유저가 몇 번 골랐는 지 전달용
    private int _instanceCounter = 0;

    //스킬증강 SO
    private List<SkillAugmentSO> _allSkillAugments;

    public AugmentDeckManager(List<SkillAugmentSO> loadedSkillAugments)
    {
        _allSkillAugments = loadedSkillAugments;
    }

    public List<AugmentCard> GenerateCards(
        bool isFirstSelection,
        List<string> ownedHeroIds,
        List<string> ownedSkillAugmentIds,
        bool isItemFull,
        int totalAugmentPicks,
        int reinforceNumber)
    {
        List<AugmentCard> drawnCards = new List<AugmentCard>();
        List<string> excludeRefIds = new List<string>();

        //보유 영웅 수 및 스킬 Max 여부 판별
        int heroCount = ownedHeroIds.Count;
        bool isSkillMax = CheckIfAllSkillsMaxed(ownedHeroIds, ownedSkillAugmentIds);

        //3개 슬롯의 타입 결정
        AugmentType[] slotTypes = DetermineSlotTypes(isFirstSelection, heroCount, isSkillMax, isItemFull);

        //결정된 타입에 맞춰 실제 데이터에서 카드를 찍어냄
        for (int i = 0; i < 3; i++)
        {
            AugmentType targetType = slotTypes[i];
            AugmentCard newCard = CreateCard(targetType, excludeRefIds, ownedHeroIds, ownedSkillAugmentIds, totalAugmentPicks, reinforceNumber);

            if (newCard != null)
            {
                drawnCards.Add(newCard); 
                excludeRefIds.Add(newCard.ReferenceId);  
            } 
            else
            {
                Debug.LogWarning($"{targetType} 타입 카드 생성 실패, 조건을 만족하는 풀x");
            }
        }

        return drawnCards;
    }

    //기획서에 명시된 예외 처리
    //0 ~ 2 첫 증강이거나 영웅이 0명이면 hero 3장
    //첫번째 슬롯이 hero인데 영웅슬롯 찼으면 스킬or아이템카드, 스킬강화도 끝났으면 아이템으로
    private AugmentType[] DetermineSlotTypes(bool isFirstSelection, int heroCount, bool isSkillMax, bool isItemFull)
    {
        if (isFirstSelection) return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Hero };
        if (heroCount == 0) return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Hero };

        AugmentType[] types = new AugmentType[] { AugmentType.Hero, AugmentType.Skill, AugmentType.Item };

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == AugmentType.Hero && heroCount >= 5)
                types[i] = isSkillMax ? AugmentType.Item : AugmentType.Skill;

            else if (types[i] == AugmentType.Skill && isSkillMax)
                types[i] = isItemFull ? AugmentType.Hero : AugmentType.Item;

            else if (types[i] == AugmentType.Item && isItemFull)
                types[i] = isSkillMax ? AugmentType.Hero : AugmentType.Skill;
        }

        return types;
    }
    private AugmentCard CreateCard(
        AugmentType type, List<string> excludeRefIds,
        List<string> ownedHeroIds, List<string> ownedSkillAugmentIds,
        int totalPicks, int reinforceNum)
    {
        _instanceCounter++;
        string instanceId = $"Card_{_instanceCounter}";

        string refId = "";
        string title = "";
        string desc = "";
        Sprite icon = null;

        switch (type)
        {
            case AugmentType.Hero:
                // TODO: TableManager.Instance.HeroTable 연동 (LINQ 없이 for문 캐싱으로 구현 필요)
                refId = "Hero_Target_ID";
                title = "새로운 영웅 해금";
                desc = "영웅을 해금합니다.";
                break;

            case AugmentType.Item:
                // TODO: TableManager.Instance.ItemTable 연동
                refId = "Item_Target_ID";
                title = "아이템 획득";
                desc = "아이템 보관소로 들어갑니다.";
                break;

            case AugmentType.Skill:
                List<SkillAugmentSO> validSkills = new List<SkillAugmentSO>();

                for (int i = 0; i < _allSkillAugments.Count; i++)
                {
                    SkillAugmentSO skill = _allSkillAugments[i];

                    if (ownedHeroIds.Contains(skill.TargetHeroID) &&
                        !ownedSkillAugmentIds.Contains(skill.AugmentID) &&
                        !ownedSkillAugmentIds.Contains(skill.ConflictID) &&
                        !excludeRefIds.Contains(skill.AugmentID))
                    {
                        validSkills.Add(skill);
                    }
                }

                if (validSkills.Count > 0)
                {
                    var pickedSkill = validSkills[Random.Range(0, validSkills.Count)];

                    int tierIndex = (totalPicks >= reinforceNum) ? 1 : 0;

                    refId = pickedSkill.AugmentID;
                    title = pickedSkill.Title;
                    icon = pickedSkill.Icon;
                    desc = pickedSkill.Tiers[tierIndex].Description;
                }
                else return null;
                break;
        }

        return new AugmentCard(instanceId, type, refId, title, desc, icon);
    }

    /// <summary>
    ///현재 보유한 영웅이 더 이상 받을 스킬 증강이 없는지 검사
    /// </summary>
    private bool CheckIfAllSkillsMaxed(List<string> ownedHeroIds, List<string> ownedSkillAugmentIds)
    {
        for (int i = 0; i < _allSkillAugments.Count; i++)
        {
            SkillAugmentSO skill = _allSkillAugments[i];

            //받을 수 있는 스킬 증강이 단 하나라도 존재한다면? 최대아님
            if (ownedHeroIds.Contains(skill.TargetHeroID) &&
                !ownedSkillAugmentIds.Contains(skill.AugmentID) &&
                !ownedSkillAugmentIds.Contains(skill.ConflictID))
            {
                return false;
            }
        }

        return true;
    }
}
