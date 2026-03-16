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
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEquipItem(PlayerRef requestPlayer, int heroIndex, int inventoryIndex)
    {
        var playerData = _stageManager.PlayerDataMap.Get(requestPlayer);
        string targetItemId = playerData.InventoryItems.Get(inventoryIndex).Replace("\0", "").Trim();

        if (string.IsNullOrEmpty(targetItemId)) return;

        //1. 영웅 슬롯 인덱스 계산
        int slotA = heroIndex * 2;
        int slotB = heroIndex * 2 + 1;

        string equippedItemA = playerData.HeroEquippedItems.Get(slotA).Replace("\0", "").Trim();
        string equippedItemB = playerData.HeroEquippedItems.Get(slotB).Replace("\0", "").Trim();

        //2. isStackable중복 장착 검사
        var itemData = TableManager.Instance.ItemTable.Get(targetItemId);
        if (itemData != null && !itemData.isStackable)
        {
            if (equippedItemA == targetItemId || equippedItemB == targetItemId)
            {
                //중복 장착 불가 메시지 전송
                RPC_ShowToast(requestPlayer, "ui_toast_item_not_overlap");
                return;
            }
        }
        //3. 슬롯 여유 검사 및 장착
        if (string.IsNullOrEmpty(equippedItemA))
        {
            playerData.HeroEquippedItems = playerData.HeroEquippedItems.Set(slotA, targetItemId);
        }
        else if (string.IsNullOrEmpty(equippedItemB))
        {
            playerData.HeroEquippedItems = playerData.HeroEquippedItems.Set(slotB, targetItemId);
        }
        else
        {
            //아이템 슬롯 가득 참 메시지 전송
            RPC_ShowToast(requestPlayer, "ui_toast_item_equip_fail");
            return;
        }
        //4. 장착 성공 시? 인벤토리에서 제거 및 임시슬롯 갱신 한번
        playerData.InventoryItems = playerData.InventoryItems.Set(inventoryIndex, "");
        ProcessTempSlot(ref playerData);

        //5. 데이터 서버에 적용 플레이어맵데이터 set
        _stageManager.PlayerDataMap.Set(requestPlayer, playerData);
        Debug.Log($"{requestPlayer}가 영웅 {heroIndex}번에게 아이템 {targetItemId} 장착 완료.");

        //3.16 RPC
        RPC_RefreshItemUI(requestPlayer);
    }


    //아이템 아군 전달용 RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestShareItem(PlayerRef requestPlayer, PlayerRef targetAlly, int inventoryIndex)
    {
        var senderData = _stageManager.PlayerDataMap.Get(requestPlayer);
        var receiverData = _stageManager.PlayerDataMap.Get(targetAlly);

        string targetItemId = senderData.InventoryItems.Get(inventoryIndex).Replace("\0", "").Trim();
        if (string.IsNullOrEmpty(targetItemId)) return;
        bool isShared = false;

        //1. 아군 보관함 빈자리 탐색
        for (int i = 0; i < SlotData_3.Length; i++)
        {
            if (string.IsNullOrEmpty(receiverData.InventoryItems.Get(i).Replace("\0", "").Trim()))
            {
                receiverData.InventoryItems = receiverData.InventoryItems.Set(i, targetItemId);
                isShared = true;
                break;
            }
        }

        //2. 전달 실패=> 아군 보관함 가득참 => 토스트 메시지 출력
        if (!isShared)
        {
            RPC_ShowToast(requestPlayer, "ui_toast_item_share_fail");
            return;
        }

        //3. 전달 성공 => 내 인벤토리에서 제거 및 임시 슬롯 갱신 한번
        senderData.InventoryItems = senderData.InventoryItems.Set(inventoryIndex, "");
        ProcessTempSlot(ref senderData);

        //4. 데이터 서버 적용
        _stageManager.PlayerDataMap.Set(requestPlayer, senderData);
        _stageManager.PlayerDataMap.Set(targetAlly, receiverData);

        //3.16 얜 아군도 갱신(줬으니까)
        RPC_RefreshItemUI(requestPlayer);
        RPC_RefreshItemUI(targetAlly);
    }


    //임시 슬롯 큐 정리용 로직
    private void ProcessTempSlot(ref PlayerNetworkData data)
    {
        string tempItem = data.TempItemSlot.ToString().Replace("\0", "").Trim();

        //임시 슬롯에 대기중인 아이템이 없다면 패스
        if (string.IsNullOrEmpty(tempItem)) return;

        //보관함에 빈자리가 생겼는지 확인 후 이동
        for (int i = 0; i < SlotData_3.Length; i++)
        {
            if (string.IsNullOrEmpty(data.InventoryItems.Get(i).Replace("\0", "").Trim()))
            {
                data.InventoryItems = data.InventoryItems.Set(i, tempItem);
                data.TempItemSlot = ""; // 임시 슬롯 비우기
                break;
            }
        }
    }

    //UI 토스트 메시지 전송
    //아예 다른 클래스로 뺄 듯
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowToast(PlayerRef targetPlayer, string stringId)
    {
        //메시지 대상자에게만 UI를 띄움
        if (Runner.LocalPlayer == targetPlayer)
        {
            var stringData = TableManager.Instance.StringTable.Get(stringId);
            string message = stringData != null ? stringData.textKor : stringId;

            if (ToastMessageUI.Instance != null)
            {
                ToastMessageUI.Instance.ShowToast(message);
            }
            else
            {
                Debug.LogWarning($"{message} (ToastMessageUI 인스턴스 없음)");
            }
        }
    }

    //3.16
    //UI 새로고침 명령
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RefreshItemUI(PlayerRef targetPlayer)
    {
        //대상자 로컬에서만 새로고침
        if (Runner.LocalPlayer == targetPlayer)
        {
            if (ItemUIManager.Instance != null)
            {
                ItemUIManager.Instance.RefreshUI();
            }
        }
    }
}
