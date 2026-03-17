using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections;

public class ChatManager : NetworkBehaviour
{
    [Header("매크로 데이터SO")]
    [SerializeField] private ChatMacroDataSO _database;
    [Header("기본 설정")]
    [SerializeField] private List<Button> _txtMacroBtns;
    [SerializeField] private List<Button> _emoticonBtns;
    [SerializeField] private GameObject _chatBubblePrefab;
    [SerializeField] private StageUI _stageUI;
    [SerializeField] private AnimUI _txtPanel;
    [SerializeField] private AnimUI _emoticonPanel;
    [SerializeField] private TMP_Text _toggleTxt;
    [SerializeField] private AnimUI _toastObject; //비어있을때 출력용 토스트 메시지

    private bool _isTeamChat = false; //기본 전체 채팅

    private void Start()
    {
        ApplySavedMacros();
    }
    // 텍스트 패널을 여는 메서드
    public void OpenTextPanel()
    {
        bool hasData = false;
        foreach (var btn in _txtMacroBtns) if (btn.gameObject.activeSelf) hasData = true;

        if (!hasData)
        {
            StopAllCoroutines();
            StartCoroutine(CO_ShowToastMsg());
            return;
        }

        if (_txtPanel.IsOpened)
        {
            _txtPanel.DeActivate();
        }
        else
        {
            _emoticonPanel.DeActivate(); // 반대쪽 끄기
            _txtPanel.Open();       // 이쪽 켜기
        }
    }

    // 이모티콘 패널을 여는 메서드
    public void OpenEmoticonPanel()
    {
        bool hasData = false;
        foreach (var btn in _emoticonBtns) if (btn.gameObject.activeSelf) hasData = true;

        if (!hasData)
        {
            StopAllCoroutines();
            StartCoroutine(CO_ShowToastMsg());
            return;
        }

        if (_emoticonPanel.IsOpened)
        {
            _emoticonPanel.DeActivate();
        }
        else
        {
            _txtPanel.DeActivate(); // 반대쪽 끄기
            _emoticonPanel.Open();       // 이쪽 켜기
        }
    }

    private IEnumerator CO_ShowToastMsg()
    {
        _toastObject.Open();
        yield return new WaitForSeconds(2.5f);
        _toastObject.DeActivate();
    }

    private void ApplySavedMacros()
    {
        // 저장된 문자열 불러오기
        string textIds = PlayerPrefs.GetString("EquippedTextIds", "");
        string emotiIds = PlayerPrefs.GetString("EquippedEmoticonIds", "");

        string[] textList = textIds.Split(',');
        string[] emotiList = emotiIds.Split(',');

        // 텍스트 버튼 적용
        for (int i = 0; i < _txtMacroBtns.Count; i++)
        {
            if (i < textList.Length && !string.IsNullOrEmpty(textList[i]))
            {
                var data = _database.allMacros.Find(m => m.id == textList[i]);
                if (data != null) SetupButton(_txtMacroBtns[i], data);
            }
            else _txtMacroBtns[i].gameObject.SetActive(false); // 데이터 없으면 버튼 숨김
        }

        // 이모티콘 버튼 적용
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
        var iconImg = btn.transform.Find("EmoticonIcon")?.GetComponent<Image>();

        if (data.type == MacroType.Text) txt.text = data.text;
        else iconImg.sprite = data.emoticonSprite;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            SendChat(data.id);

            var panelUI = btn.GetComponentInParent<AnimUI>();
            panelUI.DeActivate();
        });
    }

    // 버튼 클릭 시 호출
    private void SendChat(string macroId)
    {
        if (Runner == null)
        {
            Debug.LogError("ChatManager: Runner가 null입니다! 네트워크 연결 상태를 확인하세요.");
            return;
        }

        // 2. StageManager.Instance 체크
        if (StageManager.Instance == null)
        {
            Debug.LogError("ChatManager: StageManager.Instance가 null입니다! 씬에 StageManager가 있는지 확인하세요.");
            return;
        }
        // 서버에게 채팅 메시지 보내라고 요청 (RPC)
        RPC_SendChatMessage(Runner.LocalPlayer, macroId, IsTeamChatMode());
    }

    [Rpc(RpcSources.All, RpcTargets.All)] // 모든 클라이언트에게 전달
    private void RPC_SendChatMessage(PlayerRef sender, string macroId, bool isTeamChat)
    {
        var stageManager = StageManager.Instance;
        if (stageManager == null) return;

        // 데이터베이스에서 ID로 데이터 찾기
        var macro = _database.allMacros.Find(m => m.id == macroId);
        if (macro == null) return;

        //안전한 데이터 취득
        if (!stageManager.PlayerDataMap.TryGet(sender, out var senderData) || !stageManager.PlayerDataMap.TryGet(Runner.LocalPlayer, out var myData))
        {
            return;
        }

        // 차단한 유저는 여기서 즉시 리턴
        if (StageManager.Instance.IsBlocked(sender))
        {
            return; 
        }

        // 팀챗일때 팀다르면 수신 거부
        if (isTeamChat && senderData.Team != myData.Team && sender != Runner.LocalPlayer)
        {
            return;
        }

        // 적군들의 PlayerRef만 뽑아서 배열로 생성
        List<PlayerRef> enemies = new List<PlayerRef>();
        foreach (var player in stageManager.PlayerDataMap)
        {
            if (player.Value.Team != myData.Team && player.Key != Runner.LocalPlayer)
            {
                enemies.Add(player.Key);
            }
        }

        // 앵커 조회 (StageUI에게 필요한 정보를 모두 전달)
        Transform anchor = _stageUI.GetChatAnchor(sender, Runner.LocalPlayer, senderData.Team, myData.Team, enemies.ToArray());

        if (anchor != null)
        {
            // 말풍선 생성 및 부모 설정
            GameObject bubble = Instantiate(_chatBubblePrefab, anchor);
            RectTransform rect = bubble.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero; // 앵커의 정확한 위치로
                rect.localScale = Vector3.one;        // 스케일 깨짐 방지
            }
            else
            {
                bubble.transform.localPosition = Vector3.zero;
                bubble.transform.localScale = Vector3.one;
            }

            // 내용 적용
            bubble.GetComponent<ChatBubbleUI>().Setup(macro);

            // 2초 후 삭제
            Destroy(bubble, 2.0f);
        }
    }
    public void ToggleChatMode()
    {
        _isTeamChat = !_isTeamChat;

        _toggleTxt.text = _isTeamChat ? "팀원" : "전체";
    }
    private bool IsTeamChatMode()
    {
        // 여기서 현재 토글 버튼 상태를 반환
        return _isTeamChat;
    }

    //차단 토글버튼
    public void ToggleBlockPlayer(int slotIndex)
    {
        //슬롯 인덱스로 PlayerRef를 가져옴
        PlayerRef targetPlayer = StageManager.Instance.GetPlayerRefByIndex(slotIndex);

        if (targetPlayer == PlayerRef.None || targetPlayer == Runner.LocalPlayer)
            return; // 자기 자신은 차단 불가

        // 이미 차단된 상태면 해제, 아니면 차단
        if (StageManager.Instance.IsBlocked(targetPlayer))
        {
            StageManager.Instance.UnblockPlayer(targetPlayer);
            ToastMessageUI.Instance.ShowToast($"<color=green>차단 해제:</color> {targetPlayer}");
        }
        else
        {
            StageManager.Instance.BlockPlayer(targetPlayer);
            ToastMessageUI.Instance.ShowToast($"<color=red>차단 완료:</color> {targetPlayer}");
        }
    }

    //팀 > 전체 전환토글 인원수에 따라 숨김처리
    public void RefreshToggleButtons(int playerCount)
    {
        if (playerCount <= 2) // 1:1 상황
        {
            // 1:1이면 팀 채팅 토글 버튼도 숨김
            if (_toggleTxt != null) _toggleTxt.transform.parent.gameObject.SetActive(false);
        }
        else // 2:2 상황
        {
            // 팀 채팅 토글 버튼 표시
            if (_toggleTxt != null) _toggleTxt.transform.parent.gameObject.SetActive(true);
        }
    }
}
