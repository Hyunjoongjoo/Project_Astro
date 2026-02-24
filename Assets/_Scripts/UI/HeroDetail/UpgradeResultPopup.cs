using TMPro;
using UnityEngine;

public class UpgradeResultPopup : BaseUI
{
    [Header("스텟 텍스트 (Before -> After)")]
    [SerializeField] private TMP_Text _hpDiffTxt;
    [SerializeField] private TMP_Text _atkDiffTxt;
    [SerializeField] private TMP_Text _healDiffTxt;

    [Header("상승 수치 표시 (+Value)")]
    [SerializeField] private TMP_Text _hpPlusTxt;
    [SerializeField] private TMP_Text _atkPlusTxt;
    [SerializeField] private TMP_Text _healPlusTxt;

    public void Setup(HeroStatData oldStat, HeroStatData newStat, HeroStatData tableBase)
    {
        // 1. 체력 표시
        _hpDiffTxt.text = $"체력 : {oldStat.BaseHp} -> {newStat.BaseHp}";
        _hpPlusTxt.text = $"(+ {tableBase.ipLvHp})";

        // 2. 공격력 표시
        _atkDiffTxt.text = $"공격력 : {oldStat.baseAttackPower} -> {newStat.baseAttackPower}";
        _atkPlusTxt.text = $"(+ {tableBase.ipLvAttackPower})";

        // 3. 치유력 표시 (치유력이 0보다 클 때만 활성화 하거나 분기 처리)
        if (tableBase.baseHealingPower > 0 || tableBase.ipLvHealingPower > 0)
        {
            _healDiffTxt.text = $"치유력 : {oldStat.baseHealingPower} -> {newStat.baseHealingPower}";
            _healPlusTxt.text = $"(+ {tableBase.ipLvHealingPower})";
            _healDiffTxt.gameObject.SetActive(true);
        }
        else
        {
            _healDiffTxt.gameObject.SetActive(false);
            _healPlusTxt.text = "";
        }
    }
}
