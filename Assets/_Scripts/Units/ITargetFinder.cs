// 탐지 기능 인터페이스로 분리
// 힐러 가능성 염두 -> 적을 탐지할 지, 아군을 탐지할 지 등

using UnityEngine;

public interface ITargetFinder
{
    public float SearchRange { get; }
    public LayerMask TargetLayer { get; }
    public float SearchInterval { get; }

    public UnitBase FindTarget();
}
