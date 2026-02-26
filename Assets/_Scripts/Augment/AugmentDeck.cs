using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AugmentDeck
{
    private List<AugmentData> _heroAugments = new List<AugmentData>();
    private List<AugmentData> _skillAugments = new List<AugmentData>();
    private List<AugmentData> _itemAugments = new List<AugmentData>();

    private AugmentDataSO _masterSO;

    public AugmentDeck(AugmentDataSO so)
    {
        _masterSO = so;
        InitializeDeck();
    }

    private void InitializeDeck()
    {
        foreach(var aug in _masterSO.data)
        {
            if(aug.type == AugmentType.Hero) _heroAugments.Add(aug);
            else if(aug.type == AugmentType.Skill) _skillAugments.Add(aug);
            else if(aug.type == AugmentType.Item) _itemAugments.Add(aug);
        }
    }

    // 특정 타입의 랜덤 카드 뽑기 일단 아이템은 제외시켰음
    public AugmentData GetRandomCard(AugmentType type, List<string> excludeIds)
    {
        var source = (type == AugmentType.Hero) ? _heroAugments : _skillAugments;
        var available = source.Where(a => !excludeIds.Contains(a.id)).ToList();

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }
}
