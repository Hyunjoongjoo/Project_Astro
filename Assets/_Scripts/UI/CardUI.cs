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
            SpawnUnit(hit.point);
        }
    }
    private void SpawnUnit(Vector3 spawnPos)
    {
        GameObject prefab = GetUnitPrefab();
        //if (prefab != null)
        //{
        //    Instantiate(prefab, spawnPos, Quaternion.identity);
        //    Debug.Log($"{_cardData.name} 소환 완료!");
        //}
        //26-02-13 주현중 수정
        if (prefab == null)
        {
            return;
        }

        GameObject heroObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        HeroController ai = heroObj.GetComponent<HeroController>();
        if (ai != null)
        {
            //임시 팀
            Team myTeam = Team.Blue; //나중에 교체

            //일단 null
            // ai.Setup(myTeam, null);
        }

        Debug.Log($"{_cardData.name} 소환 완료!");
    }

    public GameObject GetUnitPrefab()
    {
        return _cardData != null ? _cardData.heroPrefab : null;
    }
}
