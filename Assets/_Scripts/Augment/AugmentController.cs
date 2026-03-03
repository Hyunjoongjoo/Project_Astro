using Fusion;
using System.Collections.Generic;
using UnityEngine;

//증강 시스템의 전체 흐름을 통제하는 네트워크 컨트롤러
//경험치 감지 => 덱 픽업 => 서버 검증
public class AugmentController : NetworkBehaviour
{
    public static AugmentController Instance { get; private set; }

    //카드 생성용 매니저
    private AugmentDeckManager _deckManager;

    //유저 인게임 상태와 게이지 관리하는 스테이지 매니저
    private StageManager _stageManager;

    //스킬증강 SO 할당
    //추후 리소스로드 or 어드레서블로 관리
    [Header("스킬 증강 데이터베이스")]
    [SerializeField] private List<SkillAugmentSO> _allSkillAugments;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //오브젝트 스폰될 때 딱 한 번 호출
    public override void Spawned()
    {
        //할당된 SO 데이터를 바탕으로 DeckManager 가동
        _deckManager = new AugmentDeckManager(_allSkillAugments);

        //현재 씬에 있는 스테이지 매니저 찾아서 캐싱
        _stageManager = FindFirstObjectByType<StageManager>();
    }

    //HeroController에서 사용할 증강 SO 검색 함수26-03-03
    public SkillAugmentSO GetSkillAugmentById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (var so in _allSkillAugments)
        {
            if (so != null && so.AugmentID == id)
            {
                return so;
            }
        }

        return null;
    }


    //카드 3장 뽑아서 화면에 띄우기
    //플레이어 상태 분석해서 맞춤 카드 3장 띄우기
    //UI에서 증강선택 토글 버튼 눌렀을 때 호출
    public void OpenAugmentWindow()
    {
        if (_stageManager == null)
        {
            _stageManager = FindFirstObjectByType<StageManager>();
            if (_stageManager == null)
            {
                Debug.LogError("StageManager를 찾지 못함");
                return;
            }
        }
        Debug.Log("데이터 분석");
        PlayerNetworkData myData = _stageManager.PlayerDataMap.Get(Runner.LocalPlayer);

        //Config테이블에서 강화 달성 횟수 가져오기
        int reinforceNum = 6; //파싱 실패 시 기본값
        var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
        if (config != null) reinforceNum = int.Parse(config.configValue);


        //덱매니저 넘겨주기 위해 NetworkArray를 일반 List<string>으로 변환
        List<string> myHeroes = new List<string>();
        for (int i = 0; i < myData.OwnedHeroes.Length; i++)
        {
            if (myData.OwnedHeroes[i] != "")
            {
                myHeroes.Add(myData.OwnedHeroes[i].ToString());
            }
        }

        List<string> mySkills = new List<string>();
        for (int i = 0; i < myData.OwnedSkillAugments.Length; i++)
        {

            if (myData.OwnedSkillAugments[i] != "")
            {
                mySkills.Add(myData.OwnedSkillAugments[i].ToString());
            }

        }

        //내 아이템 슬롯이 꽉 찼는지 검사
        bool isItemFull = true;
        for (int i = 0; i < myData.InventoryItems.Length; i++)
        {
            if (myData.InventoryItems[i] == "")
            {
                isItemFull = false;
                break;
            }
        }



        //덱매니저 실행해서 카드 3장 AugmentData형태로 뽑아오기
        List<AugmentData> cards = _deckManager.GenerateCards(
            isFirstSelection: myData.TotalAugmentPicks == 0,
            ownedHeroIds: myHeroes,
            ownedSkillAugmentIds: mySkills,
            isItemFull: isItemFull,
            totalAugmentPicks: myData.TotalAugmentPicks,
            reinforceNumber: reinforceNum
        );

        Debug.Log($"생성된 카드 수: {cards.Count}");

        //뽑은 카드 팝업 띄우기
        if (cards.Count > 0) AugmentManager.Instance.ShowAugmentWindow(cards);

        else Debug.LogWarning("생성된 카드 없음");
    }


    //유저의 카드 선택 및 서버에 결재 올리기
    public void SelectAugment(AugmentData data)
    {
        //서버에 요청하기
        RPC_RequestSelectAugment(data.targetId, data.type);
    }

    //클라 => 서버로 증강 선택 요청하기
    //꽉찼나 체크
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSelectAugment(string targetId, AugmentType type, RpcInfo info = default)
    {
        //플레이어찾기
        PlayerNetworkData data = _stageManager.PlayerDataMap.Get(info.Source);
        bool isValid = true;

        //패킷이 날아오는 동안 슬롯이 꽉 찼는지 다시 한번 확인
        if (type == AugmentType.Hero && IsArrayFull(data.OwnedHeroes)) isValid = false;
        if (type == AugmentType.Item && IsArrayFull(data.InventoryItems)) isValid = false;
        if (type == AugmentType.Skill && IsArrayFull(data.OwnedSkillAugments)) isValid = false;

        //검증 통과 여부에 따라 컨펌 or 리젝트
        if (isValid)
        {
            RPC_ConfirmAugment(targetId, type, info.Source);
        }
        else
        {
            RPC_RejectAugment(info.Source);
        }
    }

    //통과
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ConfirmAugment(string targetId, AugmentType type, PlayerRef player)
    {
        //마스터 권한인 경우엔 PlayerNetworkData 업데이트
        if (Object.HasStateAuthority)
        {
            AugmentExecutor.ApplyAugment(_stageManager, player, type, targetId);
        }

        //카드를 산 사람이 로컬이 맞다면, UI를 갱신
        if (player == Runner.LocalPlayer)
        {
            if (type == AugmentType.Hero)
            {
                //id랑 타입만 받고 아이콘은 나중에
                AugmentData localData = new AugmentData
                {
                    targetId = targetId,
                    type = type
                };

                //HeroIconSO가 추가되면 아이콘로직 추가

                //조립된 데이터를 UI 매니저에게 넘겨서 하단 덱에 카드 슬롯을 추가
                AugmentManager.Instance.AddHeroCard(localData);
            }

            //서버 승인이 떨어졌으므로, 100 게이지를 차감
            _stageManager.DecreaseAugmentGauge(GameManager.Instance.PlayerTeam, 100);

            //전투 시작 전이면 카드 다 골랐다고 스테이지에 보고
            if (_stageManager.CurrentState == StageState.AugmentSelection)
            {
                _stageManager.RPC_ReportAugmentComplete(player);
            }
        }
    }

    //반려
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RejectAugment(PlayerRef player)
    {
        Debug.LogWarning("슬롯이 가득 차서 서버에서 장착 반려");
        //UI 팝업으로 슬롯이 가득 찼습니다 등등
    }


    //NetworkArray가 꽉 찼는지 검사

    private bool IsArrayFull(NetworkArray<NetworkString<_32>> array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == "") return false; //빈칸이 하나라도 있으면 false
        }
        return true;
    }
}
