using Fusion;
using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// 이동하는 유닛과 건물 모두에게 공통된 속성 정의
public abstract class UnitBase : NetworkBehaviour
{
    [Header("체력과 방어력 설정")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float deffense;

    //protected float currentHealth;

    // 미니언이 죽었을 때 경험치 증가
    // 서브 타워 파괴시 미니언 웨이브 강화
    // 메인 타워 파괴시 게임 종료 등 Die 시 다형성을 위한 유닛 타입
    protected UnitType unitType;

    protected NetworkObject selfNetworkObj;

    public Team team;
    [Networked] public Team networkedTeam {  get;  set; }

    public float MaxHealth => maxHealth;
    public UnitType UnitType => unitType;
    public bool IsDead { get; private set; }
    [Networked, HideInInspector, OnChangedRender(nameof(OnHealthChanged))] public float CurrentHealth { get; set; }
    [Networked, HideInInspector] public UnitState CurrentState { get; set; }

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
            CurrentHealth = maxHealth;
            IsDead = false;
            networkedTeam = team;
        }
    }

    public override void FixedUpdateNetwork() { }

    public virtual void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0); ;

        if (CurrentHealth < 1)  // 1 미만인 이유는 float이라 가끔 0.0000..1 로 살아있을 수 있음
            Die();
    }

    public virtual void TakeHeal(float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }

    
    public virtual float GetAttackDistanceTo(UnitBase target)
    {
        if (target == null)//타겟이 없는 경우 항상 사거리 밖으로 판정되도록 처리
        {
            return float.MaxValue;
        }

        //자신과 타겟의 콜라이더를 기준으로 실제 전투 거리 계산
        Collider myCollider = GetComponentInChildren<Collider>();
        Collider targetCollider = target.GetComponentInChildren<Collider>();

        if (myCollider == null || targetCollider == null)//콜라이더가 없는 경우 중심점 거리 계산으로
        {
            return Vector3.Distance(transform.position, target.transform.position);
        }

        Vector3 myPoint = myCollider.ClosestPoint(target.transform.position);
        Vector3 targetPoint = targetCollider.ClosestPoint(transform.position);

        return Vector3.Distance(myPoint, targetPoint);
    }

    public virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;

        CurrentState = UnitState.Dead;
        OnDeath?.Invoke(this);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxList.DestroySound);

        if (Object.HasStateAuthority == true)
            ObjectContainer.Instance.IncreaseAugmentGauge(team, unitType);

        Runner.Despawn(selfNetworkObj);
    }

    // 체력이 변했을 때 모든 클라이언트에서 실행될 콜백
    public void OnHealthChanged()
    {
        if (CurrentHealth <= 0 || IsDead) return;

        // 모든 클라이언트의 화면에서 HP바 매니저 호출
        if (HpBarManager.Instance != null)
        {
            HpBarManager.Instance.OnUnitDamaged(transform, networkedTeam, CurrentHealth, maxHealth);
        }
    }
}
