using DG.Tweening;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using UnityEngine;

public class CorsairSkill : MonoBehaviour, IHeroSkill
{
    [SerializeField] private CorsairSkillSO _data;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        if (caster.CurrentTarget == null)
        {
            return false;
        }

        float dist = caster.GetAttackDistanceTo(caster.CurrentTarget);

        return dist <= _data.SkillRange;//기획서에 맞게 스킬범위를 새로 지정

    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        UnitBase target = GetAssaultBaseTarget(caster);
        if (target == null || target.IsDead)
        {
            return false;
        }

        //이동 방향 계산
        Vector3 dir = target.transform.position - caster.transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
        }
        else
        {
            dir = caster.transform.forward;
        }

        //워프 위치 계산, 살짝 앞에 위치하도록 보정
        Vector3 center = target.transform.position - dir * 1.2f;
        center.y = caster.transform.position.y; //지면 보정

        caster.StopMove();

        //워프
        if (caster.Agent != null)
        {
            caster.Agent.Warp(center);
            caster.Agent.ResetPath();
        }
        else
        {
            caster.transform.position = center;
        }


        LayerMask targetMask = caster.TargetLayer;

        Collider[] hits = Physics.OverlapSphere(center, runtime.Radius, targetMask);

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null || unit.IsDead || unit == caster || unit.team == caster.team)
            {
                continue;
            }

            float damage = caster.AttackPower * runtime.DamageMultiplier;

            unit.TakeDamage(damage);
        }

        //이펙트
        //caster.RPC_PlaySkillEffect(center, Quaternion.identity);

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is CorsairSkillSO assaultData)
        {
            _data = assaultData;
        }
    }

    private UnitBase GetAssaultBaseTarget(HeroController caster)
    {
        //현재 전투 타겟 우선
        if (caster.CurrentTarget != null && !caster.CurrentTarget.IsDead)
        {
            return caster.CurrentTarget;
        }

        return null;
    }

    public void TickSkill(NetworkRunner runner) { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.Radius);
    }
#endif
}
