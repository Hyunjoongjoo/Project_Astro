using System.Collections.Generic;
using UnityEngine;

public class AugmentWindowUI : BaseUI
{
    [SerializeField] private GameObject _cardPrefab; //AugmentCardUI 붙은애
    [SerializeField] Transform _cardContainer; // 카드 배치시킬 위치

    public void SetupAndOpen(List<AugmentData> datas)
    {
        foreach(Transform child in _cardContainer) Destroy(child.gameObject); //기존 카드 제거

        //새 카드 생성
        foreach(var data in datas)
        {
            var go = Instantiate(_cardPrefab,_cardContainer);
            if (go.TryGetComponent(out AugmentCardUI cardUI))
            {
                cardUI.Setup(data);
            }
        }

        base.Open();
    }
}
