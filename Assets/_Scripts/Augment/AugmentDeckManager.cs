/* 
 * 
3.9 리팩토링

## 사유

1. 아군 영웅 공유 불가: 나와 아군 영웅 슬롯에 배치된 영웅의 스킬이 선택지에 떠야 함. => 내 덱에는 없지만 아군 덱에 있는 영웅의 스킬도 내 화면에 떠야 함.
2. 동시 출현 방지 없음: 아군이 스킬 증강의 대상으로 선택되어 있는 영웅은 스킬 증강 선택지에서 배제. => 나와 아군이 동시에 동일 영웅의 스킬 증강 카드를 받으면 안 됨(컨플릭트도 뜨면 안 됨).


## 해결책: 서버에서 한 번에 뽑아서 전달

1. AugmentDeckManager 매개변수 변경

- 내정보 + 아군 정보까지 넘겨받도록

2. 서버 통제형 구조

- AugmentController에서 각자 로컬로 덱 매니저를 호출 X
- 서버가 1P 카드 3장, 2P 카드 3장 순차적으로 뽑도록 수정

3. RPC 통신 추가

- 서버가 뽑은 카드 3장의 ID를 각 클라이언트에게 RPC로 전송.
- 클라이언트는 받은 ID를 바탕으로 UI만 그리는 식으로 분리.

 */


using Fusion;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



//1. 유저 현재 상태 분석
//2. 예외처리 or 한쪽 선택시 나머지 밴
//3. 증강 3장 띄워주기

public class AugmentDeckManager
{
    //증강 화면에서 유저가 몇 번 골랐는 지 전달용
    private int _instanceCounter = 0;

    //스킬증강 SO
    private List<SkillAugmentSO> _allSkillAugments;

    //영웅 기본 스킬을 찾기 위한 모든 스킬 데이터 모음
    private List<BaseSkillSO> _allBaseSkills;

    //아이콘, 프리팹 가져올 SO
    private HeroIconDataSO _heroIconSO;

    public AugmentDeckManager(List<SkillAugmentSO> loadedSkillAugments, HeroIconDataSO heroIconSO, List<BaseSkillSO> loadedBaseSkills)
    {
        _allSkillAugments = loadedSkillAugments;
        _heroIconSO = heroIconSO;
        _allBaseSkills = loadedBaseSkills;
    }

    public List<AugmentData> GenerateCards(
        bool isFirstSelection,
        List<string> ownedHeroIds,
        List<string> ownedSkillAugmentIds,
        bool isItemFull,
        int totalAugmentPicks,
        int reinforceNumber,
        //아군의 영웅 리스트와 이미 뽑힌 타겟 영웅 배제 리스트
        List<string> teamHeroIds,
        List<string> excludedSkillTargetIds)
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
            //CreateCard에 새로 추가된 팀 정보, 제외할 스킬타겟아이디 추가
            AugmentData newCard = CreateCard(targetType, excludeRefIds, ownedHeroIds, ownedSkillAugmentIds, totalAugmentPicks, reinforceNumber, teamHeroIds, excludedSkillTargetIds);
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

    //3.4 수정사항 반영(스킬증강 완료전까지 따로 보관해야함)
    //3.13 Case 별 우선순위 세분화 완료
    //순서대로 1 -> 8 쓰면 단일 조건이 복합 조건을 삼켜버리는 경우가 생기므로 순서 정해두는 리팩토링
    //Case2 최초 -> 예외처리 -> Case6~8 단일보다 먼저 -> Case3~5 단일조건(복합에서 걸러진 케이스) -> Case1 조건 X 
    private AugmentType[] DetermineSlotTypes(bool isFirstSelection, int heroCount, bool isSkillMax, bool isItemFull)
    {
        bool isHeroFull = heroCount >= SlotData_5.Length;

        //Case 2. 최초 증강 선택 시
        if (isFirstSelection || heroCount == 0)
            return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Hero };

        //3가지가 모두 꽉 차면 예외처리
        if (isHeroFull && isItemFull && isSkillMax)
            return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Hero };

        //Case 6~8 복합조건

        //Case 6
        if (isHeroFull && isItemFull && !isSkillMax)
            return new AugmentType[] { AugmentType.Skill, AugmentType.Skill, AugmentType.Skill };

        //Case 7
        if (isHeroFull && !isItemFull && isSkillMax)
            return new AugmentType[] { AugmentType.Item, AugmentType.Item, AugmentType.Item };

        //Case 8
        if (!isHeroFull && isItemFull && isSkillMax)
            return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Hero };

        //Case 3~5는 단일조건
        //Case 3
        if (isHeroFull && !isItemFull && !isSkillMax)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0) return new AugmentType[] { AugmentType.Skill, AugmentType.Skill, AugmentType.Item };
            else return new AugmentType[] { AugmentType.Skill, AugmentType.Item, AugmentType.Item };
        }

        //Case4
        if (!isHeroFull && isItemFull && !isSkillMax)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0) return new AugmentType[] { AugmentType.Hero, AugmentType.Skill, AugmentType.Skill };
            else return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Skill };
        }

        //Case 5

        if (!isHeroFull && !isItemFull && isSkillMax)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0) return new AugmentType[] { AugmentType.Hero, AugmentType.Item, AugmentType.Item };
            else return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Item };
        }

        //Case1 조건X
        //셋 중 무작위
        int defaultRand = Random.Range(0, 3);
        if (defaultRand == 0) return new AugmentType[] { AugmentType.Hero, AugmentType.Skill, AugmentType.Item };
        else if (defaultRand == 1) return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Item };
        else return new AugmentType[] { AugmentType.Hero, AugmentType.Hero, AugmentType.Skill };
    }

    //카드 생성기
    private AugmentData CreateCard(
        AugmentType type, List<string> excludeRefIds,
        List<string> ownedHeroIds, List<string> ownedSkillAugmentIds,
        int totalPicks, int reinforceNum,
        List<string> teamHeroIds, List<string> excludedSkillTargetIds
        )
    {
        _instanceCounter++;
        string instanceId = $"Card_{_instanceCounter}";

        string refId = "";
        string title = "";
        string desc = "";
        Sprite icon = null;
        NetworkPrefabRef heroPrefab = default;

        //쿨타임 저장용
        float baseCooldown = 0f;
        float currentCooldown = 0f;

        //영웅,스킬용 변수
        HeroType hType = default;
        HeroRole hRole = default;
        MoveType mType = default;
        BaseSkillSO bSkill = null;

        string tHeroName = "";
        Sprite tHeroIcon = null;
        string bSkillName = "";

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

                    hType = pickedHero.heroType;
                    hRole = pickedHero.heroRole;

                    if (_heroIconSO != null)
                    {
                        icon = _heroIconSO.GetIcon(refId);       //ID로 아이콘
                        heroPrefab = _heroIconSO.GetPrefab(refId); //네트워크 프리팹
                    }
                    //HeroStatTable에서 쿨타임 가져오기(이건 성장x라서)
                    var heroStat = TableManager.Instance.HeroStatTable.Get(pickedHero.heroStatId);
                    if (heroStat != null)
                    {
                        baseCooldown = heroStat.spawnCooldown;
                        currentCooldown = heroStat.spawnCooldown;
                        mType = heroStat.moveType;
                    }
                    //해당 영웅의 기본 스킬 데이터 찾아오기
                    if (_allBaseSkills != null)
                    {
                        bSkill = _allBaseSkills.FirstOrDefault(s =>
                            s.heroId == refId &&
                            s.skillType == SkillType.NormalSkill);
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

                    //3.9 내 영웅 or 아군 영웅인지 확인
                    bool isTargetHeroOwned = ownedHeroIds.Contains(skill.TargetHeroID) || (teamHeroIds != null && teamHeroIds.Contains(skill.TargetHeroID));
                    //
                    if (isTargetHeroOwned &&
                        !ownedSkillAugmentIds.Contains(skill.AugmentID) &&
                        (string.IsNullOrEmpty(skill.ConflictID) || !ownedSkillAugmentIds.Contains(skill.ConflictID)) &&
                        !excludeRefIds.Contains(skill.AugmentID) &&
                        //팀원 간 겹침 방지: 이미 이번 턴에 뽑힌 타겟 영웅이면 배제
                        !excludedSkillTargetIds.Contains(skill.TargetHeroID))
                    {
                        validSkills.Add(skill);
                    }
                }

                if (validSkills.Count > 0)
                {
                    var pickedSkill = validSkills[Random.Range(0, validSkills.Count)];

                    //뽑힌 스킬의 타겟 영웅을 배제 리스트에 등록
                    excludedSkillTargetIds.Add(pickedSkill.TargetHeroID);

                    int tierIndex = (totalPicks >= reinforceNum) ? 1 : 0;

                    refId = pickedSkill.AugmentID;

                    icon = pickedSkill.Tiers[tierIndex].Icon;
                    title = GetString(pickedSkill.Tiers[tierIndex].TitleStringID);
                    desc = GetString(pickedSkill.Tiers[tierIndex].DescStringID);
                    bSkill = pickedSkill.Tiers[tierIndex].CombatSkillData;

                    //스킬 증강 카드일 때 영웅의 원본 정보들을 추적해서 채워줌
                    var heroData = TableManager.Instance.HeroTable.Get(pickedSkill.TargetHeroID);
                    if (heroData != null) tHeroName = GetString(heroData.heroName);

                    if (_heroIconSO != null) tHeroIcon = _heroIconSO.GetIcon(pickedSkill.TargetHeroID);

                    if (_allBaseSkills != null)
                    {
                        var baseSkill = _allBaseSkills.FirstOrDefault(s => s.heroId == pickedSkill.TargetHeroID && s.skillType == SkillType.NormalSkill);
                        if (baseSkill != null) bSkillName = baseSkill.skillName;
                    }
                }
                else return null;
                break;
        }

        return new AugmentData()
        {
            instanceId = instanceId,
            type = type,
            targetId = refId,
            titleName = title,
            description = desc,
            mainIcon = icon,
            heroPrefab = heroPrefab,
            baseSpawnCooldown = baseCooldown,
            currentSpawnCooldown = currentCooldown,

            heroType = hType,
            heroRole = hRole,
            moveType = mType,
            skillData = bSkill,

            targetHeroName = tHeroName,
            targetHeroIcon = tHeroIcon,
            baseSkillName = bSkillName
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
