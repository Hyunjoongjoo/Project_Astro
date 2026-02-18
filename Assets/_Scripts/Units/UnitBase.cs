using Fusion;
using System;
using UnityEngine;

// 이동하는 유닛과 건물 모두에게 공통된 속성 정의
public abstract class UnitBase : NetworkBehaviour
{
    [Header("체력과 방어력 설정")]
    [SerializeField] protected float _maxHealth;
    [SerializeField] protected float _deffense;

    protected float _currentHealth;

    // 미니언이 죽었을 때 경험치 증가
    // 서브 타워 파괴시 미니언 웨이브 강화
    // 메인 타워 파괴시 게임 종료 등 Die 시 다형성을 위한 유닛 타입
    protected UnitType _unitType;

    protected NetworkObject _selfNetworkObj;

    [HideInInspector] public Team team;

    [Networked, HideInInspector] public float CurrentHealth { get; set; }
    [Networked, HideInInspector] public UnitState CurrentState { get; set; }

    // 죽었을 때 이벤트를 알리며 자신의 타입을 알림
    public event Action<UnitBase> OnDeath;

    public override void Spawned()
    {
        // 팀 설정은 스폰되었을 때 공유 딕셔너리 등에서 주입
        team = GameManager.Instance.PlayerTeam;

        if (Object.HasStateAuthority)
        {
            _selfNetworkObj = GetComponent<NetworkObject>();
            _currentHealth = _maxHealth;
        }
    }

    public override void FixedUpdateNetwork() { }

    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;

        _currentHealth -= amount;

        if (_currentHealth < 1)  // 1 미만인 이유는 float이라 가끔 0.0000..1 로 살아있을 수 있음
        {
            Die();
        }
    }

    public virtual void Die()
    {
        CurrentState = UnitState.Dead;
        OnDeath?.Invoke(this);
        Runner.Despawn(_selfNetworkObj);
    }
}
