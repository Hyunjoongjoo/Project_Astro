using Fusion;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;
    private float _finalPower;

    ProjectileSkillSO _data;
    private NetworkRunner _runner;
    Team _team;

    public void Initialize(ProjectileSkillSO data, Team team, float power, NetworkRunner runner)
    {
        _data = data;
        _team = team;
        _runner = runner;
        _finalPower = power * _data.damageRatio;
        ApplyTeamColor(team);
    }

    public void Fire(GameObject targetPos)
    {
        transform.LookAt(targetPos.transform);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxList.ShotLaserSound);

        Destroy(gameObject, 3f);
    }

    private void FixedUpdate()
    {
        transform.Translate(Vector3.forward * _data.projectileSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ( other.gameObject.TryGetComponent(out UnitBase target) )
        {
            if (target.team != _team)
            {
                if (_runner.IsSharedModeMasterClient)
                    target.TakeDamage(_finalPower);
                Destroy(gameObject);
            }
        }
    }

    // 팀 색에 맞춰 투사체에도 색상 적용
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
}
