using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private ChatMacroDataSO _database;
    [SerializeField] private List<Button> _txtMacroBtns;
    [SerializeField] private List<Button> _emoticonBtns;
    [SerializeField] private GameObject _chatBubblePrefab;
    [SerializeField] private StageUI _stageUI;
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        ApplySavedMacros();
    }

    private void ApplySavedMacros()
    {
        // 1. 저장된 문자열 불러오기
        string textIds = PlayerPrefs.GetString("EquippedTextIds", "");
        string emotiIds = PlayerPrefs.GetString("EquippedEmoticonIds", "");

        string[] textList = textIds.Split(',');
        string[] emotiList = emotiIds.Split(',');

        // 2. 텍스트 버튼 적용
        for (int i = 0; i < _txtMacroBtns.Count; i++)
        {
            if (i < textList.Length && !string.IsNullOrEmpty(textList[i]))
            {
                var data = _database.allMacros.Find(m => m.id == textList[i]);
                if (data != null) SetupButton(_txtMacroBtns[i], data);
            }
            else _txtMacroBtns[i].gameObject.SetActive(false); // 데이터 없으면 버튼 숨김
        }

        // 3. 이모티콘 버튼 적용
        for (int i = 0; i < _emoticonBtns.Count; i++)
        {
            if (i < emotiList.Length && !string.IsNullOrEmpty(emotiList[i]))
            {
                var data = _database.allMacros.Find(m => m.id == emotiList[i]);
                if (data != null) SetupButton(_emoticonBtns[i], data);
            }
            else _emoticonBtns[i].gameObject.SetActive(false);
        }
    }

    private void SetupButton(Button btn, ChatMacroData data)
    {
        // 버튼 텍스트나 이미지를 data로 변경
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        var img = btn.GetComponent<Image>();

        if (data.type == MacroType.Text) txt.text = data.text;
        else img.sprite = data.emoticonSprite;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SendChat(data.id));
    }

    // 버튼 클릭 시 호출
    private void SendChat(string macroId)
    {
        // 서버에게 채팅 메시지 보내라고 요청 (RPC)
        RPC_SendChatMessage(Runner.LocalPlayer, macroId, IsTeamChatMode());
    }

    [Rpc(RpcSources.All, RpcTargets.All)] // 모든 클라이언트에게 전달
    private void RPC_SendChatMessage(PlayerRef sender, string macroId, bool isTeamChat)
    {
        // 1. 데이터베이스에서 ID로 데이터 찾기
        var macro = _database.allMacros.Find(m => m.id == macroId);
        if (macro == null) return;

        // 1. 메시지 보낸 사람과 나의 팀 정보 확인
        var senderData = _stageManager.PlayerDataMap.Get(sender);
        var myData = _stageManager.PlayerDataMap.Get(Runner.LocalPlayer);

        // 적군들의 PlayerRef만 뽑아서 배열로 생성
        List<PlayerRef> enemies = new List<PlayerRef>();
        foreach (var player in _stageManager.PlayerDataMap)
        {
            if (player.Value.Team != myData.Team && player.Key != Runner.LocalPlayer)
            {
                enemies.Add(player.Key);
            }
        }

        // 2. 앵커 조회 (StageUI에게 필요한 정보를 모두 전달)
        Transform anchor = _stageUI.GetChatAnchor(sender, Runner.LocalPlayer, senderData.Team, myData.Team, enemies.ToArray());

        if (anchor != null)
        {
            // 3. 말풍선 생성 및 부모 설정
            GameObject bubble = Instantiate(_chatBubblePrefab, anchor.position, Quaternion.identity);
            bubble.transform.SetParent(anchor, false); // UI 부모로 설정

            // 4. 내용 적용
            bubble.GetComponent<ChatBubbleUI>().Setup(macro);

            // 5. 2초 후 삭제
            Destroy(bubble, 2.0f);
        }
    }

    private bool IsTeamChatMode()
    {
        // 여기서 현재 토글 버튼 상태를 반환
        return false;
    }
}
