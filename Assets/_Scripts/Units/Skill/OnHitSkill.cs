using System.Collections;
using Fusion;
using UnityEngine;

public class OnHitSkill : BaseSkill<OnHitSkillSO>
{
    private int _normalAttackCounter = 0;
    private bool _onReady = false;
    private bool _yetShoot = false;
    private float _originDamage;
    private float _originRange;
    private GameObject _originVFX;
    private ProjectileSkillSO _projectileSO;
    private BaseSkillSO _duplicateSO;

    public OnHitSkill(OnHitSkillSO data, UnitController unit) : base(data, unit)
    {
        _duplicateSO = ScriptableObject.Instantiate(_cachedUnit.normalAttack.Data);
        _cachedUnit.normalAttack.ChangeData(_duplicateSO);
    }

    public override void Initialize() 
    {
        if (_cachedUnit.AttackState != null)
        {
            _cachedUnit.AttackState.OnNormalAttack = null;
            _cachedUnit.AttackState.OnNormalAttack += NormalAttackCount;
        }
    }

    public override bool UsingConditionCheck()
    {
        if (_onReady == true && _yetShoot == false)
        {
            _yetShoot = true;
            return true;
        }
        else return false;
    }

    public override void Casting()
    {
        // 스킬 시전시 평타 공격을 가져와 데이터 값을 바꿈. (투사체, 데미지 등)
        if (_duplicateSO != null && _duplicateSO is ProjectileSkillSO)
        {
            _phase = SkillPhase.Casting;

            _projectileSO = _duplicateSO as ProjectileSkillSO;

            // 원래 데미지 저장하고 추가 스킬 계수 더하기
            _originDamage = _projectileSO.damageRatio;
            _projectileSO.damageRatio += _data.additionalDamageRatio;

            // 원래 사거리 저장하고 추가 사거리 계수 더하기
            _originRange = _projectileSO.range;
            _projectileSO.range += _data.additionalRange;

            // 원래 이펙트 저장하고 바뀐 이펙트 적용하기
            _originVFX = _projectileSO.skillVFX;
            _projectileSO.skillVFX = _data.skillVFX;

            _cachedUnit.networkedOnHit = true;
            Debug.Log("평타 강화됨.");
        }
        else
            Debug.Log("OnHit 스킬 시전 실패");

        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, 0f);
    }

    private void NormalAttackCount() 
    {
        // 스킬 시전 조건이 true가 되고 다음 평타를 쏘면 아래 메서드로 들어간다.
        // 이미 강화된 평타는 날린 상태고 여기선 다시 원상 복구를 시키고 레디를 false로 바꿈.
        if (_onReady == true && _yetShoot == true)
        {
            _onReady = false;
            _yetShoot = false;
            _cachedUnit.StartCoroutine(DelayRestore());
            return;
        }

        // 5발 마다 쏘는거면 4발째 쏘는 순간이 스킬 시전 조건이다.
        _normalAttackCounter++;
        if (_normalAttackCounter >= _data.onhitPerAttack - 1)
        {
            _onReady = true;
            _yetShoot = false;
            _normalAttackCounter = 0;
        }
    }

    private IEnumerator DelayRestore()
    {
        yield return new WaitForSeconds(0.3f);
        _projectileSO.damageRatio = _originDamage;
        _projectileSO.range = _originRange;
        _projectileSO.skillVFX = _originVFX;
        _cachedUnit.networkedOnHit = false;
        // 탐색 모드로 전환
        _cachedUnit.StateMachine.ChangeState(_cachedUnit.DetectState);
        Debug.Log("평타 원상복구됨.");
    }
}
