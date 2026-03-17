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

            // 첫 로드 및 버전 체크
            await FetchAndCheckVersion();

            Debug.Log("[VersionManager] 초기화 및 실시간 리스너 등록 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] 초기화 실패: {e.Message}");
        }
    }

    // 서버에서 값이 변경되었을 때 호출되는 이벤트
    private async void OnRemoteConfigUpdate()
    {
        Debug.Log("[VersionManager] 서버 설정 변경됨");

        await FetchAndCheckVersion();
    }

    private async Task FetchAndCheckVersion()
    {
        // 최신 설정값 가져오기
        await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());

        // 대시보드 키값 읽기
        string minVersionStr = RemoteConfigService.Instance.appConfig.GetString("MinVersion", "1.0.0");

        CheckVersion(minVersionStr);
    }

    private void CheckVersion(string minVersionStr)
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
                Debug.Log($"[VersionManager] 버전 체크 성공 : 현재({currentVersion}) >= 최소({minVersion})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VersionManager] 버전 비교 중 오류 : {e.Message}");
        }
    }

    private void ShowUpdatePopup()
    {
        if (_isPopupActive) return;
        _isPopupActive = true;

        Debug.LogError("[VersionManager] 업데이트 팝업 띄우기 시도");
        //업데이트 팝업 띄우기
    }

    private void OnDestroy()
    {
        if (RemoteConfigService.Instance != null)
        {
        }
    }
}
