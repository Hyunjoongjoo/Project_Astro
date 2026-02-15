using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//02.14 로직 변경 => 모든 테이블을 찾아서 어드레서블로 로드시키도록 변경
public class TableManager : Singleton<TableManager>
{
    //테이블 목록
    //추가 시 제일 아래에 이어 작성
    public TableBase<ItemData> ItemTable = new TableBase<ItemData>();
    public TableBase<HeroData> HeroTable = new TableBase<HeroData>();
    public TableBase<ItemEffectData> ItemEffectTable = new TableBase<ItemEffectData>();
    public TableBase<ConfigData> ConfigTable = new TableBase<ConfigData>();
    public TableBase<StringData> StringTable = new TableBase<StringData>();


    protected override void Awake()
    {
        base.Awake();
        //코루틴으로 변경
        StartCoroutine(LoadAllData());
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
}