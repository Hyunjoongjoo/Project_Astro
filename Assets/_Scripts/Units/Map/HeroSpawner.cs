using System.Collections.Generic;
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

    //PlayerRef 기준으로 분리하여 독립 쿨다운
    private readonly Dictionary<PlayerRef, TickTimer> _playerSummonTimers = new Dictionary<PlayerRef, TickTimer>();

    public override void Spawned()
    {
        Instance = this;
    }

    private bool CanSummon(PlayerRef player)//소환 가능한지 여부
    {
        if (!_playerSummonTimers.TryGetValue(player, out TickTimer timer))
        {
            return true; // 아직 쿨 기록 없음 → 소환 가능
        }

        return timer.ExpiredOrNotRunning(Runner);
    }

    public bool CanDeployHero(Vector3 spawnPos, Team team)//해당 위치에 영웅 배치가 가능한지 검사
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

        //최대 배치 거리 초과 시 차단
        float distance = Vector3.Distance(deployOrigin.position, spawnPos);
        return distance <= _maxDeployDistance;
    }

    private Transform GetDeployOrigin(Team team)//함교 위치를 반환
    {
        UnitBase[] myStructures = team == Team.Blue
            ? ObjectContainer.Instance.blueSideStructure
            : ObjectContainer.Instance.redSideStructure;

        //index 2 = 함교
        return myStructures != null && myStructures.Length > 2
            ? myStructures[2].transform
            : null;
    }

    private float GetDeployDelay(float distance)//거리 기반 배치 지연시간
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    private void StartSummonCooldown(PlayerRef player, float cooldown)
    {
        _playerSummonTimers[player] =
            TickTimer.CreateFromSeconds(Runner, cooldown);
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team, RpcInfo info = default)
    {
        if (prefab == default)
        {
            return;
        }

        PlayerRef caller = info.Source;

        if (!CanSummon(caller))
        {
            return;
        }

        if (!CanDeployHero(spawnPos, team))
        {
            return;
        }

        Transform origin = GetDeployOrigin(team);
        float distance = Vector3.Distance(origin.position, spawnPos);
        float deployDelay = GetDeployDelay(distance);

        //영웅 초기 방향 결정
        Vector3 forwardDir = team == Team.Blue ? Vector3.forward : Vector3.back;
        Quaternion spawnRot = Quaternion.LookRotation(forwardDir);

        Runner.Spawn(prefab, spawnPos, spawnRot,
            onBeforeSpawned: (Runner, obj) =>
            {
                HeroController hero = obj.GetComponent<HeroController>();
                hero.Setup(team);
                //배치 및 지연 처리는 컨트롤러가 수행
                hero.BeginDeploy(spawnPos, deployDelay);
                StartSummonCooldown(caller, hero.SummonCooldown);
            });

        Debug.Log($"영웅 소환 완료!");
    }
}
