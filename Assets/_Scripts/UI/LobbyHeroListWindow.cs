using UnityEngine;
using System.Collections.Generic;

public class LobbyHeroListWindow : BaseUI
{
    [Header("연결 설정")]
    [SerializeField] private GameObject _heroCardPrefab;
    [SerializeField] private Transform _content;

    public override void Open()
    {
        if (!gameObject.activeSelf)
        {
            RefreshHeroList();
        }
        base.Open();
    }

    private void RefreshHeroList()
    {
        
        foreach (Transform child in _content)
        {
            Destroy(child.gameObject);
        }

        var heroTable = TableManager.Instance.HeroTable.GetAll();

        if (heroTable == null) return;

        foreach (HeroData data in heroTable)
        {
            // 프리팹 생성
            GameObject cardObj = Instantiate(_heroCardPrefab, _content);

            if (cardObj.TryGetComponent(out LobbyHeroCardUI cardUI))
            {
                cardUI.Setup(data);
            }
        }
    }
}
