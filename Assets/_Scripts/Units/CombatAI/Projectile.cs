using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    public void Fire(Vector3 targetPos)
    {
        transform.DOMove(targetPos, 0.18f).SetEase(Ease.Linear).OnComplete(OnHit);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(SfxList.ShotLaserSound);
        }
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
