using Fusion;
using UnityEngine;

public class HitscanSkill : BaseSkill<HitscanSkillSO>
{
    public HitscanSkill(HitscanSkillSO data, UnitController unit) : base(data, unit)
    {

    }
    public override bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;

        //4.2 여현구 추가
        //유효성(없어도버그는없었다함)
        if (_cachedUnit.currentTarget == null || _cachedUnit.currentTarget.IsDead) return false;
        //사거리검사
        Vector3 diff = _cachedUnit.currentTarget.transform.position - _cachedUnit.transform.position;
        //평타면 실시간, 스킬이면 원본데이터
        float currentRange = (_data.skillType == SkillType.NormalAttack) ? _cachedUnit.attackRange : _data.range;
        //제곱
        if (diff.sqrMagnitude > currentRange * currentRange)
        {
            return false;
        }
        return true;
    }

    public override void Casting()
    {
        if (!_cachedUnit.HasStateAuthority) return;

        UnitBase target = _cachedUnit.currentTarget;

        if (target == null || target.IsDead || target.Object == null)
        {
            return;
        }

        _phase = SkillPhase.Casting;

        //float finalCooldown;
        //if (_data.skillType == SkillType.NormalAttack)
        //{
        //    finalCooldown = _cachedUnit.AttackSpeed;
        //}
        //else
        //{
        //    finalCooldown = _data.cooldown;
        //}
        //_skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, finalCooldown);
        ApplyDamage(target);
        PostDelay();
    }

    private void ApplyDamage(UnitBase target)
    {
        if (_cachedUnit == null || target == null) return;
        if (target.Object == null) return;
        if (target.IsDead) return;
        if (_cachedUnit.IsDead) return;

        float damage = _cachedUnit.AttackPower * _data.damageRatio;
        // Debug.Log($"[히트스캔] → {target.name} / dmg:{damage}");
        if (_cachedUnit.HasStateAuthority)
        {
            target.TakeDamage(damage);

            //이펙트 추가 예정
            if (target.Object != null && target.Object.IsValid)
            {
                _cachedUnit.RPC_PlayHitscanEffect(_cachedUnit.Object.Id, target.Object.Id);
            }
        }
    }
}
