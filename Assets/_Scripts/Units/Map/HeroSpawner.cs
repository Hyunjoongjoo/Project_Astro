using Fusion;
using UnityEngine;

public class HeroSpawner : NetworkBehaviour
{
    public static HeroSpawner Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnUnit(NetworkPrefabRef prefab, Vector3 spawnPos, Team team)
    {

        if (prefab == default)
            return;

        Runner.Spawn(prefab, spawnPos, Quaternion.identity,
            onBeforeSpawned: (Runner, obj) =>
            {
                HeroController hero = obj.GetComponent<HeroController>();
                hero.Setup(team);

                hero.StartDeploy(spawnPos);
            });

        Debug.Log($"영웅 소환 완료!");
    }
}
