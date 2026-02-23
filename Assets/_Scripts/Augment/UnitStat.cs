using UnityEngine;
using System.Collections.Generic;

//현재 스탯을 계산하여 유닛에게 달아줄 관리자
//외부 요인의 효과 적용을 알맞은 Stat 객체로 분배

//02.22 실드 삭제, 받피감/쿨감 기본값 0 세팅, 이속/탐지범위 Standard 편입
public class UnitStat : MonoBehaviour //추후 NetWorkBehavior
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



    //초기화
    public void Init(HeroStatData data)
    {

    }

    //일단 3개
    public void AddModifier(float atk, float def, float spd)
    {

    }
}
