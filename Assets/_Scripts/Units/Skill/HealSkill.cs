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
                // 자신은 제외하며, 체력이 100% 미만인 경우
                if (unit != _cachedUnit && unit.CurrentHealth < unit.MaxHealth)
                {
                    if (_data.areaOfEffect)
                    {
                        // 광역 힐이면 다친 아군이 1명이라도 있으면 즉시 사용 조건 통과
                        return true;
                    }
                    else
                    {
                        // 단일 힐이라면, 체력 비율이 가장 낮은 아군을 탐색하여 _targetAlly에 캐싱
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
        }

        return canCast;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);

        List<UnitBase> targetsToHeal = new List<UnitBase>();

        if (_data.areaOfEffect)
        {
            // 광역 힐: 다시 한 번 범위 내 다친 아군을 수집
            int hitCount = Physics.OverlapSphereNonAlloc(
                _cachedUnit.transform.position,
                _data.range,
                _hitColliders,
                _cachedUnit.AllyLayer
                );
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitColliders[i].TryGetComponent(out UnitBase unit) )
                {
                    targetsToHeal.Add(unit);
                }
            }
        }
        else
        {
            // 단일 힐: UsingConditionCheck에서 찾은 대상이 아직 유효한지(파괴되지 않았는지) 확인
            if (_targetAlly != null)
            {
                targetsToHeal.Add(_targetAlly);
            }
        }

        // 도트 힐(duration > 0)인지 단발 힐인지 판단하여 실행
        if (_data.duration > 0 && _data.interval > 0)
        {
            _cachedUnit.StartCoroutine(DoTHealRoutine(targetsToHeal));
        }
        else
        {
            InstantHeal(targetsToHeal);
        }
    }

    // 단발 힐 처리 로직
    private void InstantHeal(List<UnitBase> targets)
    {
        foreach (var target in targets)
        {
            if (target != null)
            {
                target.TakeHeal(_data.recoveryAmount);

                // RPC로 이펙트 출력 요청 (NetworkObject의 Id를 넘겨 타겟 식별)
                if (target.Object != null)
                {
                    _cachedUnit.RPC_PlayChildSkillEffect(target.Object.Id, _data.skillType, true, 3f);
                }
            }
        }
    }

    // 도트 힐 (Heal Over Time) 코루틴
    private IEnumerator DoTHealRoutine(List<UnitBase> targets)
    {
        float elapsed = 0f;
        int tickCount = Mathf.FloorToInt(_data.duration / _data.interval);
        float healPerTick = _data.recoveryAmount / tickCount; // 총 회복량을 틱당 회복량으로 분할
        WaitForSeconds delay = new WaitForSeconds(_data.interval);

        while (elapsed < _data.duration)
        {
            foreach (var target in targets)
            {
                // 도트 힐 도중 타겟이 파괴될 수 있으므로 null 체크 필수
                if (target != null && target.CurrentHealth < target.MaxHealth)
                {
                    target.TakeHeal(healPerTick);

                    if (target.Object != null)
                    {
                        _cachedUnit.RPC_PlayChildSkillEffect(target.Object.Id, _data.skillType, true, _data.duration);
                    }
                }
            }

            yield return delay;
            elapsed += _data.interval;
        }
    }
}
