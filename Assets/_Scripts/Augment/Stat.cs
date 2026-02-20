using UnityEngine;
using System;
using System.Collections.Generic;

//스탯 수정자
//아이템이나 버프가 주는 효과

public class StatModifier
{
    public float Value;         
    public StatModType Type;    //연산 타입(Flat,PA,PM)
    public object Source;       //중복방지용 출처 (뭐가 줬는지, 증강, 아이템 버프스킬 등등)

    public StatModifier(float value, StatModType type, object source)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}


//스탯
//계산식이 주입된 수치 클래스

[Serializable]
public class Stat
{
    public float BaseValue; //성장치 포함 기본값

    //어떤 공식으로 계산해야 하느냐?
    public StatCalcMode CalcMode;

    //이 스탯에 장착된 아이템/버프/디버프 효과 목록(정렬 후 전체합산용)
    protected List<StatModifier> _modifiers = new List<StatModifier>();

    protected bool _isDirty = true;
    
    //최종적으로 계산이 끝난 값(캐싱용)
    protected float _value;

    public Stat(StatCalcMode mode, float baseValue = 0f)
    {
        CalcMode = mode;
        BaseValue = baseValue;
    }

    //최종값 프로퍼티
    public float Value
    {
        get
        {
            if (_isDirty)
            {
                _value = CalculateFinalValue();
                _isDirty = false;
            }
            return _value;
        }
    }


    //아래부턴 기획서 공식 3가지 구현


    private float CalculateFinalValue()
    {
        //수정자 없으면 기본값반환
        if (_modifiers.Count == 0) return BaseValue;

        //모드에 따른 계산식 분기점(0218 기준 3개)
        switch (CalcMode)
        {
            case StatCalcMode.Standard:
                return CalculateStandard(); //체력, 공격력 등

            case StatCalcMode.Delay:
                return CalculateDelay(); //실드, 탐지범위

            case StatCalcMode.Additive:
                return CalculateAdditive(); //공속, 쿨감
        }
        return BaseValue;
    }

    //Standard: 체력, 공격력, 치유력, 이속, 탐지범위
    //증가% 합, 감소% 합 마지막에 곱셈 X
    //02.20 변경: (기본값 * (1 + 증가%합) * (1 - 감소%합)) + 고정합
    private float CalculateStandard()
    {
        //깡스탯, 증가%, 감소%
        float sumFlat = 0f;
        float sumInc = 0f;
        float sumDec = 0f;

        //종류별로 합산
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];
            if (mod.Type == StatModType.Flat) sumFlat += mod.Value;
            else if (mod.Type == StatModType.PercentAdd) sumInc += mod.Value;
            else if (mod.Type == StatModType.PercentMult) sumDec += mod.Value;
        }

        //02.20 변경, 캬 한줄컷
        //퍼센트 곱연산 수행 후, 마지막 깡스탯 더함(깡스탯 자체가 사라질 가능성 있음)
        float result = (BaseValue * (1f + sumInc) * (1f - sumDec)) + sumFlat;

        //소수점 내림
        return Mathf.Max(result, 0f);
    }

    //Delay: 공속, 리스폰
    //02.20 변경: 기본값 * (1 - 단축버프%합 + 증가디버프%합)
    private float CalculateDelay()
    {
        float sumInc = 0f;     //딜레이 단축 버프
        float sumDec = 0f;      //딜레이 증가 디버프

        //종류별로 합산
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];
            if (mod.Type == StatModType.PercentAdd) sumInc += mod.Value;
            else if (mod.Type == StatModType.PercentMult) sumDec += mod.Value;
        }


        float result = BaseValue * (1f - sumInc + sumDec);

        //0초 딜레이 방지를 위해 0.1초 강제(나중에 최소값 리팩토링)
        //공속, 리스폰 두 최소값이 다를 경우 계산식 자체를 나눌 예정
        return Mathf.Max(result, 0.1f);
    }

    //받피감, 스킬쿨감 계산식 추가
    //순수 퍼센트 수치 합산(Base 0)
    private float CalculateAdditive()
    {
        float sumFlat = 0f;

        //깡스탯 먼저
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];

            //쿨감/받피감의 경우, "20% 쿨감 증강"은 Value = 0.2 인 Flat 타입으로 들어온다고 가정
            //얘도 리팩토링 예상됨 테이블값 보고 정하기
            if (mod.Type == StatModType.Flat) sumFlat += mod.Value;
        }

        return BaseValue + sumFlat;
    }


    //아래부터 수정자 추가/제거 메서드

    //새로운 스탯 변동을 적용, 아이템이나 버프
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _isDirty = true;        //값이 더러워졌으니 다시 계산
    }

    //기존 스탯 변동을 제거하는 메서드, Source 비교
   public bool RemoveModifier(object source)
    {
        //source와 일치하는 모든 효과 제거
        int removed = _modifiers.RemoveAll(x => x.Source == source);
        if (removed > 0)
        {
            _isDirty = true; //재계산
            return true;
        }

        return false;
    }

    //청소, 모든 수정자 제거
    public void ClearModifiers()
    {
        _modifiers.Clear();
        _isDirty = true;
    }
}
