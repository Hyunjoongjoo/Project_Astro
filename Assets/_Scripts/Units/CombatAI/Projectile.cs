using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;

    public void Fire(Vector3 targetPos, Team team)
    {
        ApplyTeamColor(team);

        transform.DOMove(targetPos, 0.18f).SetEase(Ease.Linear).OnComplete(OnHit);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(SfxList.ShotLaserSound);
        }
    }

    private void ApplyTeamColor(Team team)
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInChildren<Renderer>();
        }

        Material mat = _targetRenderer.material;

        if (team == Team.Blue)
        {
            mat.SetColor("_Color01", new Color(0.2f, 0.6f, 1f));
            mat.SetColor("_Color02", new Color(0f, 0.8f, 1f));
        }
        else
        {
            mat.SetColor("_Color01", new Color(1f, 0.3f, 0.1f));
            mat.SetColor("_Color02", new Color(1f, 0.6f, 0.2f));
        }
    }

    private void OnHit()
    {
        Destroy(gameObject);
    }

    //private void OnDisable()//풀링으로 교체시
    //{
    //    DOTween.Kill(transform);
    //}
}
