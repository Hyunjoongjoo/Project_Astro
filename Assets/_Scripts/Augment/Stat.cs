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

            case StatCalcMode.SimpleSum:
                return CalculateSimpleSum(); //실드, 탐지범위

            case StatCalcMode.Individual:
                return CalculateIndividual(); //공속, 쿨감
        }
        return BaseValue;
    }

    //공식 1~3
    //각 계산식별 로직 적용해주기

    //Standard, 체력, 공격력, 치유력, 이속
    //증가% 합, 감소% 합 마지막에 곱셈
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
        

        //공식: (기본값 + 깡스탯) * (1 + 증가%총합) * (1 - 감소%총합)
        float finalBase = BaseValue + sumFlat;
        float totalInc = 1f + sumInc;
        float totalDec = 1f - sumDec;

        float result = finalBase * totalInc * totalDec;

        //소수점 내림
        return Mathf.Max(Mathf.Floor(result), 1f);
    }

    //단순 합연산
    //실드, 탐지 범위, 받피감
    private float CalculateSimpleSum()
    {
        float sumFlat = 0f;
        float sumInc = 0f;

        //종류별로 합산
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];
            if (mod.Type == StatModType.Flat) sumFlat += mod.Value;
            else if (mod.Type == StatModType.PercentAdd) sumInc += mod.Value;
        }

        //공식: (기본값 + 깡스탯) * (1 + 증가%총합)
        float result = (BaseValue + sumFlat) * (1f + sumInc);
        return Mathf.Max(Mathf.Floor(result), 0f); //실드 0 이하로 안 내려가게
    }

    //개별 복리형, 100% 쿨감 등 밸런싱 버그 막기
    //공속, 쿨감, 리스폰
    private float CalculateIndividual()
    {
        float result = BaseValue;
        
        //깡스탯 먼저
        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Type == StatModType.Flat)
                result += _modifiers[i].Value;
        }

        //남은 % 순서대로 곱하기
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];
            if (mod.Type != StatModType.Flat)
            {
                //감소는 0.2가 들어오면 (1 - 0.2) = 0.8을 곱해줌
                float multiplier = (mod.Type == StatModType.PercentMult) ? (1f - mod.Value) : (1f + mod.Value);
                result *= multiplier;
            }
        }

        return result;
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
