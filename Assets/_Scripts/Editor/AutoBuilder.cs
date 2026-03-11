using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using System.Linq;
using UnityEditor.Build.Reporting;

public class AutoBuilder
{
    //상단 메뉴바에 버튼 생성
    [MenuItem("Tools/Build/Build Addressables + APK", false, 10)]
    public static void BuildAll()
    {

        //어드레서블 빌드
        AddressableAssetSettings.BuildPlayerContent();

        //씬 목록 가져오기 
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        //빌드 설정
        string buildPath = "Builds/Android_TestBuild.apk";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        //안드로이드 빌드 실행
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"저장 위치: {summary.outputPath}");

            
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("APK 빌드 중 에러 발생, 빨간 줄 확인");
        }
    }
}