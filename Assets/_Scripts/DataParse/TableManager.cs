using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

//02.14 로직 변경 => 모든 테이블을 찾아서 어드레서블로 로드시키도록 변경
public class TableManager : Singleton<TableManager>
{
    //테이블 목록
    //추가 시 제일 아래에 이어 작성
    public TableBase<ConfigData> ConfigTable = new TableBase<ConfigData>();
    public TableBase<HeroData> HeroTable = new TableBase<HeroData>();
    public TableBase<HeroStatData> HeroStatTable = new TableBase<HeroStatData>();
    public TableBase<HeroLevelData> HeroLevelTable = new TableBase<HeroLevelData>();
    public TableBase<HeroLevelRewardData> HeroLevelRewardTable = new TableBase<HeroLevelRewardData>();
    public TableBase<ItemData> ItemTable = new TableBase<ItemData>();
    public TableBase<ItemEffectData> ItemEffectTable = new TableBase<ItemEffectData>();
    public TableBase<StringData> StringTable = new TableBase<StringData>();
    public TableBase<UnitData> UnitTable = new TableBase<UnitData>();
    public TableBase<SkillInfoData> SkillInfoTable = new TableBase<SkillInfoData>();
    public TableBase<MatchRewardData> MatchRewardTable = new TableBase<MatchRewardData>();

    //언어 설정 -> UI 출력 변경용 이벤트
    //로컬 설정 저장은 나중에 따로 뺄 수도?

    public LanguageType CurrentLanguage { get; private set; } = LanguageType.Kor;
    public event Action OnLanguageChanged;
    private const string LANGUAGE_PREFS_KEY = "SelectedLanguage";

    //언어변경메서드
    public void ChangeLanguage(LanguageType newLang)
    {
        if (CurrentLanguage == newLang) return; 

        CurrentLanguage = newLang;
        //변경된 언어 로컬에 저장
        PlayerPrefs.SetInt(LANGUAGE_PREFS_KEY, (int)newLang);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    protected override void Awake()
    {
        base.Awake();

        //저장된 언어 불러오기
        CurrentLanguage = (LanguageType)PlayerPrefs.GetInt(LANGUAGE_PREFS_KEY, (int)LanguageType.Kor);

        //코루틴으로 변경
        StartCoroutine(LoadAllData());
        InputValidator.InitializeBadWords();
    }


    private IEnumerator LoadAllData()
    {
        //TableManager의 모든 public 변수를 가져옴
        FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            //변수 타입이 TableBase로 시작하는 것만
            if (field.FieldType.Name.Contains("TableBase"))
            {
                //변수 이름을 가져오기

                //2.14 수정, 변수명에서 Table 떼고 어드레서블 주소 만들기
                string addressKey = field.Name.Replace("Table", "");

                //테이블 인스턴스를 가져옴
                object tableInstance = field.GetValue(this);

                //Load => LoadAsync 함수 실행
                MethodInfo loadMethod = field.FieldType.GetMethod("LoadAsync");

                if (loadMethod != null)
                {
                    //LoadAsync 실행 => Item테이블꺼 받고 => 인자값은? Item(주소값) => 인보크한거 코루틴이니 형변환
                    var loadRoutine = loadMethod.Invoke(tableInstance, new object[] { addressKey }) as IEnumerator;

                    if (loadRoutine != null)
                    {
                        //로드가 끝날 때까지 대기
                        yield return StartCoroutine(loadRoutine);
                    }
                }
            }
        }

        Debug.Log("[TableManager] 데이터 리플렉션 완료");
    }

    //3.20 리팩토링
    public string GetString(string id)
    {
        var data = StringTable.Get(id);
        if (data == null) return id; //데이터없으면 아이디리턴추가

        //현재 언어에따라 출력 변경
        return CurrentLanguage == LanguageType.Eng ? data.textEng : data.textKor; 
    }
}