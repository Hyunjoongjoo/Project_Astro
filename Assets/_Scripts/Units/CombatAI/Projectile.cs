using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    public void Fire(Vector3 targetPos)
    {
        Debug.Log("투사체 호출됨");
        transform.DOMove(targetPos, 0.3f).SetEase(Ease.Linear).OnComplete(OnHit);
    }

    private void OnHit()
    {
        Destroy(gameObject);
    }

    private void OnDisable()//풀링으로 교체시
    {
        DOTween.Kill(transform);
    }
}
