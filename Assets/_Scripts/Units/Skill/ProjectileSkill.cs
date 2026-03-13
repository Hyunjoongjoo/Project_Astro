using System.Collections;
using Fusion;
using UnityEngine;

public class ProjectileSkill : ISkill
{
    private ProjectileSkillSO _data;
    private bool _isCasting;

    private UnitController _cachedUnit;
    private Coroutine _fireCoroutine;
    private TickTimer _skillCooldown;
    private int _attackCounter;//공격 카운트 추가
    private Collider[] _searchColliders = new Collider[20];//타겟 검색용 배열

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ProjectileSkill(ProjectileSkillSO data, UnitController unit)
    {
        //string heroId = unit.HeroId;
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ProjectileSkillSO projectileData)
            _data = projectileData;
        else
            Debug.LogWarning($"[ProjectileSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;
        if (_data.skillVFX == null || _cachedUnit.firePoint == null) return false;
        if (_cachedUnit.currentTarget == null) return false;

        if (Vector3.Distance(_cachedUnit.transform.position, _cachedUnit.currentTarget.transform.position) > _data.range)
            return false;

        return true;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        if (_data.skillVFX == null || _cachedUnit.firePoint == null) return;
        if (_cachedUnit.currentTarget == null) return;

        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);

        // 기존 코루틴이 있다면 정지 후 새로 시작 (UnitController를 통해 실행)
        if (_fireCoroutine != null)
            _cachedUnit.StopCoroutine(_fireCoroutine);

        _fireCoroutine = _cachedUnit.StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        // 연발 횟수 (최소 1번)
        int repeatCount = _data.repeatingFire > 0 ? _data.repeatingFire : 1;

        for (int i = 0; i < repeatCount; i++)
        {
            // 도중에 타겟이 사라지면 중단
            if (_cachedUnit.currentTarget == null || _cachedUnit.currentTarget.IsDead) break;

            _cachedUnit.RPC_FireProjectile(
                _cachedUnit.Object.Id,
                _data.skillType,
                _cachedUnit.currentTarget.transform.position,
                _cachedUnit.AttackPower
                );

            // 마지막 발사가 아니면 간격만큼 대기
            if (i < repeatCount - 1)
            {
                yield return new WaitForSeconds(_data.interval);
            }
        }
    }

    private UnitBase FindFarthestTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
         _cachedUnit.transform.position,
         _data.range,
         _searchColliders,
         _cachedUnit.TargetLayer
     );

        UnitBase bestHero = null;
        UnitBase bestUnit = null;

        float farHeroDist = -1f;
        float farUnitDist = -1f;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _searchColliders[i];

            if (!col.TryGetComponent(out UnitBase enemy)) continue;
            if (enemy == _cachedUnit) continue;
            if (enemy.IsDead) continue;

            float dist = (_cachedUnit.transform.position - enemy.transform.position).sqrMagnitude;

            // 영웅 우선
            if (enemy is HeroController)
            {
                if (dist > farHeroDist)
                {
                    farHeroDist = dist;
                    bestHero = enemy;
                }
            }
            else
            {
                if (dist > farUnitDist)
                {
                    farUnitDist = dist;
                    bestUnit = enemy;
                }
            }
        }

        // 영웅이 있으면 영웅
        if (bestHero != null)
            return bestHero;

        // 없으면 유닛
        return bestUnit;
    }

    public void RegisterBasicAttack()
    {
        if (!_cachedUnit.Object.HasStateAuthority) return;

        _attackCounter++;

        if (_attackCounter >= _data.attackCount)
        {
            _attackCounter = 0;

            UnitBase target = FindFarthestTarget();

            if (target == null || target.IsDead) return;


            _cachedUnit.RPC_FireProjectile(
                _cachedUnit.Object.Id,
                _data.skillType,
                target.transform.position,
                _cachedUnit.AttackPower
            );
        }
    }
}
