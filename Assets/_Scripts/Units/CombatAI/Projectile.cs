using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    private float _damage;

    public void Fire(Transform target, float damage)
    {
        _damage = damage;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 hitPos = target.position;

        transform.DOMove(hitPos, 0.3f).SetEase(Ease.Linear).OnComplete(() => { Hit(target); });
    }

    private void Hit(Transform target)
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Tower tower = target.GetComponent<Tower>();
        if (tower != null)
        {
            tower.TakeDamage(_damage);
        }
        else
        {
            UnitController unit = target.GetComponent<UnitController>();
            if (unit != null && !unit.IsDead)
            {
                unit.TakeDamage(_damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnDisable()//풀링으로 교체시
    {
        DOTween.Kill(transform);
    }
}
