using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoBuilder : EditorWindow
{
    private string _password = "";
    private bool _rememberMe = false;
    private const string PREFS_KEY = "KSPass";

    //빌드창 열기
    [MenuItem("Tools/Build/AutoBuilder", false, 10)]
    public static void Open()
    {
        var window = GetWindow<AutoBuilder>(true, "Build Settings", true);
        window.minSize = window.maxSize = new Vector2(350, 150);
        window.Show();
    }

    //창 활성화 시 한 번만 실행
    private void OnEnable()
    {
        LoadPrefs();
    }

    //레지스트리에서 저장된 비번 불러오기
    private void LoadPrefs()
    {
        string saved = EditorPrefs.GetString($"{Application.productName}_{PREFS_KEY}", "");
        if (!string.IsNullOrEmpty(saved))
        {
            _password = saved;
            _rememberMe = true;
        }
    }

    //UI 출력
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        _password = EditorGUILayout.PasswordField("Keystore Password", _password);
        _rememberMe = EditorGUILayout.Toggle("이 컴퓨터에 저장", _rememberMe);
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Build Start", GUILayout.Height(40)))
        {
            //비었으면 경고 팝업
            if (string.IsNullOrEmpty(_password))
            {
                EditorUtility.DisplayDialog("빌드 중단", "비밀번호를 입력하십쇼", "확인");
                return;
            }

            //체크됐으면 레지스트리에 저장, 해제됐으면 삭제
            if (_rememberMe)
            {
                EditorPrefs.SetString($"{Application.productName}_{PREFS_KEY}", _password);
            }
            else
            {
                EditorPrefs.DeleteKey($"{Application.productName}_{PREFS_KEY}");
            }

            //비번 들고 실제 빌드 시작
            BuildAll(_password);
            this.Close();
        }
    }
    public static void BuildAll(string password)
    {
        //키스토어 설정 적용
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystorePass = password;
        PlayerSettings.Android.keyaliasPass = password;

        //어드레서블 빌드
        AddressableAssetSettings.BuildPlayerContent();

        //씬 목록 가져오기 
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        //빌드 설정
        string date = DateTime.Now.ToString("MMdd_HHmm");
        string buildPath = $"Builds/ASTRO_Build_{date}.apk";

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

    //GitHub Actions 전용 빌드
    public static void BuildAndroidBatch()
    {
        try
        {
            //CI/CD 서버의 환경 변수에서 Keystore 비밀번호를 가져옴
            //3.25 추가 경로, 별명까지
            string keystorePass = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
            string keystorePath = Environment.GetEnvironmentVariable("KEYSTORE_PATH"); 
            string keyAlias = Environment.GetEnvironmentVariable("KEY_ALIAS");
            if (!string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keyaliasName = keyAlias;
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasPass = keystorePass;
            }
            else
            {
            }

            //어드레서블 자동 빌드
            AddressableAssetSettings.BuildPlayerContent();

            //빌드 활성화된 씬 수집
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            //빌드 경로 강제 고정
            string buildPath = "Builds/Android/AstroCommanders.apk";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            //APK 빌드 실행
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            //결과 반환
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("빌드성공");
                EditorApplication.Exit(0); //시스템 정상 종료
            }
            else
            {
                Debug.Log($"빌드실패 결과: {summary.result}");
                Debug.LogError($"에러 개수: {summary.totalErrors}");
                EditorApplication.Exit(1); //시스템 에러 종료
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"오류 발생");
            Debug.LogError($"Message: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}
