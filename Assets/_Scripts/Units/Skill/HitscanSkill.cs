using Fusion;
using UnityEngine;

public class HitscanSkill : ISkill
{
    private HitscanSkillSO _data;
    private UnitController _unit;

    private TickTimer _cooldown;

    public BaseSkillSO Data => _data;

    public bool IsCasting => false;

    public HitscanSkill(HitscanSkillSO data, UnitController unit)
    {
        _data = data;
        _unit = unit;
        _cooldown = TickTimer.CreateFromSeconds(_unit.Runner, _data.initCooldown);
    }

    public void ChangeData(BaseSkillSO newData)
    {
        _data = newData as HitscanSkillSO;
    }

    public bool UsingConditionCheck()
    {
        if (!_cooldown.ExpiredOrNotRunning(_unit.Runner)) return false;
        if (_unit.currentTarget == null) return false;
        if (_unit.currentTarget.IsDead) return false;
        if (_unit.IsDead) return false;

        Vector3 dir = _unit.currentTarget.transform.position - _unit.transform.position;

        if (dir.sqrMagnitude > _data.range * _data.range)
            return false;

        return true;
    }

    public void Initialize() { }

    public void PreDelay() { }

    public void PostDelay() { }

    public void Execute(UnitBase target)
    {
        if (_unit == null || target == null) return;
        if (target.Object == null) return;
        if (target.IsDead) return;
        if (_unit.IsDead) return;

        float damage = _unit.AttackPower * _data.damageRatio;

        if (_unit.HasStateAuthority)
        {
            target.TakeDamage(damage);
        }

        Vector3 start = _unit.transform.position;
        Vector3 dir = (target.transform.position - start).normalized;
        float distance = Vector3.Distance(start, target.transform.position);

        Debug.DrawRay(start, dir * distance, Color.cyan, 0.2f);


        _unit.RPC_PlayChildSkillEffect(
            _unit.Object.Id,
            target.Object.Id,
            _data.skillType,
            true,
            1f
        );
    }

    public void Casting()
    {
        if (!_unit.HasStateAuthority) return;
        if (_unit.currentTarget == null) return;
        float finalCooldown;
        if (_data.skillType == SkillType.NormalAttack)
        {
            finalCooldown = _unit.AttackSpeed;
        }
        else
        {
            finalCooldown = _data.cooldown;
        }
        _cooldown = TickTimer.CreateFromSeconds(_unit.Runner, finalCooldown);
        Execute(_unit.currentTarget);
    }

    public void Tick() { }
}
