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

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("더미 아이템 사용 -> 공격력증가");
            _unitStat.AddModifier(EffectType.IncreaseAttackPower,
                new StatModifier(10f, StatModType.Flat, this)
            );

            Debug.Log($"[test] 공격력 증가 적용 → 현재 공격력: {_hero.AttackPower}");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("더미 아이템 사용 -> 받피감 30%");

            _unitStat.AddModifier(
                EffectType.DecreaseDamageTaken,
                new StatModifier(-0.3f, StatModType.Flat, this)
            );
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            _unitStat.RemoveModifier(EffectType.IncreaseAttackPower,this);
            _unitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);
            Debug.Log("더미 아이템 효과 제거");
        }
    }
}
