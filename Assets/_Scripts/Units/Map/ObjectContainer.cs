using System;
using UnityEngine;

// 스테이지 씬에서 전역 접근 필요하면 여기 추가하세요
public class ObjectContainer : Singleton<ObjectContainer>
{
    [Header("영웅 & 미니언 경험치 (100당 1회 증강 선택)")]
    private int MINION_AUGMENT_EXP;
    private int SUPER_MINION_AUGMENT_EXP;
    private int HERO_AUGMENT_EXP;

    [Header("전역 참조가 필요한 오브젝트")]
    public UnitBase[] blueSideStructure;
    public UnitBase[] redSideStructure;

    public int BridgeIndex {  get; private set; }

    public event Action<Team, int> OnIncreasedAugmentGauge;

    private void Start()
    {
        BridgeIndex = 2;

        MINION_AUGMENT_EXP = int.Parse(TableManager.Instance.ConfigTable.Get("augment_exp_minion").configValue);
        SUPER_MINION_AUGMENT_EXP = int.Parse(TableManager.Instance.ConfigTable.Get("augment_exp_superminion").configValue);
        HERO_AUGMENT_EXP = int.Parse(TableManager.Instance.ConfigTable.Get("augment_exp_hero").configValue);
    }

    public void IncreaseAugmentGauge(Team diedUnitTeam, UnitType type)
    {
        // 타워와 브릿지, 중립 유닛은 증강 게이지를 채우지 않는다.
        if (type == UnitType.Tower || type == UnitType.Bridge) return;
        if (diedUnitTeam == Team.None) return;

        // 어느 팀에서 호출했느냐에 따라 다름.
        // 죽은 유닛이 호출하니까
        // 게이지는 반대 팀을 채워줘야 함.
        Team targetTeam = diedUnitTeam == Team.Blue ? Team.Red : Team.Blue;
        int amount = type == UnitType.Hero ? HERO_AUGMENT_EXP : MINION_AUGMENT_EXP;

        OnIncreasedAugmentGauge?.Invoke(targetTeam, amount); 
    }
}
