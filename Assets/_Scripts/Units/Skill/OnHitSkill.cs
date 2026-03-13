using Fusion;
using UnityEngine;

public class OnHitSkill : ISkill
{
    private OnHitSkillSO _data;
    private bool _isCasting;
    private UnitController _cachedUnit;

    private int _normalAttackCounter = 0;
    private bool _onReady = false;
    private GameObject _originVFX;
    private ProjectileSkillSO _projectileSO;

    public BaseSkillSO Data => _data;

    public bool IsCasting => _isCasting;

    public OnHitSkill(OnHitSkillSO data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
    }

    public void Initialize() 
    {
        if (_cachedUnit.AttackState != null)
        {
            _cachedUnit.AttackState.OnNormalAttack += NormalAttackCount;
        }
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is OnHitSkillSO onHitData)
            _data = onHitData;
        else
            Debug.LogWarning($"[OnHitSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (_onReady) return true;
        else return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        BaseSkillSO normalInfo = _cachedUnit.normalAttack.Data;
        if (normalInfo is ProjectileSkillSO)
        {
            _projectileSO = (ProjectileSkillSO)normalInfo;
            _projectileSO.damageRatio += _data.additionalDamageRatio;
            _originVFX = _projectileSO.skillVFX;
            _projectileSO.skillVFX = _data.skillVFX;
        }
        else
            Debug.Log("OnHit 스킬 시전 실패");
    }

    private void NormalAttackCount() 
    {
        // 스킬 시전 조건이 true가 되고 다음 평타를 쏘면 아래 메서드로 들어간다.
        // 이미 강화된 평타는 날린 상태고 여기선 다시 원상 복구를 시키고 레디를 false로 바꿈.
        if (_onReady)
        {
            _projectileSO.damageRatio -= _data.additionalDamageRatio;
            _projectileSO.skillVFX = _originVFX;
            _onReady = false;
            return;
        }

        // 5발 마다 쏘는거면 4발째 쏘는 순간이 스킬 시전 조건이다.
        _normalAttackCounter++;
        if (_normalAttackCounter >= _data.onhitPerCasting - 1)
        {
            _onReady = true;
            _normalAttackCounter = 0;
        }
    }
}
