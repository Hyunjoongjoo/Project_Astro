using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealSkill : ISkill
{
    private HealSkillSO _data;
    private bool _isCasting;
    private UnitController _cachedUnit;
    private TickTimer _skillCooldown;

    private UnitBase _targetAlly;
    // 물리 탐색 시 메모리 할당(GC)을 줄이기 위한 NonAlloc 배열
    private Collider[] _hitColliders = new Collider[20];

    private List<UnitBase> _targetsToHeal = new List<UnitBase>(10);

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public HealSkill(HealSkillSO data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is HealSkillSO healData)
            _data = healData;
        else
            Debug.LogWarning($"[HealSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner))
            return false;

        _targetAlly = null;
        bool canCast = false;
        float minHealthRatio = 1f;

        // 자신을 중심으로 사거리 내의 모든 콜라이더 탐색
        int hitCount = Physics.OverlapSphereNonAlloc(
            _cachedUnit.transform.position,
            _data.range,
            _hitColliders,
            _cachedUnit.AllyLayer
            );

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitColliders[i].TryGetComponent(out UnitBase unit))
            {
                // 본인 제외
                if (unit == _cachedUnit)
                {
                    continue;
                }

                // 영웅만 힐 (기획 기준)
                if (unit.UnitType != UnitType.Hero)
                {
                    continue;
                }

                // 풀피면 제외
                if (unit.CurrentHealth >= unit.MaxHealth)
                {
                    continue;
                }

                if (_data.areaOfEffect)
                {
                    return true;
                }
                else
                {
                    float healthRatio = unit.CurrentHealth / unit.MaxHealth;

                    if (healthRatio < minHealthRatio)
                    {
                        minHealthRatio = healthRatio;
                        _targetAlly = unit;
                        canCast = true;
                    }
                }
            }
        }

        return canCast;
    }

    public void PreDelay() 
    {
        _cachedUnit.HeroAnimator.SetBool("IsCasting", true);
        _isCasting = true; 
    }

    public void PostDelay() 
    {
        _cachedUnit.HeroAnimator.SetBool("IsCasting", false);
        _isCasting = false; 
    }

    public void Casting()
    {
        float finalCooldown = _data.cooldown * _data.cooldownMultiplier;

        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, finalCooldown);

        //List<UnitBase> targetsToHeal = new List<UnitBase>();
        _targetsToHeal.Clear();

        if (_data.areaOfEffect)
        {
            //대상 중심 AoE
            Vector3 center = _targetAlly != null
               ? _targetAlly.transform.position
               : _cachedUnit.transform.position;

            // 광역 힐: 다시 한 번 범위 내 다친 아군을 수집
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                _data.range,
                _hitColliders,
                _cachedUnit.AllyLayer
                );
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitColliders[i].TryGetComponent(out UnitBase unit) )
                {
                    if (unit == _cachedUnit)//시전자 제외
                    {
                        continue;
                    }
                    if (unit.UnitType == UnitType.Tower || unit.UnitType == UnitType.Bridge)//타워,함교는 제외
                    {
                        continue;
                    }
                    if (unit.CurrentHealth >= unit.MaxHealth)//풀피 유닛 제외
                    {
                        continue;
                    }
                    _targetsToHeal.Add(unit);
                }
            }
        }
        else
        {
            // 단일 힐: UsingConditionCheck에서 찾은 대상이 아직 유효한지(파괴되지 않았는지) 확인
            if (_targetAlly != null)
            {
                _targetsToHeal.Add(_targetAlly);
            }
        }

        // 도트 힐(duration > 0)인지 단발 힐인지 판단하여 실행
        if (_data.duration > 0 && _data.interval > 0)
        {
            _cachedUnit.StartCoroutine(DoTHealRoutine(_targetsToHeal));
        }
        else
        {
            InstantHeal(_targetsToHeal);
        }
    }

    public void Tick() { }

    // 단발 힐 처리 로직
    private void InstantHeal(List<UnitBase> targets)
    {
        foreach (var target in targets)
        {
            if (target != null)
            {
                float beforeHp = target.CurrentHealth;
                float healAmount = _cachedUnit.HealPower * _data.healCoefficient;
                target.TakeHeal(healAmount);//치유량 * 스킬계수
                float afterHp = target.CurrentHealth;
                Debug.Log($"힐 적용 → {target.name} 힐량: {afterHp - beforeHp} | {beforeHp} → {afterHp}");
                // RPC로 이펙트 출력 요청 (NetworkObject의 Id를 넘겨 타겟 식별)
                if (target.Object != null)
                {
                    _cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, target.Object.Id, _data.skillType, true, 3f);
                }
            }
        }
    }

    // 도트 힐 (Heal Over Time) 코루틴
    private IEnumerator DoTHealRoutine(List<UnitBase> targets)
    {
        float elapsed = 0f;
        int tickCount = Mathf.FloorToInt(_data.duration / _data.interval);
        float totalHeal = _cachedUnit.HealPower * _data.healCoefficient;
        float healPerTick = totalHeal / tickCount; // 총 회복량을 틱당 회복량으로 분할
        WaitForSeconds delay = new WaitForSeconds(_data.interval);

        while (elapsed < _data.duration)
        {
            foreach (var target in targets)
            {
                if (target.Object.IsValid == false)
                    continue;

                // 도트 힐 도중 타겟이 파괴될 수 있으므로 null 체크 필수
                if (target != null && target.CurrentHealth < target.MaxHealth)
                {
                    target.TakeHeal(healPerTick);

                    if (target.Object != null)
                    {
                        _cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, target.Object.Id, _data.skillType, true, _data.duration);
                    }
                }
            }

            yield return delay;
            elapsed += _data.interval;
        }
    }
}
