using Fusion;
using UnityEngine;

public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 2.5f;
    [SerializeField] private float _minDeployDistance = 1f;
    [SerializeField] private float _maxDeployDistance = 15f;

    public override void Spawned()
    {
        Instance = this;
    }

    public bool CanDeployHero(Vector3 spawnPos, Team team)
    {
        if (!Object.HasStateAuthority)
        {
            return false;
        }

        if (!GameManager.Instance.IsGameStarted) // 카운트다운 중 차단
        {
            return false;
        }

        Transform deployOrigin = GetDeployOrigin(team);
        if (deployOrigin == null)
        {
            return false;
        }

        float distance = Vector3.Distance(deployOrigin.position, spawnPos);
        return distance <= _maxDeployDistance;
    }

    private Transform GetDeployOrigin(Team team)
    {
        UnitBase[] myStructures = team == Team.Blue
            ? ObjectContainer.Instance.blueSideStructure
            : ObjectContainer.Instance.redSideStructure;

        //index 2 = 함교
        return myStructures != null && myStructures.Length > 2
            ? myStructures[2].transform
            : null;
    }

    private float GetDeployDelay(float distance)
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team)
    {

        if (prefab == default)
            return;

        if (!CanDeployHero(spawnPos, team))
        {
            return;
        }

        Transform origin = GetDeployOrigin(team);
        float distance = Vector3.Distance(origin.position, spawnPos);
        float deployDelay = GetDeployDelay(distance);

        Runner.Spawn(prefab, spawnPos, Quaternion.identity,
            onBeforeSpawned: (Runner, obj) =>
            {
                HeroController hero = obj.GetComponent<HeroController>();
                hero.Setup(team);

                hero.BeginDeploy(spawnPos, deployDelay);
            });

        Debug.Log($"영웅 소환 완료!");
    }
}
