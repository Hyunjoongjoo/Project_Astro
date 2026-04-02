using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct CooldownData : INetworkStruct
{
    public float EndTime;
    public float TotalDuration;
}

public struct DeployZoneData
{
    public Vector3 Center;
    public Vector2 Size;

    public DeployZoneData(Vector3 center, Vector2 size)
    {
        Center = center;
        Size = size;
    }
}


public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 6.5f;
    [SerializeField] private float _minDeployDistance = 1f;
    //[SerializeField] private float _baseDeployDistance = 8f;
    //[SerializeField] private float _deployExpandPerTower = 3f;
    [SerializeField] private float _maxDeployDistance = 20f;

    [Header("사각형 배치 존 설정")]
    [SerializeField] private Vector2 _centerZoneOffset = new Vector2(0f, 4.5f);
    [SerializeField] private Vector2 _centerZoneSize = new Vector2(18f, 13f); 

    [SerializeField] private Vector2 _leftZoneOffset = new Vector2(-4.5f, 13f); 
    [SerializeField] private Vector2 _leftZoneSize = new Vector2(9f, 8f); 

    [SerializeField] private Vector2 _rightZoneOffset = new Vector2(4.5f, 13f); 
    [SerializeField] private Vector2 _rightZoneSize = new Vector2(9f, 8f); 

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

    // 상대 진영의 왼쪽 포탑이 파괴되었는지 확인
    private bool IsLeftTowerDestroyed(Team team)
    {
        UnitBase[] enemyStructures = team == Team.Blue
            ? ObjectContainer.Instance.redSideStructure
            : ObjectContainer.Instance.blueSideStructure;

        if (enemyStructures == null || enemyStructures.Length <= 1)
        {
            return false;
        }

        UnitBase leftTower = enemyStructures[1];
        return leftTower == null || leftTower.team == Team.None;
    }

    // 상대 진영의 오른쪽 포탑이 파괴되었는지 확인
    private bool IsRightTowerDestroyed(Team team)
    {
        UnitBase[] enemyStructures = team == Team.Blue
            ? ObjectContainer.Instance.redSideStructure
            : ObjectContainer.Instance.blueSideStructure;

        if (enemyStructures == null || enemyStructures.Length <= 1)
        {
            return false;
        }

        UnitBase rightTower = enemyStructures[0];
        return rightTower == null || rightTower.team == Team.None;
    }

    // 팀 방향을 반영해 배치 존의 중심 좌표를 월드 좌표로 변환
    private Vector3 GetZoneCenterWorld(Transform origin, Team team, Vector2 offset)
    {
        float x = team == Team.Blue ? offset.x : -offset.x;
        float z = team == Team.Blue ? offset.y : -offset.y;

        return origin.position + new Vector3(x, 0f, z);
    }

    // 주어진 좌표가 직사각형 배치 존 내부에 있는지 검사
    private bool IsInsideRectZone(Vector3 point, Vector3 center, Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        bool insideX = point.x >= center.x - halfWidth && point.x <= center.x + halfWidth;
        bool insideZ = point.z >= center.z - halfHeight && point.z <= center.z + halfHeight;

        return insideX && insideZ;
    }

    // 현재 열려 있는 배치 존 중 하나에 목적지가 포함되는지 확인
    private bool IsInsideAvailableDeployZone(Vector3 spawnPos, Team team)
    {
        Transform origin = GetDeployOrigin(team);

        if (origin == null) return false;
       
        bool isLeftTowerDestroyed = IsLeftTowerDestroyed(team);
        bool isRightTowerDestroyed = IsRightTowerDestroyed(team);

        Vector3 centerZoneCenter = GetZoneCenterWorld(origin, team, _centerZoneOffset);
        if (IsInsideRectZone(spawnPos, centerZoneCenter, _centerZoneSize))        
            return true;
        

        if (isLeftTowerDestroyed)
        {
            Vector3 leftZoneCenter = GetZoneCenterWorld(origin, team, _leftZoneOffset);
            if (IsInsideRectZone(spawnPos, leftZoneCenter, _leftZoneSize)) 
                return true;
        }

        if (isRightTowerDestroyed)
        {
            Vector3 rightZoneCenter = GetZoneCenterWorld(origin, team, _rightZoneOffset);
            if (IsInsideRectZone(spawnPos, rightZoneCenter, _rightZoneSize))           
                return true;
        }

        return false;
    }

    public bool CanDeployHero(Vector3 spawnPos, Team team)//해당 위치에 영웅 배치가 가능한지 검사
    {
        if (!Object.HasStateAuthority) return false;        
        if (!GameManager.Instance.IsGameStarted) return false; // 카운트다운 중 차단

        //Transform deployOrigin = GetDeployOrigin(team);
        //if (deployOrigin == null)
        //{
        //    return false;
        //}

        ////최대 배치 거리 초과 시 차단
        //float distance = Vector3.Distance(deployOrigin.position, spawnPos);
        //float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        //return distance <= maxDistance;
        return IsInsideAvailableDeployZone(spawnPos, team);
    }

    public bool CanPreviewDeployHero(Vector3 spawnPos, Team team)//UI체크
    {
        //Transform origin = GetDeployOrigin(team);

        //if (origin == null)
        //{
        //    return false;
        //}

        //float distance = Vector3.Distance(origin.position, spawnPos);
        //float maxDistance = GetCurrentDeployDistance(team);//현재 남은 포탑 기반 배치 거리
        //return distance <= maxDistance;
        return IsInsideAvailableDeployZone(spawnPos, team);
    }

    // 팀에 해당하는 함교 Transform을 배치 기준점으로 반환
    private Transform GetDeployOrigin(Team team)
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

    // 함교와 목적지 거리 기반으로 디플로이 지연 시간을 계산
    private float GetDeployDelay(float distance)
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);
        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    // 영웅 소환 후 플레이어별 쿨타임 정보를 네트워크 딕셔너리에 기록
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

    // 지정한 플레이어와 영웅의 전체 소환 쿨타임 값을 반환
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

    // 활성화된 배치 존 목록 반환
    public List<DeployZoneData> GetAvailableDeployZones(Team team)
    {
        List<DeployZoneData> zones = new List<DeployZoneData>();

        Transform origin = GetDeployOrigin(team);
        if (origin == null)
        {
            return zones;
        }

        bool isLeftTowerDestroyed = IsLeftTowerDestroyed(team);
        bool isRightTowerDestroyed = IsRightTowerDestroyed(team);

        Vector3 centerZoneCenter = GetZoneCenterWorld(origin, team, _centerZoneOffset);
        zones.Add(new DeployZoneData(centerZoneCenter, _centerZoneSize));

        if (isLeftTowerDestroyed)
        {
            Vector3 leftZoneCenter = GetZoneCenterWorld(origin, team, _leftZoneOffset);
            zones.Add(new DeployZoneData(leftZoneCenter, _leftZoneSize));
        }

        if (isRightTowerDestroyed)
        {
            Vector3 rightZoneCenter = GetZoneCenterWorld(origin, team, _rightZoneOffset);
            zones.Add(new DeployZoneData(rightZoneCenter, _rightZoneSize));
        }

        return zones;
    }


    //stat이 없으면 default로 전달
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team)
    {
        HeroStatNetworkData emptyStat = default;
        RPC_SpawnUnit(prefab, spawnPos, team, emptyStat);
    }

    //플레이어별 성장 stat을 함께 넘긴다.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team, HeroStatNetworkData stat, RpcInfo info = default)
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
             hero.Setup(team, spawnPos, deployDelay, prefab, caller, stat);
         });
    }

#if UNITY_EDITOR
    // 포탑 파괴 여부에 따라 배치 영역 기즈모를 표시
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        DrawDeployGizmo(Team.Blue, Color.blue);

        DrawDeployGizmo(Team.Red, Color.red);
    }

    private void DrawRectGizmo(Vector3 center, Vector2 size)
    {
        Vector3 half = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);

        Vector3 p1 = center + new Vector3(-half.x, 0f, -half.z);
        Vector3 p2 = center + new Vector3(-half.x, 0f, half.z);
        Vector3 p3 = center + new Vector3(half.x, 0f, half.z);
        Vector3 p4 = center + new Vector3(half.x, 0f, -half.z);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
    
    private void DrawDeployGizmo(Team team, Color color)
    {
        Transform origin = GetDeployOrigin(team);
        if (origin == null) return;

        bool isLeftTowerDestroyed = IsLeftTowerDestroyed(team);
        bool isRightTowerDestroyed = IsRightTowerDestroyed(team);

        Gizmos.color = color;

        Vector3 centerZoneCenter = GetZoneCenterWorld(origin, team, _centerZoneOffset); 
        DrawRectGizmo(centerZoneCenter, _centerZoneSize);

        if (isLeftTowerDestroyed)
        {
            Vector3 leftZoneCenter = GetZoneCenterWorld(origin, team, _leftZoneOffset);
            DrawRectGizmo(leftZoneCenter, _leftZoneSize);
        }

        if (isRightTowerDestroyed)
        {
            Vector3 rightZoneCenter = GetZoneCenterWorld(origin, team, _rightZoneOffset);
            DrawRectGizmo(rightZoneCenter, _rightZoneSize);
        }
    }
#endif
}
