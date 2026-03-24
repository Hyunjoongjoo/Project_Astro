using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpView : MonoBehaviour
{
    [Header("인게임 규칙")]
    [SerializeField] private Image[] _ingamePageImgs;
    [SerializeField] private TMP_Text[] _ingamePageTxts;
    [Header("증강")]
    [SerializeField] private Image[] _augmentPageImgs;
    [SerializeField] private TMP_Text[] _augmentPageTxts;
    [Header("아이템")]
    [SerializeField] private Image[] _itemPageImgs;
    [SerializeField] private TMP_Text[] _itemPageTxts;
    [Header("영웅")]
    [SerializeField] private Image[] _heroPageImgs;
    [SerializeField] private TMP_Text[] _heroPageTxts;

    //특정 카테고리 배열에 데이터 채워넣기
    private void SetContent(Image[] imgs, TMP_Text[] txts, List<HelpData> dataList, HelpIconDataSO iconSO)
    {
        for (int i = 0; i < imgs.Length; i++)
        {
            if (i < dataList.Count)
            {
                // 이미지 설정 (SO에서 스프라이트 가져오기)
                if (iconSO != null) imgs[i].sprite = iconSO.GetIcon(dataList[i].image);

                // 텍스트 설정 (번역 테이블 참조)
                txts[i].text = TableManager.Instance.GetString(dataList[i].des);
            }
        }
    }

    //각 카테고리별로 외부에서 호출할 함수들
    public void SetIngameContent(List<HelpData> data, HelpIconDataSO iconSO) => SetContent(_ingamePageImgs, _ingamePageTxts, data, iconSO);
    public void SetAugmentContent(List<HelpData> data, HelpIconDataSO iconSO) => SetContent(_augmentPageImgs, _augmentPageTxts, data, iconSO);
    public void SetItemContent(List<HelpData> data, HelpIconDataSO iconSO) => SetContent(_itemPageImgs, _itemPageTxts, data, iconSO);
    public void SetHeroContent(List<HelpData> data, HelpIconDataSO iconSO) => SetContent(_heroPageImgs, _heroPageTxts, data, iconSO);
}
