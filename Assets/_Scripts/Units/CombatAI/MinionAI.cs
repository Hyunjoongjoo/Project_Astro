using UnityEngine;
using Fusion;

/// <summary>
/// 미니언 자동 전투 AI
/// </summary>
public class MinionAI : BaseAutoBattleAI
{
    [Header("Advance")]
    [SerializeField] private Transform _enemyBase; //전진 목표

    //미니언 전용 데이터 추가 예정


    //public override void Spawned() //네트워크 연결전이라 주석처리
    //{
    //    base.Spawned();

    //    //if (!Object.HasStateAuthority)
    //    //{
    //    //    return;
    //    //}

    //    //전진 목표 설정
    //    if (_enemyBase != null)
    //    {
    //        advancePoint = _enemyBase.position;
    //    }
    //}


    protected override void UpdateCombat()
    {
        if (!IsTargetValid())
        {
            ChangeState(AutoBattleState.Advance);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > controller.AttackRange)
        {
            MoveTo(currentTarget.position);
            return;
        }

        StopMove();
        controller.Attack(currentTarget);
    }
}

    //전투상태로 들어갈수있는지 판단하는 메서드 추가 예정
    //}
