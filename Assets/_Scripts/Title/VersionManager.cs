using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class VersionManager : Singleton<VersionManager>
{
    [SerializeField] private GameObject _versionResultPrefab;
    [SerializeField] private TextMeshProUGUI _versionText;

    public struct userAttributes { }
    public struct appAttributes { }

    private bool _isPopupActive = false;

    private Coroutine _checkRoutine;

    private void Start()
    {
        _ = InitializeAsync();
        _versionText.text = $"v{Application.version}";
    }

    // 초기화 및 최초 버전 체크
    public async Task InitializeAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // Remote Config 접근 권한 확보
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // 수정 시 바로 업데이트 하라는 팝업 이벤트
            RemoteConfigService.Instance.FetchCompleted += ApplyRemoteConfig;

            // 첫 로드 및 버전 체크
            await FetchAndCheckVersion();

            Debug.Log("[VersionManager] 초기화 및 실시간 리스너 등록 완료");

            if (_checkRoutine != null)
            {
                StopCoroutine(_checkRoutine);
            }
            _checkRoutine = StartCoroutine(Co_PeriodicVersionCheck(10f));
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] 초기화 실패: {e.Message}");
        }
    }

    private async Task FetchAndCheckVersion()
    {
        // 최신 설정값 요청하기
        await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
    }

    // ApplyRemoteConfig 방식 (데이터가 도착했을 때 이벤트 실행)
    private void ApplyRemoteConfig(ConfigResponse configResponse)
    {
        // 응답 상태 확인
        if (configResponse.status != ConfigRequestStatus.Success)
        {
            Debug.LogWarning($"[VersionManager] : 데이터 로드 실패 : {configResponse.status}");
            return;
        }

        if(GameManager.Instance.FlowState == SceneState.Stage)
        {
            Debug.LogWarning("[VersionManager] : 현재 Stage씬이라 실패.");
            return;
        }

        // 대시보드에 설정한 "MinVersion" 키값 읽기
        string minVersionStr = RemoteConfigService.Instance.appConfig.GetString("MinVersion", "0.0.1");
        ProcessVersionCheck(minVersionStr);
    }

    // 버전 비교 로직
    private void ProcessVersionCheck(string minVersionStr)
    {
        try
        {
            Version minVersion = new Version(minVersionStr);
            Version currentVersion = new Version(Application.version);

            if (currentVersion < minVersion)
            {
                ShowUpdatePopup();
            }
            else
            {
                Debug.Log($"[VersionManager] : 버전 체크 통과 : 현재({currentVersion}) / 최소({minVersion})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] : 버전 오류 ({minVersionStr}) : {e.Message}");
        }
    }

    private void ShowUpdatePopup()
    {
        if (_isPopupActive) return;
        string updateUrl = RemoteConfigService.Instance.appConfig.GetString("UpdateURL", "https://play.google.com/");
        Debug.Log($"[VersionManager] : {updateUrl}");
        if (_versionResultPrefab != null)
        {
            _isPopupActive = true;
            //프리팹 생성하고?
            GameObject popup = Instantiate(_versionResultPrefab);

            //컴포넌트 찾고?
            var popupUI = popup.GetComponentInChildren<VersionPopupUI>();

            //할당을 한다.
            if (popupUI != null)
            {
                popupUI.Setup(updateUrl);
            }
        }
        else
        {
            Debug.Log("[VersionManager] : 팝업 프리팹이 없슴다.");
        }
    }

    private IEnumerator Co_PeriodicVersionCheck(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            // 매칭 플레이 중 이거나 이미 팝업이 떠 있다면 잠시 패스
            if (GameManager.Instance.FlowState == SceneState.Stage || _isPopupActive)
            {
                Debug.Log("[VersionManager] 게임 진행 중 또는 팝업이 떠있어 패스.");
                continue;
            }

            // 서버에 최신 데이터 요청
            _ = FetchAndCheckVersion();
        }
    }

    private void OnDestroy()
    {
        if (RemoteConfigService.Instance != null)
        {
            RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteConfig;
        }
    }
}
