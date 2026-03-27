using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//영웅 증강 카드 스크립트
//Base 는 AugmentCardUI인데 작성되면 삭제할 예정임 AugmentCardUI를
public class HeroCardUI : MonoBehaviour, IAugmentUI, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Main UI")]
    [SerializeField] private Image _heroIconImg;       //기획서 1: 영웅 아이콘
    [SerializeField] private TMP_Text _titleTxt;       //기획서 2: 영웅 증강 이름 ("영웅 획득 : {0}")
    [SerializeField] private TMP_Text _descTxt;        //기획서 3: 영웅 증강 설명 ("영웅 {0}을/를 사용할 수 있습니다")

    [Header("Hero Info UI")]
    [SerializeField] private TMP_Text _heroTypeTxt;    //기획서 4-a: 영웅 유형 (우주 함선 등)
    [SerializeField] private TMP_Text _heroRoleTxt;    //기획서 4-b: 영웅 역할 (강습형 등)
    [SerializeField] private TMP_Text _moveTypeTxt;    //기획서 4-c: 이동 유형 (소형 등)
    [SerializeField] private TMP_Text _respawnTimeTxt; //기획서 5: 재소환 쿨타임 ("재소환 대기시간 : {0}초")

    [Header("Skill Info UI")]
    [SerializeField] private TMP_Text _skillNameTxt;   //기획서 6-a: 스킬 이름 ("스킬 [{0}]")
    [SerializeField] private TMP_Text _skillDescTxt;   //기획서 6-b: 스킬 설명
    [SerializeField] private TMP_Text _skillCoolTxt;   //기획서 7: 스킬 쿨타임 ("쿨타임: {0}초")

    [Header("Button")]
    [SerializeField] private Button _selectBtn;
    [SerializeField] private GameObject _highlightObj;

    [Header("Animation (DOTween)")]
    [SerializeField] private RectTransform _visualRoot; 
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _animDuration = 0.2f;

    private AugmentData _data;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _visualRoot.DOKill();

        _visualRoot.DOScale(_hoverScale, _animDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _visualRoot.DOKill();
        _visualRoot.DOScale(1f, _animDuration).SetEase(Ease.OutCubic);
    }

    //private bool _isClicked = false;

    public void Setup(AugmentData data)
    {
        _data = data;
        _selectBtn.interactable = true;


        //1. 기본 아이콘 세팅
        if (_heroIconImg != null) _heroIconImg.sprite = data.mainIcon;

        //2 & 3. 테이블 데이터 직접 호출 영진님이만든 겟스트링 사용(여기 코드의 겟스트링 메서드삭제)
        string nameFormat = TableManager.Instance.GetString("ui_augment_hero_name");
        string descFormat = TableManager.Instance.GetString("ui_augment_hero_des");

        if (_titleTxt != null) _titleTxt.text = string.Format(nameFormat, data.titleName);
        if (_descTxt != null) _descTxt.text = string.Format(descFormat, data.titleName);

        //4. 타입 정보 세팅
        if (_heroTypeTxt != null)
            _heroTypeTxt.text = TableManager.Instance.GetString($"hero_type_{data.heroType.ToString().ToLower()}_ingame");

        if (_heroRoleTxt != null)
            _heroRoleTxt.text = TableManager.Instance.GetString($"hero_role_{data.heroRole.ToString().ToLower()}");

        if (_moveTypeTxt != null)
            _moveTypeTxt.text = TableManager.Instance.GetString($"hero_movetype_{data.moveType.ToString().ToLower()}");

        //5. 재소환 쿨타임 세팅
        string respawnFormat = TableManager.Instance.GetString("ui_augment_hero_respawn");
        if (_respawnTimeTxt != null) _respawnTimeTxt.text = string.Format(respawnFormat, data.baseSpawnCooldown);

        //6 & 7. 스킬 정보 세팅 (기본 스킬이 있을 경우)
        if (data.skillData != null)
        {
            string skillNameFormat = TableManager.Instance.GetString("ui_augment_hero_skill_name");
            if (_skillNameTxt != null) _skillNameTxt.text = string.Format(skillNameFormat, TableManager.Instance.GetString(data.skillData.skillName));
            if (_skillDescTxt != null) _skillDescTxt.text = TableManager.Instance.GetString(data.skillData.skillDescription);

            string skillCoolFormat = TableManager.Instance.GetString("ui_augment_hero_skill_cooldown");
            if (_skillCoolTxt != null) _skillCoolTxt.text = string.Format(skillCoolFormat, data.skillData.cooldown);
        }

        //버튼 클릭 이벤트 세팅
        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(OnSelectClicked);
    }

    public void ToggleHighlight(bool isOn)
    {
        if (_highlightObj != null) _highlightObj.SetActive(isOn);
    }

    private void OnSelectClicked()
    {
        _visualRoot.DOPunchScale(new Vector3(-0.05f, -0.05f, 0), 0.1f);
        GetComponentInParent<AugmentWindowUI>().OnCardSelected(this, _data);
    }
}