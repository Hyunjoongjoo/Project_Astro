using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct CooldownData : INetworkStruct
{
    public float EndTime;
    public float TotalDuration;
}

public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 2.5f;
    [SerializeField] private float _minDeployDistance = 1f;

    [Header("배치 거리 확장")]
    [SerializeField] private float _baseDeployDistance = 8f;
    [SerializeField] private float _deployExpandPerTower = 3f;
    [SerializeField] private float _maxDeployDistance = 14f;

    [SerializeField] private List<NetworkPrefabRef> _prefabs;

    [Networked, Capacity(32)]
    private NetworkDictionary<int, CooldownData> CooldownMap => default;

    public override void Spawned()
    {
        Instance = this;
    }
    private int GetPrefabId(NetworkPrefabRef prefab)
    {
        int index = _prefabs.IndexOf(prefab);

        if (index < 0)
        {
            Debug.LogError($"[HeroSpawner] prefabId 못찾음: {prefab}");
            return 0;
        }

        return index;
    }

    private int GetKey(PlayerRef player, int prefabId)
    {
        return (player.PlayerId << 16) | prefabId;
    }

    private bool CanSummon(PlayerRef player, NetworkPrefabRef prefab)
    {
        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (!CooldownMap.TryGet(key, out var data))
            return true;

        return Runner.SimulationTime >= data.EndTime;
    }

    private int GetDestroyedTowerCount(Team team)//현재 파괴된 포탑 수
    {
        int aliveCount = 0;

        Team enemyTeam = team == Team.Blue ? Team.Red : Team.Blue;

        foreach (var tower in Tower.AliveTowers)
        {
            if (tower == null || tower.team == Team.None) continue;
            if (tower.team == enemyTeam)
            {
                aliveCount++;
            }
        }

        return Mathf.Clamp(2 - aliveCount, 0, 2);//팀당 포탑 2개 기준
    }

    private float GetCurrentDeployDistance(Team team)//현재 배치 가능 거리 계산
    {
        int destroyedCount = GetDestroyedTowerCount(team);

        float distance = _baseDeployDistance + destroyedCount * _deployExpandPerTower;

        return Mathf.Min(distance, _maxDeployDistance);
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
        float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        return distance <= maxDistance;
    }

    public bool CanPreviewDeployHero(Vector3 spawnPos, Team team)//UI체크
    {
        Transform origin = GetDeployOrigin(team);

        if (origin == null)
        {
            return false;
        }

        float distance = Vector3.Distance(origin.position, spawnPos);
        float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        return distance <= maxDistance;
    }

    private Transform GetDeployOrigin(Team team)//함교 위치를 반환
    {
        UnitBase[] myStructures = team == Team.Blue
            ? ObjectContainer.Instance.blueSideStructure
            : ObjectContainer.Instance.redSideStructure;

        if (myStructures == null || myStructures.Length <= 2)
        {
            return null;
        }

        if (myStructures[2] == null)
        {
            return null;
        }

        return myStructures[2].transform;
    }

    private float GetDeployDelay(float distance)//거리 기반 배치 지연시간
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    public void StartSummonCooldown(PlayerRef player, NetworkPrefabRef prefab, float cooldown)
    {
        if (!Object.HasStateAuthority) return;

        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        CooldownMap.Set(key, new CooldownData
        {
            EndTime = Runner.SimulationTime + cooldown,
            TotalDuration = cooldown
        });

        Debug.Log($"[쿨타임 시작] player:{player.PlayerId}, prefabId:{prefabId}, cd:{cooldown}");
    }


    //UI에서 사용할수있도록 메서드로 지정한 플레이어와 프리팹에 대한 남은 소환 쿨타임을 반환
    public float GetRemainingCooldown(PlayerRef player, NetworkPrefabRef prefab)
    {
        if (Runner == null || !Runner.IsRunning)
            return 0f;

        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (CooldownMap.TryGet(key, out var data))
        {
            float remaining = data.EndTime - Runner.SimulationTime;
            return Mathf.Max(remaining, 0f);
        }

        return 0f;
    }

    public float GetTotalCooldown(PlayerRef player, NetworkPrefabRef prefab)
    {
        int prefabId = GetPrefabId(prefab);
        int key = GetKey(player, prefabId);

        if (CooldownMap.TryGet(key, out var data))
        {
            return data.TotalDuration;
        }

        return 0f;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team, RpcInfo info = default)
    {
        if (prefab == default) return;

        PlayerRef caller = info.Source;

        if (!CanSummon(caller, prefab)) return;
        if (!CanDeployHero(spawnPos, team)) return;

        Transform origin = GetDeployOrigin(team);
        if (origin == null) return;

        float distance = Vector3.Distance(origin.position, spawnPos);
        float deployDelay = GetDeployDelay(distance);

        //영웅 초기 방향 결정
        Vector3 forwardDir = team == Team.Blue ? Vector3.forward : Vector3.back;
        Quaternion spawnRot = Quaternion.LookRotation(forwardDir);

        Runner.Spawn(prefab, origin.position, spawnRot, caller,
         onBeforeSpawned: (runner, obj) =>
         {
             HeroController hero = obj.GetComponent<HeroController>();
             hero.Setup(team, spawnPos, deployDelay, prefab, caller);
         });
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        DrawDeployGizmo(Team.Blue, Color.blue);

        DrawDeployGizmo(Team.Red, Color.red);
    }

    private void DrawDeployGizmo(Team team, Color color)
    {
        Transform origin = GetDeployOrigin(team);
        if (origin == null) return;

        float distance = GetCurrentDeployDistance(team);

        Gizmos.color = color;
        Gizmos.DrawWireSphere(origin.position, distance);
    }
#endif
}
