using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailPopup : BaseUI
{
    [Header("메인 정보")]
    [SerializeField] private Image _heroPilotImg;
    [SerializeField] private TMP_Text _heroNameTxt;
    [SerializeField] private TMP_Text _heroRoleTxt;
    [SerializeField] private TMP_Text _heroTypeTxt;
    [SerializeField] private TMP_Text _descriptionTxt;

    [Header("하단 스와이프 패널")]
    [SerializeField] private SwipeUI _detailSwipeUI;
    [SerializeField] private GameObject[] _swipePages;

    private HeroData _currentData;

    public void Setup(HeroData data)
    {
        _currentData = data;

        //기본정보 매핑
        _heroNameTxt.text = data.heroName;
        _descriptionTxt.text = data.heroDesc;
        //_heroTypeTxt.text = data.heroType;    이넘때문에 일단 주석 이넘부분 처리완료되면 수정예정
        //_heroRoleTxt.text = data.heroRole;

        //이미지는 어드레서블로  나중에 연결시킬예정 like 어드레서블 매니저


        RefreshDetailPages();
    }

    private void RefreshDetailPages()
    {
        // 정보(스텟) 페이지 셋업
        //SetupStatPage(_currentData.heroStatId);

        // 프리뷰 영상 페이지 셋업
        SetupVideoPage(_currentData.heroPreviewVideo);

        // 스킬 정보 셋업
        SetupSkillPage(_currentData.skill);
    }

    private void SetupStatPage(string statId)
    {
        // 나중에 스텟 테이블 생기면 작업할 예정
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
