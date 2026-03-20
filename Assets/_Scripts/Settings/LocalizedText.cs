using UnityEngine;
using TMPro;

//텍스트가 들어가는 모든 TMP 오브젝트에 이 스크립트 추가.

//엑셀에 시작, 설정, 메뉴 등 ID 만들어주면?
//현재 TMP 오브젝트를 클릭해서 이 컴포넌트 추가 => 인스펙터의 StringId에 ui_btn_start 복붙


[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("String 테이블에 등록된 ID 적어주시면 됩니다")]
    [SerializeField] private string _stringId;

    private TextMeshProUGUI _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        RefreshText();

        //구독
        if (TableManager.Instance != null)
        {
            TableManager.Instance.OnLanguageChanged += RefreshText;
        }
    }

    private void OnDestroy()
    {
        //구취
        if (TableManager.Instance != null)
        {
            TableManager.Instance.OnLanguageChanged -= RefreshText;
        }
    }

    //번역 갱신 함수
    private void RefreshText()
    {
        if (string.IsNullOrEmpty(_stringId)) return;

        //매니저가 현재 언어에 맞는 텍스트를 전달
        _tmp.text = TableManager.Instance.GetString(_stringId);
    }

    //동적 생성되는 UI를 위한 함수
    //따로 없으면 삭제
    public void SetStringId(string newId)
    {
        _stringId = newId;
        RefreshText();
    }
}