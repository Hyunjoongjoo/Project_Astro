using Fusion;
using UnityEngine;
using DG.Tweening;

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

        Debug.Log($"<color=yellow>[SkillFail]</color> 거리 부족: 현재 {dist} / 필요 {_data.SkillRange}");
        return dist <= _data.SkillRange;//기획서에 맞게 스킬범위를 새로 지정

    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        Debug.Log($"[Skill Execute] {caster.name} skill 실행 | EffectPrefab: {runtime.EffectPrefab}");
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        UnitBase target = GetAssaultBaseTarget(caster);
        if (target == null || target.IsDead)
        {
            return false;
        }

        //워프 위치 계산 (타겟 정중앙)
        Vector3 warpPos = target.transform.position;
        warpPos.y = caster.transform.position.y; //지면 보정

        //워프
        caster.transform.position = warpPos;
        caster.ForceStopMoveForSkill();

        LayerMask targetMask = caster.TargetLayer;

        Collider[] hits = Physics.OverlapSphere(warpPos, runtime.Radius, targetMask);

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            float damage = caster.AttackPower * runtime.DamageMultiplier;
            Debug.Log($"<color=red>[SkillExecute]</color> Corsair 스킬 발동! 중심지: {warpPos}, 예상 데미지: {damage}");

            unit.TakeDamage(damage);
        }

        //이펙트
        caster.RPC_PlaySkillEffect(warpPos, Quaternion.identity);

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


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.Radius);
    }
#endif
}
