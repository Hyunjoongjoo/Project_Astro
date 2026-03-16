using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//인벤토리 아이템 UI
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int InventoryIndex { get; private set; }

    [SerializeField] private Image _iconImg; 

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originPos;
    private Transform _originalParent;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    //서버의 InventoryItems 배열 데이터 바탕으로 UI 출력
    public void Setup(int inventoryIndex, Sprite icon)
    {
        InventoryIndex = inventoryIndex;

        if (icon != null)
        {
            _iconImg.sprite = icon;
            _iconImg.gameObject.SetActive(true);
        }
        else
        {
            _iconImg.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //빈 슬롯이면 드래그 불가
        //3.16 임시슬롯도 드래그 방어
        if (!_iconImg.gameObject.activeSelf || InventoryIndex == 99) return;

        //롤백용
        _originPos = _rectTransform.anchoredPosition;
        _originalParent = transform.parent;

        //가림방지
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_iconImg.gameObject.activeSelf) return;

        _rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_iconImg.gameObject.activeSelf) return;

        _canvasGroup.blocksRaycasts = true;

        //타겟검사로직
        //마우스 커서가 닿은 오브젝트가 있다면?
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

            //타겟이 내 영웅 카드인지 검사 -> 장착
            HeroHandCardUI heroCard = dropTarget.GetComponentInParent<HeroHandCardUI>();
            if (heroCard != null)
            {
                ItemManager.Instance.RPC_RequestEquipItem(
                    NetworkRunner.Instances[0].LocalPlayer,
                    heroCard.HeroIndex,
                    InventoryIndex
                );
                //원위치 시켜놓고 서버 응답 오면 인벤토리 UI 통째로 리로드
                ResetPosition(); 
                return;
            }

            //아군 영역인지 검사 -> 전달
            TeamCardSlotUI teamSlot = dropTarget.GetComponentInParent<TeamCardSlotUI>();
            if (teamSlot != null)
            {
                ItemManager.Instance.RPC_RequestShareItem(
                    NetworkRunner.Instances[0].LocalPlayer,
                    teamSlot.AllyPlayerRef,
                    InventoryIndex
                );
                ResetPosition();
                return;
            }
        }

        //아무 데도 안 맞았거나 허공에 놨으면 원위치로 롤백
        ResetPosition();
    }

    private void ResetPosition()
    {
        transform.SetParent(_originalParent);
        _rectTransform.anchoredPosition = _originPos;
    }
}