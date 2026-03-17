using UnityEngine;
using Fusion;
//내 보관함 UI 3개와 영웅 카드들 캐싱해두고 StageManager에서 데이터 읽어와서 아이콘 이미지 갈아끼우는 역할
//변경된 네트워크 데이터를 감지하고 하단 패널을 새로고침하게 구현

public class ItemUIManager : Singleton<ItemUIManager>
{
    [Header("UI References")]
    //프레임 3개 연결
    [SerializeField] private InventoryItemUI[] _inventorySlots; 
    [SerializeField] private Transform _heroCardContainer;

    //3.16 임시슬롯 1칸짜리
    [SerializeField] private InventoryItemUI _tempSlotUI;

    [Header("IconSO")]
    [SerializeField] private ItemIconDataSO _itemIconData;

    //갱신 명령 시 실행할 함수
    public void RefreshUI()
    {
        //1. 채신 네트워크 데이터 가져오기
        if (StageManager.Instance == null || NetworkRunner.Instances.Count == 0) return;

        var myData = StageManager.Instance.PlayerDataMap.Get(NetworkRunner.Instances[0].LocalPlayer);

        //2. 인벤 3칸 렌더링
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            string itemId = myData.InventoryItems.Get(i).Replace("\0", "").Trim();
            Sprite icon = GetItemSprite(itemId);

            _inventorySlots[i].Setup(i, icon);
        }

        //2.5 임시슬롯 렌더링 깜빡;
        if (_tempSlotUI != null)
        {
            string tempItemId = myData.TempItemSlot.ToString().Replace("\0", "").Trim();
            Sprite tempIcon = GetItemSprite(tempItemId);

            //드래그 금지
            _tempSlotUI.Setup(99, tempIcon);
        }

        //3. 영웅 장착슬롯 렌더링 => 컨테이너 하위 내 영웅카드 찾아서
        HeroHandCardUI[] heroCards = _heroCardContainer.GetComponentsInChildren<HeroHandCardUI>();
        foreach (var card in heroCards)
        {
            int index = card.HeroIndex;
            string itemA = myData.HeroEquippedItems.Get(index * 2).Replace("\0", "").Trim();
            string itemB = myData.HeroEquippedItems.Get(index * 2 + 1).Replace("\0", "").Trim();

            card.UpdateEquippedItems(GetItemSprite(itemA), GetItemSprite(itemB));
        }

        Debug.Log("인벤토리 및 영웅 장착 UI 렌더링 완료");
    }



    //Resources말고 얘도 그냥 SO 하나 만들어서 아이디와 직렬화
    private Sprite GetItemSprite(string itemId)
    {
        if (_itemIconData == null) return null;
        return _itemIconData.GetIcon(itemId);
    }
}
