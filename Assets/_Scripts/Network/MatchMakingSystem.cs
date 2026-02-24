using DG.Tweening;
using Fusion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public enum MatchType { OneVsOne, TwoVsTwo }

public class MatchMakingSystem : MonoBehaviour
{
    [SerializeField] private GameObject _runnerPrefab;
    [SerializeField] private GameObject _matchMakingPanel;
    [SerializeField] private Button _cancelBtn;

    private NetworkRunner _networkRunner;
    private bool _isMatching = false;

    private int _height;

    private void Awake()
    {
        _cancelBtn.interactable = false;
        _height = Screen.height;
    }

    public async void OnClickMatchMaking(int matchType)
    {
        if (_isMatching) return; // 중복 클릭 방지

        _isMatching = true;

        GameObject obj = Instantiate(_runnerPrefab);
        DontDestroyOnLoad(obj);
        _networkRunner = obj.GetComponent<NetworkRunner>();
        obj.GetComponent<MatchMakingRunner>().Initialize(_cancelBtn, _networkRunner);

        _matchMakingPanel.transform.DOMoveY(0, 0.8f).SetEase(Ease.OutCubic);
        await OnConnected((MatchType)matchType);
    }

    public async void OnClickCancelMatching()
    {
        _cancelBtn.interactable = false;
        _isMatching = false;

        await _networkRunner.Shutdown();

        _matchMakingPanel.transform.DOMoveY(_height + 1, 0.8f).SetEase(Ease.OutCubic);
    }

    public async Task OnConnected(MatchType matchType)
    {
        var sessionProps = new Dictionary<string, SessionProperty>
        {
            ["matchType"] = (int)matchType
        };

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            PlayerCount = matchType == MatchType.OneVsOne ? 2 : 4,
            SessionProperties = sessionProps
        };

        var result = await _networkRunner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError($"매칭 실패: {result.ErrorMessage}");
            // 실패 시 UI 원위치
            _matchMakingPanel.transform.DOMoveY(2340, 0.8f).SetEase(Ease.OutCubic);
        }
    }
}
