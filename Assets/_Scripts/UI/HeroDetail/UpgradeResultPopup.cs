using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeResultPopup : BaseUI
{
    [Header("영웅 정보")]
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private Image _heroExpBar;
    [SerializeField] private TMP_Text _heroExpTxt;

    [Header("스텟 텍스트 (Before -> After)")]
    [SerializeField] private TMP_Text _hpDiffTxt;
    [SerializeField] private TMP_Text _atkDiffTxt;
    [SerializeField] private TMP_Text _healDiffTxt;
    [SerializeField] private GameObject _healPanel;

    [Header("상승 수치 표시 (+Value)")]
    [SerializeField] private TMP_Text _hpPlusTxt;
    [SerializeField] private TMP_Text _atkPlusTxt;
    [SerializeField] private TMP_Text _healPlusTxt;

    private readonly string growthColor = "#000BFF";  //성장 수치 글자 색

    public void Setup(HeroData heroData, HeroDbModel userHero,HeroStatData oldStat, HeroStatData newStat, HeroStatData tableBase, HeroIconDataSO iconSO)
    {
        //상단 영웅 정보 갱신
        _nameTxt.text = TableManager.Instance.GetString(heroData.heroName);
        _levelTxt.text = $"Lv. {userHero.level}";

        if (iconSO != null)
            _iconImg.sprite = iconSO.GetIcon(heroData.heroIcon);

        // 경험치 바는 레벨업 직후이므로 다음 레벨 데이터 참조
        var levelData = TableManager.Instance.HeroLevelTable.Get(userHero.level.ToString());
        if (levelData != null)
        {
            _heroExpBar.fillAmount = (float)userHero.exp / levelData.expRequirement;
            _heroExpTxt.text = $"{userHero.exp} / {levelData.expRequirement}";
        }
        else
        {
            _heroExpBar.fillAmount = 1f;
            _heroExpTxt.text = "MAX";
        }

        //체력 표시
        _hpDiffTxt.text = $"체력 : {oldStat.BaseHp} -> {newStat.BaseHp}";
        _hpPlusTxt.text = $"<color={growthColor}>(+ {tableBase.ipLvHp})</color>";

        //공격력 표시
        _atkDiffTxt.text = $"공격력 : {oldStat.baseAttackPower} -> {newStat.baseAttackPower}";
        _atkPlusTxt.text = $"<color={growthColor}>(+ {tableBase.ipLvAttackPower})</color>";

        //치유력 표시
        if (tableBase.baseHealingPower > 0 || tableBase.ipLvHealingPower > 0)
        {
            _healDiffTxt.text = $"치유력 : {oldStat.baseHealingPower} -> {newStat.baseHealingPower}";
            _healPlusTxt.text = $"<color={growthColor}>(+ {tableBase.ipLvHealingPower})</color>";
            _healDiffTxt.gameObject.SetActive(true);
            _healPlusTxt.gameObject.SetActive(true);
        }
        else
        {
            _healPanel.SetActive(false);
            _healDiffTxt.gameObject.SetActive(false);
            _healPlusTxt.gameObject.SetActive(false);
        }
    }
}
