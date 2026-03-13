using Fusion;
using UnityEngine;

//아이템 장착, 공유 등의 가능여부를 서버가 검증하고 처리하기 위한 클래스
//장착, 공유, 임시슬롯 큐 처리
//클라이언트가 드래그 앤 드롭을 마치면 RPC 통신
public class ItemManager : NetworkBehaviour
{
    public static ItemManager Instance { get; private set; }

    private StageManager _stageManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Spawned()
    {
        _stageManager = FindFirstObjectByType<StageManager>();
    }

    //아이템 장착 요청 RPC

    //아이템 아군 전달용 RPC

    //임시 슬롯 큐 정리용 로직

    //UI 토스트 메시지 전송(후순위)
}
