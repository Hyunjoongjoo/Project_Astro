using Fusion;
using UnityEngine;
using DG.Tweening;

public class AssaultSkill : NetworkBehaviour, IHeroSkill
{
    [Header("범위 설정")]
    [SerializeField] private float _radius = 3f;

    [Header("데미지 설정")]
    [SerializeField] private float _damageMultiplier = 1f;

    [Header("이펙트")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private float _effectScaleTime = 0.25f;

    //public bool BlockAttackDuringSkill => true;
    //public bool BlockMoveDuringSkill => true;

    public bool CanUse(HeroController caster)
    {
        if (caster.CurrentTarget == null)
        {
            return false;
        }

        float dist = caster.GetAttackDistanceTo(caster.CurrentTarget);

        return dist <= caster.SearchRange;//강습에 맞게 스킬 자체는 탐지범위에 걸리면 시전하도록
    }

    public void Execute(HeroController caster)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return;
        }

        UnitBase target = caster.SkillTarget;
        if (target == null)
        {
            return;
        }

        //워프 위치 계산 (타겟 정중앙)
        Vector3 warpPos = target.transform.position;
        warpPos.y = caster.transform.position.y; //지면 보정

        //워프
        caster.transform.position = warpPos;
        caster.ForceStopMoveForSkill();

        LayerMask targetMask = caster.TargetLayer;

        Collider[] hits = Physics.OverlapSphere(warpPos, _radius, targetMask);

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }

            if (unit.IsDead)
            {
                continue;
            }

            float damage = caster.AttackPower * _damageMultiplier;
            unit.TakeDamage(damage);
#if UNITY_EDITOR
            Debug.Log($"[강습!] {caster.name} -> {unit.name}, dmg={damage}");
#endif
        }

        //이펙트
        RPC_PlayEffect(warpPos);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffect(Vector3 position)
    {
        PlayEffect(position);
    }

    private void PlayEffect(Vector3 position)
    {
        if (_effectPrefab == null)
        {
            return;
        }

        GameObject effects = Instantiate(_effectPrefab, position, Quaternion.identity);

        //파티클 재생
        ParticleSystem particleSystem = effects.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
        }

        //연출 (충격파 느낌)
        effects.transform.localScale = Vector3.zero;
        effects.transform.DOScale(_radius * 2f, _effectScaleTime).SetEase(Ease.OutQuad);

        //파티클 종료 후 제거
        float lifeTime = particleSystem != null ? particleSystem.main.duration + particleSystem.main.startLifetime.constantMax : 1f;

        Destroy(effects, lifeTime);
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
#endif
}
