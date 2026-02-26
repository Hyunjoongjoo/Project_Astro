using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroHandCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image _iconImg;
    private AugmentData _data;
    private Vector3 _originPos;
    private Camera _mainCam;

    public void Setup(AugmentData data)
    {
        _data = data;
        _iconImg.sprite = data.icon;
        _mainCam = Camera.main;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _originPos = transform.position;
        _iconImg.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _iconImg.color = new Color(1, 1, 1, 1f);

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
        transform.position = _originPos;
    }

    public NetworkPrefabRef GetUnitPrefab()
    {
        return _data != null ? _data.heroPrefab : default;
    }
}
