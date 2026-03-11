using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private Button _lobbyBtn;

    private void Start()
    {
        _lobbyBtn.onClick.AddListener(() =>
        {
            // 현재 씬에 있는 StageManager를 찾음
            var stageManager = FindFirstObjectByType<StageManager>();

            if (stageManager != null)
            {
                Debug.Log("[DebugUI] 스테이지 매니저를 통해 종료 및 로비 이동 시작");
                stageManager.ShutDownAndSceneChange();
            }
            
        });
    }
}
