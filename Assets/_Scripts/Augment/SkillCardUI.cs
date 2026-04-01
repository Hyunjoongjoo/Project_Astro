using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillCardUI : MonoBehaviour, IAugmentUI
{
    [Header("Main UI")]
    [SerializeField] private Image _skillIconImg;        //기획서 1: 스킬 아이콘
    [SerializeField] private TMP_Text _titleTxt;         //기획서 3: 증강 스킬 이름
    [SerializeField] private TMP_Text _descTxt;          //기획서 4: 스킬 증강 설명 ("영웅 {0}의 스킬을 강화합니다")

    [Header("Hero Info UI")]
    [SerializeField] private Image _heroIconImg;         //기획서 2: 영웅 얼굴 아이콘

    [Header("Skill Upgrade Info UI")]
    [SerializeField] private TMP_Text _baseSkillNameTxt; //기획서 5-a: 원본 스킬 이름 ("스킬 [{0}] 업그레이드")
    [SerializeField] private TMP_Text _upgradeDescTxt;   //기획서 5-b: 증강 스킬 설명 (Tiers에 적힌 내용)
    [SerializeField] private TMP_Text _skillCoolTxt;     //기획서 5-c: 스킬 쿨타임 (CombatSkillData의 쿨타임)

    [Header("Button")]
    [SerializeField] private Button _selectBtn;
    [SerializeField] private GameObject _highlightObj;

    [Header("Animation (DOTween)")]
    [SerializeField] private RectTransform _visualRoot;
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _animDuration = 0.2f;

    private AugmentData _data;
    //private bool _isClicked = false;

    public void Setup(AugmentData data)
    {
        _data = data;
        //_isClicked = false;
        _selectBtn.interactable = true;

        //1 & 2. 아이콘 세팅
        if (_skillIconImg != null) _skillIconImg.sprite = data.mainIcon;
        if (_heroIconImg != null) _heroIconImg.sprite = data.targetHeroIcon;

        //3. 증강 이름 세팅 (테이블에서 꺼내온 이름)
        if (_titleTxt != null) _titleTxt.text = data.titleName;

        //4. 스킬 증강 설명 세팅
        string descFormat = TableManager.Instance.GetString("ui_augment_skill_des");
        if (_descTxt != null) _descTxt.text = string.Format(descFormat, data.targetHeroName);

        //5. 스킬 업그레이드 정보 세팅
        string baseSkillFormat = TableManager.Instance.GetString("ui_augment_skill_name_format");
        if (_baseSkillNameTxt != null)
            _baseSkillNameTxt.text = string.Format(baseSkillFormat, TableManager.Instance.GetString(data.baseSkillName));

        //업그레이드 상세 설명은 이미 덱 매니저에서 텍스트로 완성해서 보내줌
        if (_upgradeDescTxt != null) _upgradeDescTxt.text = data.description;

        //스킬 쿨타임 세팅
        if (data.skillData != null && _skillCoolTxt != null)
        {
            string skillCoolFormat = TableManager.Instance.GetString("ui_augment_hero_skill_cooldown");
            _skillCoolTxt.text = string.Format(skillCoolFormat, data.skillData.cooldown);
        }

        //버튼 클릭 이벤트 세팅
        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(OnSelectClicked);
    }

    public void ToggleHighlight(bool isOn)
    {
        if (_highlightObj != null) _highlightObj.SetActive(isOn);
    }

    //나중엔 닫기 대신 확정 버튼 활성화로 로직 변경할 예정
    private void OnSelectClicked()
    {
        _visualRoot.DOKill();
        GetComponentInParent<AugmentWindowUI>().OnCardSelected(this, _data);
        _visualRoot.DOPunchScale(new Vector3(-0.05f, -0.05f, 0), 0.1f);

        //if (_isClicked) return;
        //_isClicked = true;
        //_selectBtn.interactable = false;

        //AugmentManager.Instance.SelectAugment(_data);
        //GetComponentInParent<AugmentWindowUI>().Close();
    }
}