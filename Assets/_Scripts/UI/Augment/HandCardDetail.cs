using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandCardDetail : BaseUI
{
    [Header("영웅 정보")]
    [SerializeField] private Image _heroIcon;       //영웅 아이콘
    [SerializeField] private TMP_Text _heroName;    //영웅 이름
    [SerializeField] private TMP_Text _skillDes;    //영웅 기본 스킬 설명

    [Header("증강 정보")]
    [SerializeField] private Image _augmentIcon;    //적용된 증강 아이콘
    [SerializeField] private TMP_Text _augmentName; //적용된 증강 이름
    [SerializeField] private TMP_Text _augmentDes;  //적용된 증강 설명

    [Header("아이템 정보")]
    [SerializeField] private Image _itemIcon1;      //장착한 아이템 아이콘 1
    [SerializeField] private Image _itemIcon2;      //장착한 아이템 아이콘 2
    [SerializeField] private TMP_Text _item1Name;   //장착한 아이템1의 이름
    [SerializeField] private TMP_Text _item2Name;   //장착한 아이템1의 이름
    [SerializeField] private TMP_Text _itemDes1;    //장착한 아이템1의 효과 설명
    [SerializeField] private TMP_Text _itemDes2;    //장착한 아이템2의 효과 설명

    public void SetupByIds(AugmentData heroData, string itemId1, string itemId2, List<string> augIds)
    {
        Debug.Log($"Detail Open: {heroData.targetId}, Item1: {itemId1}, AugCount: {augIds.Count}");
        // [영웅 정보]
        _heroIcon.sprite = heroData.mainIcon;
        _heroName.text = TableManager.Instance.GetString(heroData.titleName);
        // SkillCardUI처럼 data.skillData에서 직접 가져옴
        _skillDes.text = (heroData.skillData != null) ? TableManager.Instance.GetString(heroData.skillData.skillDescription) : "";

        // [아이템 정보]
        SetItemSlot(_itemIcon1, _item1Name, _itemDes1, itemId1);
        SetItemSlot(_itemIcon2, _item2Name, _itemDes2, itemId2);

        // [증강 정보]
        _augmentDes.text = "";
        if (augIds == null || augIds.Count == 0)
        {
            _augmentName.text = "증강 없음";
            _augmentDes.text = "적용된 증강이 없습니다.";
            _augmentIcon.gameObject.SetActive(false); // 아이콘도 꺼주기
        }
        else
        {
            _augmentIcon.gameObject.SetActive(true);
            for (int i = 0; i < augIds.Count; i++)
            {
                string rawId = augIds[i];
                if (string.IsNullOrEmpty(rawId)) continue;

                string pureId = rawId.Split('#')[0];
                int tier = rawId.Contains("#") ? int.Parse(rawId.Split('#')[1]) : 0;

                var so = AugmentController.Instance.GetSkillAugmentById(pureId);
                if (so != null)
                {
                    // 티어 범위 체크 강화
                    int targetTier = Mathf.Clamp(tier, 0, so.Tiers.Length - 1);
                    var tierData = so.Tiers[targetTier];

                    string t = TableManager.Instance.GetString(tierData.TitleStringID);
                    string d = TableManager.Instance.GetString(tierData.DescStringID);

                    if (i == 0) // 첫 번째 증강을 대표로 표시
                    {
                        _augmentName.text = t;
                        _augmentIcon.sprite = tierData.Icon;
                    }
                    _augmentDes.text += $"{d}"; // 가독성 위해 줄바꿈 추가
                }
            }
        }
    }

    private void SetItemSlot(Image icon, TMP_Text nameTxt, TMP_Text desTxt, string id)
    {
        // 1. null이나 공백, 혹은 "null"이라는 문자열(가끔 발생) 체크
        if (string.IsNullOrWhiteSpace(id) || id == "0" || id.ToLower() == "null")
        {
            icon.gameObject.SetActive(false);
            nameTxt.text = "";
            desTxt.text = "장착 아이템 없음"; // 명시적으로 표시
            return;
        }

        // 2. 아이콘 설정
        Sprite s = ItemUIManager.Instance.GetItemSprite(id);
        icon.sprite = s;
        icon.gameObject.SetActive(s != null);

        // 3. 데이터 테이블에서 정보 가져오기
        var data = TableManager.Instance.ItemTable.Get(id);
        if (data != null)
        {
            nameTxt.text = TableManager.Instance.GetString(data.name);
            desTxt.text = TableManager.Instance.GetString(data.note);
        }
        else
        {
            nameTxt.text = "알 수 없는 아이템";
            desTxt.text = $"ID: {id} 데이터 없음";
        }
    }
}
