using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;


public class CsvPostprocessor : AssetPostprocessor
{
    //감시 폴더와 보관할 그룹명
    private const string CsvFolder = "Assets/_Data/Csv";
    private const string TargetGroupName = "TableData";

    //어떤 변화라도 생기면 호출되도록 설정
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return; //세팅 파일 없으면 무시

        //TableData 그룹 체크 및 없으면 생성
        AddressableAssetGroup group = settings.FindGroup(TargetGroupName);
        if (group == null)
        {
            group = settings.CreateGroup(TargetGroupName, false, false, true, null);
        }

        bool isDirty = false;

        //파일검사
        foreach (string path in importedAssets)
        {
            //경로 + 확장자 체크
            if (path.StartsWith(CsvFolder) && Path.GetExtension(path).ToLower() == ".csv")
            {
                //고유 GUID 추출
                string guid = AssetDatabase.AssetPathToGUID(path);

                //그룹 등록
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
                
                //어드레스 설정 
                //Assets/_Data/Csv/Hero.csv 에서 Hero로 단축해서 등록
                string fileName = Path.GetFileNameWithoutExtension(path);
                entry.address = fileName;

                Debug.Log($"어드레서블 {fileName} 자동 등록 완료");
                isDirty = true;
            }
        }
        //변경사항 있으면 저장
        if (isDirty)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
