using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailPopup : BaseUI
{
    [Header("메인 정보")]
    [SerializeField] private Image _heroPilotImg;
    [SerializeField] private TMP_Text _heroNameTxt;
    [SerializeField] private TMP_Text _heroTypeTxt;
    [SerializeField] private TMP_Text _heroRoleTxt;
    [SerializeField] private TMP_Text _descriptionTxt;

    [Header("하단 스와이프 패널")]
    [SerializeField] private SwipeUI _detailSwipeUI;
    [SerializeField] private GameObject[] _swipePages;

    [Header("스텟 페이지 설정")]
    [SerializeField] private Transform _statContainer;
    [SerializeField] private GameObject _statPanelPrefab;

    private HeroData _currentHeroData;

    public void Setup(HeroData heroData)
    {
        _currentHeroData = heroData;

        //기본정보 매핑
        _heroNameTxt.text = heroData.heroName;
        _descriptionTxt.text = heroData.heroDesc;
        _heroTypeTxt.text = heroData.heroType.ToString();
        _heroRoleTxt.text = heroData.heroRole.ToString();

        //이미지는 어드레서블로  나중에 연결시킬예정 like 어드레서블 매니저


        RefreshDetailPages();
    }

    private void RefreshDetailPages()
    {
        // 정보(스텟) 페이지 셋업
        SetupStatPage(_currentHeroData.heroStatId);

        // 프리뷰 영상 페이지 셋업
        SetupVideoPage(_currentHeroData.heroPreviewVideo);

        // 스킬 정보 셋업
        SetupSkillPage(_currentHeroData.skill);
    }

    private void SetupStatPage(string statId)
    {
        // 기존에 생성된 스텟 항목들 제거
        foreach (Transform child in _statContainer)
            Destroy(child.gameObject);

        Debug.Log($"요청하는 StatId: {statId}");
        HeroStatData status = HeroManager.Instance.GetStatus(statId);

        if (status == null)
        {
            Debug.LogError($"[HeroDetailPopup] {statId}에 해당하는 데이터를 HeroManager에서 찾을 수 없습니다! 딕셔너리를 확인하세요.");
            return;
        }

        AddStatItem("체력", status.BaseHp.ToString());
        AddStatItem("공격력", status.baseAttackPower.ToString());
        AddStatItem("치유력", status.baseHealingPower.ToString());
        AddStatItem("공격 속도", status.attackSpeed.ToString("F2"));
        AddStatItem("이동 속도", status.moveSpeed.ToString("F1"));
        AddStatItem("공격 범위", status.detectionRange.ToString("F1"));
        AddStatItem("재소환 대기시간", $"{status.spawnCooldown}초");
    }
    private void AddStatItem(string name, string value)
    {
        GameObject obj = Instantiate(_statPanelPrefab, _statContainer);
        var statItem = obj.GetComponent<StatPanelUI>();
        if (statItem != null)
        {
            statItem.SetStat(name, value);
        }
    }

    private void SetupVideoPage(string videoKey)
    {
        // VideoPlayer 컴포넌트에 videoKey(주소)를 할당하여 재생 준비
    }

    private void SetupSkillPage(string skillKey)
    {
        // 스킬 아이콘 및 설명 텍스트 세팅
    }
}
