using Fusion;
using UnityEngine;

//아이템 장착, 공유 등의 가능여부를 서버가 검증하고 처리하기 위한 클래스
//부착은 음 거기 그
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

    //1. 영웅 슬롯 인덱스 계산
    //2. isStackable중복 장착 검사
    //3. 슬롯 여유 검사 및 장착
    //4. 장착 성공 시? 인벤토리에서 제거 및 임시슬롯 갱신 한번
    //5. 데이터 서버에 적용 플레이어맵데이터 set


    //아이템 아군 전달용 RPC

    //1. 아군 보관함 빈자리 탐색
    //2. 전달 실패=> 아군 보관함 가득참 => 토스트 메시지 출력
    //3. 전달 성공 => 내 인벤토리에서 제거 및 임시 슬롯 갱신 한번
    //4. 데이터 서버 적용

    //임시 슬롯 큐 정리용 로직

    //1. 임시 슬롯에 대기중인 아이템 없으면 바로 패스
    //2. 보관함 빈자리 체크하고 이동

    //UI 토스트 메시지 전송(후순위)
    //화면에 message를 2초간 띄우기 어쩌고
}
