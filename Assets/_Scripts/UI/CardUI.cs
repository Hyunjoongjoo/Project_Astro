using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IBeginDragHandler,IDragHandler, IEndDragHandler
{
    [SerializeField] CardData _cardData;
    [SerializeField] Image _cardImg;

    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        if (_cardData == null) return;
        if( _cardImg != null ) _cardImg.sprite = _cardData.heroImg;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _cardImg.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cardImg.color = new Color(1, 1, 1, 1f);

        Ray ray = _mainCam.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            HeroSpawner.Instance.RPC_SpawnUnit(
                GetUnitPrefab(), 
                hit.point, 
                GameManager.Instance.PlayerTeam
                );
        }
    }

    public NetworkPrefabRef GetUnitPrefab()
    {
        return _cardData != null ? _cardData.heroPrefab : default;
    }
}
