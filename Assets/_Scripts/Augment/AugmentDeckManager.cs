using Fusion;
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

    //아이콘, 프리팹 가져올 SO
    private HeroIconDataSO _heroIconSO;

    public AugmentDeckManager(List<SkillAugmentSO> loadedSkillAugments, HeroIconDataSO heroIconSO)
    {
        _allSkillAugments = loadedSkillAugments;
        _heroIconSO = heroIconSO;
    }

    public List<AugmentData> GenerateCards(
        bool isFirstSelection,
        List<string> ownedHeroIds,
        List<string> ownedSkillAugmentIds,
        bool isItemFull,
        int totalAugmentPicks,
        int reinforceNumber)
    {
        List<AugmentData> drawnCards = new List<AugmentData>();
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
            AugmentData newCard = CreateCard(targetType, excludeRefIds, ownedHeroIds, ownedSkillAugmentIds, totalAugmentPicks, reinforceNumber);

            if (newCard != null)
            {
                drawnCards.Add(newCard); 
                excludeRefIds.Add(newCard.targetId);  
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
            {
                types[i] = isSkillMax ? AugmentType.Item : AugmentType.Skill;
            }

            else if (types[i] == AugmentType.Skill && isSkillMax)
            {
                types[i] = isItemFull ? AugmentType.Hero : AugmentType.Item;
            }

            else if (types[i] == AugmentType.Item && isItemFull)
            {
                types[i] = isSkillMax ? AugmentType.Hero : AugmentType.Skill;
            }
        }

        return types;
    }

    //카드 생성기
    private AugmentData CreateCard(
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
        NetworkPrefabRef heroPrefab = default;

        switch (type)
        {
            case AugmentType.Hero:
                List<HeroData> validHeroes = new List<HeroData>();
                var allHeroes = TableManager.Instance.HeroTable.GetAll();

                for (int i = 0; i < allHeroes.Count; i++)
                {
                    HeroData hero = allHeroes[i];

                    //내 덱에 없는 영웅 & 이번 턴에 이미 띄운 카드가 아닐 것
                    if (!ownedHeroIds.Contains(hero.id) && !excludeRefIds.Contains(hero.id))
                    {
                        validHeroes.Add(hero);
                    }
                }

                if (validHeroes.Count > 0)
                {
                    HeroData pickedHero = validHeroes[Random.Range(0, validHeroes.Count)];
                    refId = pickedHero.id;

                    title = GetString(pickedHero.heroName);
                    desc = GetString(pickedHero.heroDesc); 
                    if (_heroIconSO != null)
                    {
                        icon = _heroIconSO.GetIcon(refId);       // ID(hero_corsair 등)로 아이콘 획득
                        heroPrefab = _heroIconSO.GetPrefab(refId); // ID로 네트워크 프리팹 획득
                    }
                }
                else return null; //뽑을 영웅이 없으면 null
                break;

            case AugmentType.Item:
                List<ItemData> validItems = new List<ItemData>();
                var allItems = TableManager.Instance.ItemTable.GetAll();

                for (int i = 0; i < allItems.Count; i++)
                {
                    ItemData item = allItems[i];

                    //이번 턴에 중복으로 띄우지 않을 것
                    if (!excludeRefIds.Contains(item.id))
                    {
                        validItems.Add(item);
                    }
                }

                if (validItems.Count > 0)
                {
                    ItemData pickedItem = validItems[Random.Range(0, validItems.Count)];
                    refId = pickedItem.id;
                    title = GetString(pickedItem.name);
                    desc = "";
                    //여기쯤 아이콘 연동?
                }
                else return null;
                break;

            case AugmentType.Skill:
                List<SkillAugmentSO> validSkills = new List<SkillAugmentSO>();

                for (int i = 0; i < _allSkillAugments.Count; i++)
                {
                    SkillAugmentSO skill = _allSkillAugments[i];

                    //1. 내필드의 영웅 강화 증강인가
                    //2. 내가이미 집은 증강인가
                    //3. 반대 트리 제외
                    //4. 동시출현 방지
                    if (ownedHeroIds.Contains(skill.TargetHeroID) &&
                        !ownedSkillAugmentIds.Contains(skill.AugmentID) &&
                        (string.IsNullOrEmpty(skill.ConflictID) || !ownedSkillAugmentIds.Contains(skill.ConflictID)) &&
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

        return new AugmentData()
        {
            instanceId = instanceId,
            type = type,
            targetId = refId,
            name = title,
            description = desc,
            icon = icon,
            heroPrefab = heroPrefab
        };
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
                (string.IsNullOrEmpty(skill.ConflictID) || !ownedSkillAugmentIds.Contains(skill.ConflictID)))
            {
                return false;
            }
        }

        return true;
    }

    //String테이블 가져오기
    private string GetString(string stringId)
    {
        if (string.IsNullOrEmpty(stringId)) return "";

        var stringData = TableManager.Instance.StringTable.Get(stringId);

        if (stringData != null)
        {
            return stringData.textKor; 
        }

        //데이터가 누락되었다면 키값 띄우기
        return stringId;
    }
}
