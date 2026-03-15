using UnityEngine;
using Fusion;
//내 보관함 UI 3개와 영웅 카드들 캐싱해두고 StageManager에서 데이터 읽어와서 아이콘 이미지 갈아끼우는 역할
//변경된 네트워크 데이터를 감지하고 하단 패널을 새로고침하게 구현

public class ItemUIManager : Singleton<ItemUIManager>
{

    //3.16 자고일어나서 시작할 거

    //갱신 명령 시 실행할 함수

    //1. 채신 네트워크 데이터 가져오기
    //2. 인벤 3칸 랜더링
    //3. 영웅 장착슬롯 랜더링 => 컨테이너 하위 내 영웅카드 찾아서


    //아이템ID 받아서 아이콘 Sprite 반환하는 헬퍼 함수 필요한데
    //Resources말고 얘도 그냥 SO 하나 만들어서 아이디와 직렬화


    //아이템 ID 받아서 아이콘 스프라이트 출력하는 헬퍼함수도 필요

    //끝나고 아이템매니저쪽이나 여기에 RPC트리거 만들 거 UI랜더 새로고침하라는 RPC
    //Executor도 증강 선택 시 갱신하도록 추가해야하고



}
