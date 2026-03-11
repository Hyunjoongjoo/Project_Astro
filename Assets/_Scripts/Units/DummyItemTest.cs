using Fusion;
using UnityEngine;

public class DummyItemTest : NetworkBehaviour
{
    private HeroController _hero;
    private UnitStat _unitStat;

    void Start()
    {
        _hero = GetComponent<HeroController>();
        _unitStat = GetComponent<UnitStat>();
    }

    void Update()
    {
        if (!Object.HasStateAuthority) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("더미 아이템 사용 -> 이동속도증가 15%");

            _unitStat.AddModifier(
                EffectType.IncreaseMoveSpeed,
                new StatModifier(0.15f, StatModType.PercentAdd, this)
            );
            Debug.Log("MoveSpeed : " + _hero.MoveSpeed);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("더미 아이템 사용 -> 탐지범위증가 15%");

            _unitStat.AddModifier(
                EffectType.IncreaseDetectionRange,
                new StatModifier(0.15f, StatModType.PercentAdd, this)
            );
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("더미 아이템 사용 -> 받피증 30%");

            _unitStat.AddModifier(
                EffectType.IncreaseDamageTaken,
                new StatModifier(0.3f, StatModType.Flat, this)
            );
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("더미 아이템 사용 -> 받피감 30%");

            _unitStat.AddModifier(
                EffectType.DecreaseDamageTaken,
                new StatModifier(-0.3f, StatModType.Flat, this)
            );
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("더미 아이템 사용 -> 공격력증가");
            _unitStat.AddModifier(EffectType.IncreaseAttackPower,
                new StatModifier(10f, StatModType.Flat, this)
            );

            Debug.Log($"[test] 공격력 증가 적용 → 현재 공격력: {_hero.AttackPower}");
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("더미 아이템 사용 -> 힐량증가");

            _unitStat.AddModifier(
                EffectType.IncreaseHealPower,
                new StatModifier(100f, StatModType.Flat, this)
            );
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            _unitStat.RemoveModifier(EffectType.IncreaseAttackPower,this);
            _unitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);
            Debug.Log("더미 아이템 효과 제거");
        }
    }
}
