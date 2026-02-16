using System;
using UnityEngine;

// 이동하는 유닛과 건물 모두에게 공통된 속성 정의
public abstract class UnitBase : MonoBehaviour
{
    [SerializeField] protected float _maxHealth;
    [SerializeField] protected float _currentHealth;
    [SerializeField] protected float _deffense;

    protected Team team;

    // 미니언이 죽었을 때 경험치 증가
    // 서브 타워 파괴시 미니언 웨이브 강화
    // 메인 타워 파괴시 게임 종료 등 Die 시 다형성을 위한 유닛 타입
    protected UnitType unitType;

    // 죽었을 때 이벤트를 알리며 자신의 타입을 알림
    public event Action<UnitBase> OnDeath;

    public virtual void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
