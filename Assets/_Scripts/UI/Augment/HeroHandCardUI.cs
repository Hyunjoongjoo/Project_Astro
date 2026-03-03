using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroHandCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private LayerMask _groundLayer;
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

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);  //에디터에서 확인용 

        if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
        {
            Debug.Log($"소환 지점 발견: {hit.point}");
            Debug.Log($"맞은 오브젝트: {hit.collider.name}");

            HeroSpawner.Instance.RPC_SpawnUnit(
                GetUnitPrefab(),
                hit.point,
                GameManager.Instance.PlayerTeam
            );
        }
        else
        {
            Debug.LogWarning("소환실패");
        }
        transform.position = _originPos;
    }

    public NetworkPrefabRef GetUnitPrefab()
    {
        return _data != null ? _data.heroPrefab : default;
    }
}
