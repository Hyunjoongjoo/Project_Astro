using Fusion;
using System.Collections.Generic;
using UnityEngine;

//증강 시스템의 전체 흐름을 통제하는 네트워크 컨트롤러
//경험치 감지 => 덱 픽업 => UI 호출 => 서버 검증
public class AugmentController : NetworkBehaviour
{
    //카드 생성용 매니저
    private AugmentDeckManager _deckManager;

    //스킬증강 SO 할당
    //추후 리소스로드 or 어드레서블로 관리
    [Header("스킬 증강 데이터베이스")]
    [SerializeField] private List<SkillAugmentSO> _allSkillAugments;
}
