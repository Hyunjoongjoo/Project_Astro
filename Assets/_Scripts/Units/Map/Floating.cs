using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Floating : MonoBehaviour
{
    [SerializeField] List<Transform> _targets = new List<Transform>();
    [SerializeField] float _moveRange = 30f;
    [SerializeField] float _moveTime = 2f;
    private void Start()
    {
        foreach (var target in _targets)
        {
            if (target == null) continue;

            // 각 타겟의 현재 로컬 Y 위치를 기준으로 루프 생성
            target.DOLocalMoveY(target.localPosition.y + _moveRange, _moveTime)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(target.gameObject);
        }
    }
}
