using System.Collections;
using Fusion;
using UnityEngine;

public class ProjectileSkill : BaseSkill<ProjectileSkillSO>
{
    private bool _castingEnd = false;

    private Coroutine _fireCoroutine;
    private Vector3 _targetPosition;

    public ProjectileSkill(ProjectileSkillSO data, UnitController unit) : base(data, unit)
    {
        string heroId = unit.HeroId;
    }

    public override bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;
        if (_data.skillVFX == null || _cachedUnit.firePoint == null) return false;
        if (_cachedUnit.currentTarget == null) return false;


        //4.2 여현구 추가
        //사거리검사
        Vector3 diff = _cachedUnit.currentTarget.transform.position - _cachedUnit.transform.position;

        //평타면 실시간, 스킬이면 원본데이터
        float currentRange = (_data.skillType == SkillType.NormalAttack) ? _cachedUnit.attackRange : _data.range;

        //제곱
        if (diff.sqrMagnitude > currentRange * currentRange)
        {
            return false;
        }

        // 스킬을 시전하기로 한 녀석의 위치를 저장해둠. (타겟이 사라져도 그 자리에 쏘도록 하기 위해)
        _targetPosition = _cachedUnit.currentTarget.transform.position;
        return true;
    }

    public override void Casting()
    {
        if (!_cachedUnit.HasStateAuthority) return;
        if (_data.skillVFX == null || _cachedUnit.firePoint == null) return;

        _phase = SkillPhase.Casting;
        _castingEnd = false;
        // 기존 코루틴이 있다면 정지 후 새로 시작 (UnitController를 통해 실행)
        if (_fireCoroutine != null)
            _cachedUnit.StopCoroutine(_fireCoroutine);

        _fireCoroutine = _cachedUnit.StartCoroutine(FireRoutine());
    }

    public override void Tick() 
    {
        // FSM 기반으로 선딜레이 -> 캐스팅 -> 후딜레이 순으로 타이머를 설정하며 순차 실행
        switch (_phase)
        {
            case SkillPhase.PreDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    Casting();
                break;

            case SkillPhase.Casting:
                if (_castingEnd)
                    PostDelay();
                break;

            case SkillPhase.PostDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    _phase = SkillPhase.Idle;
                break;
        }
    }

    private IEnumerator FireRoutine()
    {
        // 연발 횟수 (최소 1번)
        int repeatCount = _data.repeatingFire > 0 ? _data.repeatingFire : 1;

        for (int i = 0; i < repeatCount; i++)
        {

            _cachedUnit.RPC_FireProjectile(
                _cachedUnit.Object.Id,
                _data.skillType,
                _targetPosition,
                _cachedUnit.AttackPower * _data.damageRatio
                );

            // 마지막 발사가 아니면 간격만큼 대기
            if (i < repeatCount - 1)
            {
                yield return new WaitForSeconds(_data.interval);
            }
        }
        _castingEnd = true;
    }
}
