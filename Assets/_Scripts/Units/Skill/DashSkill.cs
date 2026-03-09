using System.Collections;
using Fusion;
using UnityEngine;

public class DashSkill : ISkill
{
    private DashSkillSO _data;
    private bool _isCasting;
    private UnitController _cachedUnit;
    private TickTimer _skillCooldown;

    private Collider[] _hitColliders;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public DashSkill(DashSkillSO data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);

        // 광역화 옵션 켜져있다면 배열 미리 할당
        if (data.areaOfEffect)
            _hitColliders = new Collider[20];
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is DashSkillSO dashData)
        {
            _data = dashData;
            if (_data.areaOfEffect && _hitColliders == null)
                _hitColliders = new Collider[20];
        }
        else
            Debug.LogWarning($"[DashSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (_cachedUnit.currentTarget == null) return false;
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;
        
        // 사거리 체크: 대상과의 거리가 최소~최대 거리 범위 안에 있는지 확인
        float distanceToTarget = Vector3.Distance(_cachedUnit.transform.position, _cachedUnit.currentTarget.transform.position);
        if (distanceToTarget >= _data.canDashMinDistance && distanceToTarget <= _data.canDashMaxDistance)
        {
            // 사용 조건을 만족하면 쿨다운을 돌리고 true 반환
            _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);
            return true;
        }

        return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        if (_cachedUnit.currentTarget == null) return;

        // 코루틴을 통해 목표 지점까지 이동하는 로직 실행
        _cachedUnit.StartCoroutine(DashRoutine(_cachedUnit.currentTarget));
    }

    private IEnumerator DashRoutine(UnitBase target)
    {
        // 대상이 사라질 경우 종료
        if (target == null) yield break;

        bool hasReached = false;

        // 도착 임계값 (적과 딱 붙을 수가 없으므로 어느 정도 거리 둠)
        float reachThreshold = 1.8f;

        while (!hasReached)
        {
            if (target == null) yield break;

            Vector3 targetPos = target.transform.position;

            // 현재 위치에서 타겟 위치로 델타 타임에 맞춰 이동
            _cachedUnit.transform.position = Vector3.MoveTowards(
                _cachedUnit.transform.position,
                targetPos,
                _data.dashSpeed * Time.deltaTime
            );

            // 타겟과의 거리가 임계값보다 작아지면 도착으로 판정
            if (Vector3.Distance(_cachedUnit.transform.position, targetPos) <= reachThreshold)
                hasReached = true;

            yield return null; // 다음 프레임까지 대기
        }

        // 도착 후 공격 판정 (데미지 계수가 0보다 클 때만)
        if (_data.damageRatio > 0)
            ApplyDamage(target);
    }

    private void ApplyDamage(UnitBase mainTarget)
    {
        float finalDamage = _cachedUnit.AttackPower * _data.damageRatio;

        if (_data.areaOfEffect)
        {
            // 광역 피해 처리
            int hitCount = Physics.OverlapSphereNonAlloc(
                _cachedUnit.transform.position,
                _data.attackRange,
                _hitColliders,
                _cachedUnit.TargetLayer
                );
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitColliders[i].TryGetComponent(out UnitBase enemy))
                {
                    _cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, _data.skillType, false, 1.5f);
                    // 네트워크 환경: 마스터 클라이언트에서만 데미지 연산 수행
                    if (_cachedUnit.Runner.IsSharedModeMasterClient)
                        enemy.TakeDamage(finalDamage);
                }
            }
        }
        else
        {
            // 단일 피해 처리
            if (mainTarget != null)
            {
                if (_cachedUnit.Runner.IsSharedModeMasterClient)
                    mainTarget.TakeDamage(finalDamage);
            }
        }
    }
}
