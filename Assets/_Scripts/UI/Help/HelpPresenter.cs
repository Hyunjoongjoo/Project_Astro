using System.Linq;
using UnityEngine;

public class HelpPresenter : MonoBehaviour
{
    [SerializeField] private HelpView _view;
    [SerializeField] private HelpIconDataSO _helpIcons;

    private void Start()
    {
        InitAllHelpData();
    }
    private void InitAllHelpData()
    {
        var allData = TableManager.Instance.HelpTable.GetAll();

        foreach (var data in allData)
        {
            Debug.Log($"[CSV로드체크] ID: {data.id}, Image: {data.image}, Category: {data.category}");
        }

        //카테고리별로 필터링 및 정렬
        var ingameData = allData.Where(d => d.category == Category.play).OrderBy(d => d.page).ToList();
        var augmentData = allData.Where(d => d.category == Category.augment).OrderBy(d => d.page).ToList();
        var itemData = allData.Where(d => d.category == Category.item).OrderBy(d => d.page).ToList();
        var heroData = allData.Where(d => d.category == Category.hero).OrderBy(d => d.page).ToList();

        //View에 데이터 전달하여 텍스트/이미지 셋팅
        _view.SetIngameContent(ingameData, _helpIcons);
        _view.SetAugmentContent(augmentData, _helpIcons);
        _view.SetItemContent(itemData, _helpIcons);
        _view.SetHeroContent(heroData, _helpIcons);
    }
}
