using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class VersionManager : Singleton<VersionManager>
{
    public struct userAttributes { }
    public struct appAttributes { }

    private bool _isPopupActive = false;

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
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] 초기화 실패: {e.Message}");
        }
    }

    private async Task FetchAndCheckVersion()
    {
        // 최신 설정값 가져오기
        await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
    }

    // ApplyRemoteConfig 방식 (데이터가 도착했을 때 실행)
    private void ApplyRemoteConfig(ConfigResponse configResponse)
    {
        // 응답 상태 확인
        if (configResponse.status != ConfigRequestStatus.Success)
        {
            Debug.LogWarning($"[VersionManager] 데이터 로드 실패 : {configResponse.status}");
            return;
        }

        // 데이터 출처 로그
        switch (configResponse.requestOrigin)
        {
            case ConfigOrigin.Default:
                Debug.Log("[VersionManager] 기본값 사용 중");
                break;
            case ConfigOrigin.Cached:
                Debug.Log("[VersionManager] 캐시된 데이터 사용 중");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("[VersionManager] 서버에서 새 데이터 로드 완료");
                break;
        }

        // 대시보드에 설정한 "MinVersion" 키값 읽기
        string minVersionStr = RemoteConfigService.Instance.appConfig.GetString("MinVersion", "1.0.0");

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
                Debug.Log($"[VersionManager] 버전 체크 통과 : 현재({currentVersion}) / 최소({minVersion})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] 버전 형식 오류 ({minVersionStr}) : {e.Message}");
        }
    }

    private void ShowUpdatePopup()
    {
        if (_isPopupActive) return;
        _isPopupActive = true;

        Debug.LogError("업데이트가 필요합니다. 팝업 띄움.");
        // 여기에 띄울 팝업 세팅하기(전역적으로 쓸 예정이니 프리팹화 된 패널 하나, 확인버튼 시 업데이트 URL 및 앱종료)
        // 팝업은 Panel, Text, button인데 "최신버전으로 업데이트가 필요합니다!" 일 뿐이니 따로 추가작업은 필요없을듯.
        // 프리팹화해둔 버튼 자체에 그냥 string으로 url입력해두고 [url + 앱종료] 호출만 하는 방식으로 처리하면 될듯?
    }

    private void OnDestroy()
    {
        if (RemoteConfigService.Instance != null)
        {
            RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteConfig;
        }
    }
}
