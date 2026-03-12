using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMacroPresenter : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private ChatMacroView _view;
    [SerializeField] private ChatMacroDataSO _database;
    [SerializeField] private int invenSize = 8;

    [Header("카테고리 BG")]
    [SerializeField] private Image _textTabBg;
    [SerializeField] private Image _emoticonTabBg;

    // 현재 선택된 탭 상태
    private MacroType _currentTab = MacroType.Text;

    //장착 리스트
    private List<string> _equippedTextIds = new List<string>();
    private List<string> _equippedEmoticonIds = new List<string>();

    // 편집 중인 리스트
    private List<string> _editingTextIds = new List<string>();
    private List<string> _editingEmoticonIds = new List<string>();

    private void Start()
    {
        LoadFromPlayerPrefs();
        OnTabChanged(0);
    }

    private List<string> GetCurrentEquipped() => (_currentTab == MacroType.Text) ? _equippedTextIds : _equippedEmoticonIds;
    private List<string> GetCurrentEditing() => (_currentTab == MacroType.Text) ? _editingTextIds : _editingEmoticonIds;

    // 탭 버튼 클릭 시 호출
    public void OnTabChanged(int tabIndex) // 0: Text, 1: Emoticon
    {
        _currentTab = (MacroType)tabIndex;

        _textTabBg.color = (_currentTab == MacroType.Text) ? Color.skyBlue : Color.gray;
        _emoticonTabBg.color = (_currentTab == MacroType.Emoticon) ? Color.skyBlue : Color.gray;

        // 탭 변경 시 편집 리스트를 확정 데이터로부터 초기화
        _editingTextIds = new List<string>(_equippedTextIds);
        _editingEmoticonIds = new List<string>(_equippedEmoticonIds);

        UpdateView();
    }

    public void OnMacroSlotClicked(string macroId)
    {
        var editingList = GetCurrentEditing();

        if (editingList.Contains(macroId))
            editingList.Remove(macroId);
        else if (editingList.Count < invenSize)
            editingList.Add(macroId);

        UpdateView();
    }

    // 상단 X 버튼 클릭 - 확정 리스트와 편집 리스트 양쪽에서 즉시 제거
    public void OnRemoveEquippedMacro(string macroId)
    {
        var equippedList = GetCurrentEquipped();
        var editingList = GetCurrentEditing();

        if (equippedList.Remove(macroId))
        {
            editingList.Remove(macroId);
            UpdateView();
        }
    }

    public void OnSaveClicked()
    {
        _equippedTextIds = new List<string>(_editingTextIds);
        _equippedEmoticonIds = new List<string>(_editingEmoticonIds);

        SaveToPlayerPrefs();
        Debug.Log("저장 완료: 텍스트 " + _equippedTextIds.Count + "개, 이모티콘 " + _equippedEmoticonIds.Count + "개");
        UpdateView();
    }

    private void UpdateView()
    {
        // 현재 탭에 맞는 데이터만 필터링
        var filteredMacros = _database.allMacros.FindAll(m => m.type == _currentTab);

        _view.RefreshList(filteredMacros, GetCurrentEditing(), GetCurrentEquipped(), invenSize, OnMacroSlotClicked, OnRemoveEquippedMacro);
    }

    public void SaveToPlayerPrefs()
    {
        // 리스트를 콤마로 구분된 하나의 문자열로 변환하여 저장
        PlayerPrefs.SetString("EquippedTextIds", string.Join(",", _equippedTextIds));
        PlayerPrefs.SetString("EquippedEmoticonIds", string.Join(",", _equippedEmoticonIds));
        PlayerPrefs.Save();
    }

    private void LoadFromPlayerPrefs()
    {
        // 저장된 문자열을 불러와 리스트로 복원
        string textData = PlayerPrefs.GetString("EquippedTextIds", "");
        string emoticonData = PlayerPrefs.GetString("EquippedEmoticonIds", "");

        _equippedTextIds = string.IsNullOrEmpty(textData) ? new List<string>() : new List<string>(textData.Split(','));
        _equippedEmoticonIds = string.IsNullOrEmpty(emoticonData) ? new List<string>() : new List<string>(emoticonData.Split(','));

        // 로드 후 편집 리스트 초기화
        _editingTextIds = new List<string>(_equippedTextIds);
        _editingEmoticonIds = new List<string>(_equippedEmoticonIds);
    }
}
