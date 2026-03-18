using Fusion;
using System;
using UnityEngine;

// 이동하는 유닛과 건물 모두에게 공통된 속성 정의
public abstract class UnitBase : NetworkBehaviour
{
    [Header("범위 체크용 빨간 원")]
    [SerializeField] private float _testRange;

    [Header("사망 이펙트")]
    [SerializeField] protected GameObject deathEffectPrefab;
    [SerializeField] protected float deathEffectLifeTime = 0.5f;

    [Header("체력과 방어력 설정")]
    [Networked] public float MaxHealth { get; set; }
    protected float deffense;

    protected UnitStat _unitStat;

    //protected float currentHealth;

    // 미니언이 죽었을 때 경험치 증가
    // 서브 타워 파괴시 미니언 웨이브 강화
    // 메인 타워 파괴시 게임 종료 등 Die 시 다형성을 위한 유닛 타입
    protected UnitType unitType;

    protected NetworkObject selfNetworkObj;

    public Team team;
    [Networked] public Team networkedTeam { get; set; }
    public UnitType UnitType => unitType;
    public bool IsDead { get; protected set; }

    [Networked, HideInInspector, OnChangedRender(nameof(OnHealthChanged))]
    public float CurrentHealth { get; set; }

    // 죽었을 때 이벤트를 알리며 자신의 타입을 알림
    public event Action<UnitBase> OnDeath;

    public override void Spawned()
    {
        BaseUnitInit();
    }

    protected virtual void BaseUnitInit()
    {
        if (Object.HasStateAuthority)
        {
            selfNetworkObj = GetComponent<NetworkObject>();
            CurrentHealth = MaxHealth;
            IsDead = false;
            networkedTeam = team;
        }
    }

    public override void FixedUpdateNetwork() { }

    // 매개변수 값은 아무 계산도 안한 순수 데미지 초기값
    // 여기서 최종 받는 데미지를 계산한다.
    public virtual void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return;

        float finalTakenDamage = amount;

        // 받피감이 1을 넘어가면 체력이 오히려 찰 것.
        // Config 테이블에서 상한 값 확인해봤는데 뭔가 이상함.
        // TODO: 상한 값 검사 및 조정 로직 필요.
        // TableManager.Instance.ConfigTable.Get("min_hero_damage_reduce");
        if (_unitStat != null)
        {
            //finalTakenDamage *= (1 + _unitStat.DamageReduction.Value);
            float modify = _unitStat.DamageReduction.Value;

            var config = TableManager.Instance.ConfigTable.Get("min_hero_damage_reduce");
            float minModify = 0f;

            if (config != null)
            {
                float.TryParse(config.configValue, out minModify);
            }

            modify = Mathf.Max(modify, minModify);

            finalTakenDamage *= (1f + modify);
        }

        CurrentHealth = Mathf.Max(CurrentHealth - finalTakenDamage, 0f); ;

        if (CurrentHealth <= 0f)  // 1 미만인 이유는 float이라 가끔 0.0000..1 로 살아있을 수 있음
            Die();
    }

    public virtual void TakeHeal(float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }


    public virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;

        OnDeath?.Invoke(this);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxList.DestroySound);

        if (Object.HasStateAuthority == true)
        {
            ObjectContainer.Instance.IncreaseAugmentGauge(team, unitType);

            Runner.Despawn(Object);
        }      
    }

    // 체력이 변했을 때 모든 클라이언트에서 실행될 콜백
    public void OnHealthChanged()
    {
        if (CurrentHealth <= 0 || IsDead) return;

        // 모든 클라이언트의 화면에서 HP바 매니저 호출
        if (HpBarManager.Instance != null)
        {
            HpBarManager.Instance.OnUnitDamaged(transform, networkedTeam, CurrentHealth, MaxHealth, unitType);
        }
    }

    //사망 이펙트
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!hasState) return;
        if (deathEffectPrefab != null)
        {
            GameObject deathEffectObject = Instantiate(
                deathEffectPrefab,
                transform.position,
                transform.rotation
            );

            Destroy(deathEffectObject, deathEffectLifeTime);
        }
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _testRange);
        if (_unitStat != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _unitStat.DetectRange.Value);
        }
    }
#endif
}
