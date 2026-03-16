using UnityEngine;
using System.Collections.Generic;

//영웅 부착 시 아이템 발동 조건을 감시하는 옵저버
public class HeroItemObserver : MonoBehaviour
{
    private HeroController _hero;
    private UnitStat _unitStat;
    private List<ItemEffectTracker> _trackers = new List<ItemEffectTracker>();

    //단일 효과의 상태를 추적하기 위한 내부 클래스
    private class ItemEffectTracker
    {
        public ItemEffectData Data;
        public StatModifier Modifier;
        public bool IsApplied; //현재 스탯이 적용중인지 여부
    }
    
    public void Init(HeroController hero, UnitStat unitStat, List<ItemEffectData> effects)
    {
        _hero = hero;
        _unitStat = unitStat;

        foreach (var effect in effects)
        {
            //수치 파싱
            float val = 0f;
            float.TryParse(effect.effectValue, out val);

            //Modifier 생성 (기본 Flat 타입 기준. 필요시 PA, PM 변경 가능)
            StatModifier mod = new StatModifier(val, StatModType.Flat, this);

            var tracker = new ItemEffectTracker
            {
                Data = effect,
                Modifier = mod,
                IsApplied = false
            };

            _trackers.Add(tracker);

            //패시브면 바로 부여
            if (effect.triggerCondition == TriggerCondition.Passive)
            {
                ApplyEffect(tracker);
            }
        }

    }

    private void Update()
    {
        //영웅이 죽었거나 세팅이 덜 되었다면 검사 중단
        if (_hero == null || _unitStat == null || _hero.IsDead) return;

        //현재 체력 퍼센테이지 계산
        float hpPercent = (_hero.CurrentHealth / _unitStat.MaxHp.Value) * 100f;

        //매 프레임마다 조건부 효과들을 순회하며 상태 갱신
        for (int i = 0; i < _trackers.Count; i++)
        {
            var tracker = _trackers[i];

            //패시브 패스
            if (tracker.Data.triggerCondition == TriggerCondition.Passive) continue;

            float triggerVal = 0f;
            float.TryParse(tracker.Data.triggerValue, out triggerVal);

            bool conditionMet = false;

            //조건 타입에 따른 발동 여부 검사, 지금은 이거 2개뿐인데 커지면 얘도 전략패턴 쓸 듯?
            switch (tracker.Data.triggerCondition)
            {
                case TriggerCondition.HpBelow:
                    conditionMet = hpPercent <= triggerVal;
                    break;
                case TriggerCondition.HpAbove:
                    conditionMet = hpPercent >= triggerVal;
                    break;
            }

            //조건은 달성했는데 아직 미적용 상태 => 버프 켜기
            if (conditionMet && !tracker.IsApplied)
            {
                ApplyEffect(tracker);
            }
            //조건이 깨졌는데 적용 상태 => 버프 끄기
            else if (!conditionMet && tracker.IsApplied)
            {
                RemoveEffect(tracker);
            }
        }
    }


    //AddModifier 적용
    private void ApplyEffect(ItemEffectTracker tracker)
    {
        _unitStat.AddModifier(tracker.Data.effectType, tracker.Modifier);
        tracker.IsApplied = true;
    }

    //제거
    private void RemoveEffect(ItemEffectTracker tracker)
    {
        _unitStat.RemoveModifier(tracker.Data.effectType, tracker.Modifier.Source);
        tracker.IsApplied = false;
    }

}
