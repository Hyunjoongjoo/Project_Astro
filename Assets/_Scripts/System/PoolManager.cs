using UnityEngine;
using System.Collections.Generic;

public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    public struct Pool
    {
        public string tag;  // 식별용 태그 (프리팹이름 등등)
        public GameObject prefab;
        public int size;   // 초기 생성 갯수
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> _poolDict;

    protected override void OnSingletonAwake()
    {
        _poolDict = new Dictionary<string, Queue<GameObject>>();

        foreach(Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for(int i=0; i<pool.size; i++)
            {
                GameObject obj = CreateNewObject(pool.tag, pool.prefab);
                objectPool.Enqueue(obj);
            }

            _poolDict.Add(pool.tag, objectPool);
        }
    }

    //새로운 오브젝트 인스턴스화용
    private GameObject CreateNewObject(string tag, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.name = tag;
        obj.SetActive(false);
        return obj;
    }

    //풀에서 꺼내서 활성화시키기
    public GameObject SpawnFromPool(string tag, Vector3 positon, Quaternion rotation, Transform parent = null)
    {
        if (!_poolDict.ContainsKey(tag)) return null;

        GameObject objectToSpawn;

        if (_poolDict[tag].Count == 0) //폴 크기 자동확장
        {
            Pool pool = pools.Find(p => p.tag == tag);
            objectToSpawn = CreateNewObject(tag, pool.prefab);
        }
        else // 있으면 재사용
        {
            objectToSpawn = _poolDict[tag].Dequeue();
        }

        if (parent != null) objectToSpawn.transform.SetParent(parent); //부모설정 , 캔버스자식으로 넣기용

        objectToSpawn.transform.SetPositionAndRotation(positon, rotation);
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    // 풀로 반환 시키기
    public void ReturnToPool(string tag,GameObject obj)
    {
        if (!_poolDict.ContainsKey(tag)) //잘못된 풀로 반환 시도시 그냥 파괴
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        _poolDict[tag].Enqueue(obj);
    }
}
