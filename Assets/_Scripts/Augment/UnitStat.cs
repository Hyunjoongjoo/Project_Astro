using UnityEngine;
using System.Collections.Generic;

//현재 스탯을 계산하여 유닛에게 달아줄 관리자
//외부 요인의 효과 적용을 알맞은 Stat 객체로 분배

//02.22 실드 삭제, 받피감/쿨감 기본값 0 세팅, 이속/탐지범위 Standard 편입
public class UnitStat : MonoBehaviour 
{
    //각 스탯 정의

    //Standard 
    public Stat MaxHp { get; private set; }
    public Stat Attack { get; private set; }
    public Stat HealPower { get; private set; }

    //Delay 공속, 리스폰
    public Stat AttackSpeed { get; private set; }
    public Stat RespawnTime { get; private set; }

    //Speed 이속 탐지범위
    public Stat MoveSpeed { get; private set; }
    public Stat DetectRange { get; private set; }

    //Additive 받피감, 쿨다운
    public Stat DamageReduction { get; private set; }
    public Stat CooldownReduction { get; private set; }



    //EffectType 을 확인, 어떤 Stat 객체에 넣어줄지 연결해주는 딕셔너리
    private Dictionary<EffectType, Stat> _statMap = new Dictionary<EffectType, Stat>();

    //초기화
    //HeroManager가 영웅 정보를 던져주면 세팅
    //추후 미니언도 다 이렇게 굴러갈 듯?
    //그 땐 아예 이 스크립트를 HeroStat, 미니언을 MinionStat 로 분리할 수도 있을 듯.
    public void Init(HeroStatData data)
    {
        //각 스탯마다 StatCalcMode 할당 및 기본값 셋업

        //Standard 그룹
        MaxHp = new Stat(StatCalcMode.Standard, data.BaseHp);
        Attack = new Stat(StatCalcMode.Standard, data.baseAttackPower);
        HealPower = new Stat(StatCalcMode.Standard, data.baseHealingPower);

        //Delay 그룹
        AttackSpeed = new Stat(StatCalcMode.Delay, data.attackSpeed);
        RespawnTime = new Stat(StatCalcMode.Delay, data.spawnCooldown);

        //Speed 그룹
        MoveSpeed = new Stat(StatCalcMode.Speed, data.moveSpeed);
        DetectRange = new Stat(StatCalcMode.Speed, data.detectionRange);

        //Additive 그룹 (기본값 0)
        DamageReduction = new Stat(StatCalcMode.Additive, 0f);
        CooldownReduction = new Stat(StatCalcMode.Additive, 0f);

        MapStat(EffectType.IncreaseMaxHp, MaxHp);
        MapStat(EffectType.DecreaseMaxHp, MaxHp);

        MapStat(EffectType.IncreaseAttackPower, Attack);
        MapStat(EffectType.IncreaseHealPower, HealPower);

        MapStat(EffectType.IncreaseMoveSpeed, MoveSpeed);
        MapStat(EffectType.DecreaseMoveSpeed, MoveSpeed);
        MapStat(EffectType.IncreaseDetectionRange, DetectRange); //탐지범위 범례 추가 시 주석 해제(이름바꿔야함)

        //받피감
        MapStat(EffectType.DecreaseDamageTaken, DamageReduction);
        MapStat(EffectType.IncreaseDamageTaken, DamageReduction);


        MapStat(EffectType.IncreaseAttackSpeed, AttackSpeed);
        MapStat(EffectType.DecreaseAttackSpeed, AttackSpeed);
        MapStat(EffectType.DecreaseCooldown, CooldownReduction);
        MapStat(EffectType.DecreaseRespawnTime, RespawnTime);

    }

    public void Init(UnitData data)//03-06 오버로딩
    {
        MaxHp = new Stat(StatCalcMode.Standard, data.baseHp);
        Attack = new Stat(StatCalcMode.Standard, data.baseAttackPower);
        HealPower = new Stat(StatCalcMode.Standard, data.baseHealingPower);

        AttackSpeed = new Stat(StatCalcMode.Delay, data.attackSpeed);
        RespawnTime = new Stat(StatCalcMode.Delay, 0f);

        MoveSpeed = new Stat(StatCalcMode.Speed, data.moveSpeed);
        DetectRange = new Stat(StatCalcMode.Speed, data.detectionRange);

        DamageReduction = new Stat(StatCalcMode.Additive, data.damageReduce);
        CooldownReduction = new Stat(StatCalcMode.Additive, 0f);

        MapStat(EffectType.IncreaseMaxHp, MaxHp);
        MapStat(EffectType.DecreaseMaxHp, MaxHp);

        MapStat(EffectType.IncreaseAttackPower, Attack);
        MapStat(EffectType.IncreaseHealPower, HealPower);

        MapStat(EffectType.IncreaseMoveSpeed, MoveSpeed);
        MapStat(EffectType.DecreaseMoveSpeed, MoveSpeed);
        MapStat(EffectType.IncreaseDetectionRange, DetectRange);

        MapStat(EffectType.DecreaseDamageTaken, DamageReduction);
        MapStat(EffectType.IncreaseDamageTaken, DamageReduction);

        MapStat(EffectType.IncreaseAttackSpeed, AttackSpeed);
        MapStat(EffectType.DecreaseAttackSpeed, AttackSpeed);
        MapStat(EffectType.DecreaseCooldown, CooldownReduction);
        MapStat(EffectType.DecreaseRespawnTime, RespawnTime);
    }

    //딕셔너리에 매핑을 추가하는 헬퍼 함수
    private void MapStat(EffectType type, Stat statObj)
    {
        if (!_statMap.ContainsKey(type))
        {
            _statMap.Add(type, statObj);
        }
    }


    //새로운 스탯 변동(증강 선택, 버프, 디버프)을 적용
    public void AddModifier(EffectType type, StatModifier modifier)
    {
        if (_statMap.TryGetValue(type, out Stat stat))
        {
            float before = stat.Value;

            stat.AddModifier(modifier);
            Debug.Log($"<color=green>[StatMod]</color> {type} 변경: {before} -> {stat.Value} (Source: {modifier.Source})");

        }
        else
        {
            Debug.LogWarning($"UnitStat에서 연결되지 않은 효과 타입: {type}");
        }
    }

    //기존 스탯 변동을 제거(Source 기반 역추적)
    public void RemoveModifier(EffectType type, object source)
    {
        if (_statMap.TryGetValue(type, out Stat stat))
        {
            stat.RemoveModifier(source);
        }
    }

    
    //현재 최종 스탯 값 반환 메서드 (전투 데미지 계산, 스킬 쿨타임 로직 등에서 호출하기 위함)
    public float GetStatValue(EffectType type)
    {
        if (_statMap.TryGetValue(type, out Stat stat))
        {
            //내부적으로 _isDirty 플래그를 체크하여 필요할 때만 재계산된 값을 반환
            return stat.Value;
        }

        Debug.LogWarning("매핑 안 된 스탯임");
        return 0f; //매핑되지 않은 스탯을 물어보면 0 반환
    }
}
