using UnityEngine;
using UnityEditor;
using UnityEngine.Networking; //시트 통신용
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class GoogleSheetImporter : EditorWindow
{
    //스프레드 시트 고유 ID 넣고
    private string _sheetId = "1OYp8Qpi3HPuoeZmpP4ssRck5V24W1sZCL6idIvUCerw";
    //경로설정
    private const string CsvSavePath = "Assets/_Data/Csv";
    private const string ClassSavePath = "Assets/_Scripts/TableData";

    //현재 테이블 이름으로 정보 가져오는 게 안돼서 GID로 직접 받을 예정
    ////테이블 목록이 적힌 관리용 시트
    //private const string MetaSheetName = "__TableList";

    //__TableList 시트의 GID
    private const string MetaSheetGID = "709900156";

    //일단 Data 내부에
    [MenuItem("Tools/Data/Open Sheet Importer")]
    public static void ShowWindow()
    {
        GetWindow<GoogleSheetImporter>("Sheet Importer");
    }

    //꾸미기
    private void OnGUI()
    {
        //라벨명 바꿀 수도
        GUILayout.Label("Google Sheets Downloader 입니다.", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //텍스트 입력 필드(시트 ID 바뀌었을 때 수정할 수 있도록)
        _sheetId = EditorGUILayout.TextField("Sheet ID 입력", _sheetId);

        EditorGUILayout.Space();

        //버튼 생성
        if (GUILayout.Button("Download", GUILayout.Height(40)))
        {
            DownloadAllSheets();
        }
    }



    //다운로드 로직 코루틴 시작
    private void DownloadAllSheets()
    {
        //저장폴더체크, 없으면 생성
        if(!Directory.Exists(CsvSavePath)) Directory.CreateDirectory(CsvSavePath);

        //코루틴 실행
        var routine = DownloadRoutine();

        //작업 끝나면 구독 해제하기위해 변수에 담아두기
        EditorApplication.CallbackFunction updater = null;

        updater = () =>
        {
            //MoveNext(): yield return을 만날 때까지 실행
            //더 이상 실행할 게 없으면 false를 반환하는 bool메서드
            if (routine != null && !routine.MoveNext())
            {
                routine = null;
                //매 프레임 호출되던 updater를 명단에서 제외 = 멈춤
                EditorApplication.update -= updater;
            }
        };

        //에디터의 업데이트 루프에 updater 함수 등록
        EditorApplication.update += updater;
    }

    //시트정보 GId 연결용 구조체
    struct SheetInfo
    {
        public string Name;
        public string Gid;
    }

    //URL 생성 전담 함수로 빼두는 게 나을 듯
    private string GetDownloadUrl(string gid)
    {
        return $"https://docs.google.com/spreadsheets/d/{_sheetId}/export?format=csv&gid={gid}";
    }

    //다운로드로직
    private IEnumerator DownloadRoutine()
    {
        Debug.Log("테이블 목록을 가져오기 1단");

        //메타 시트 다운로드
        string metaUrl = GetDownloadUrl(MetaSheetGID);
        List<SheetInfo> targetSheets = new List<SheetInfo>(); //다운받을 시트 이름과 gid 저장할 리스트

        //using을 쓰면 통신이 끝난 후 메모리를 알아서 청소한다고 함
        using (UnityWebRequest www = UnityWebRequest.Get(metaUrl))
        {
            //서버에 요청 보내고 응답이 올 때까지 yield return
            www.SendWebRequest();

            while (!www.isDone) yield return null;

            //통신 실패 체크
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"목록 시트를 찾을 수 없읆...");
                yield break;
            }

            //다운받은 텍스트를 줄바꿈 기준 잘라서 배열 생성
            //__TableList A열 1행부터 시트명 쭉 적을 예정
            string data = www.downloadHandler.text;
            string[] lines = data.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                if (cols.Length < 2) continue; //GID 없으면 무시

                string sheetName = cols[0].Trim();
                string sheetGid = cols[1].Trim();

                //빈 칸 X, 본인 X만 리스트에 추가
                if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(sheetGid)) continue;

                targetSheets.Add(new SheetInfo { Name = sheetName, Gid = sheetGid });
            }
        }

        Debug.Log($"목록 확인 => {targetSheets.Count}개의 시트 다운로드");


        //아래부턴 실제 엑셀 시트 다운로드 로직

        int successCount = 0;
        //targetSheets 순회하며 데이터 가져오기
        foreach (var info in targetSheets)
        {
            string url = GetDownloadUrl(info.Gid);

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SendWebRequest(); //대기
                while (!www.isDone) yield return null;


                //상당히 직관적이야
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{info.Name} => 다운로드 실패: {www.error}");
                }
                else
                {
                    //경로 결합
                    string filePath = Path.Combine(CsvSavePath, $"{info.Name}.csv");

                    //실제 파일 생성 및 내용 쓰기
                    File.WriteAllText(filePath, www.downloadHandler.text);

                    successCount++;
                }
            }
        }

        Debug.Log($"다운로드 완료 => {successCount}개의 CSV 파일 갱신.");

        //파일이 밖에서 만들어졌기 때문에 유니티 에디터는 파일 생성을 인식 못한다고 함
        //그래서 강제 리프레쉬
        AssetDatabase.Refresh();

        //TableClassGenerator 연결해서 스크립트 자동 생성
        TableClassGenerator.GenerateAllClassesInFolder(CsvSavePath, ClassSavePath);
    }
}