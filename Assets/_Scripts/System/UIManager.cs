using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* 
모든 UI 패널들은 BaseUI를 상속받으며 
UI 매니저를 통해서 관리되어야 한다.
나중에 모바일 환경임을 고려해서 리팩토링을 해야한다.
*/

public class UIManager : Singleton<UIManager>
{
    // 싱글톤이라 인스펙터에서 직접 꽂으면 씬 전환시 참조를 잃어버리는 문제 있음
    // 추 후 리팩토링 예정
    [Header("UI 부모 설정")]
    [SerializeField] private Transform _windowRoot;
    [SerializeField] private Transform _popupRoot;

    [Header("인풋 액션 연결")]
    [SerializeField] private InputActionReference _backAction;  //UI cancel 액션 연결용

    //현재 열려있는 팝업들 관리하는 스택 (뒤로가기 등에 활용)
    private Stack<BaseUI> _popupStack = new Stack<BaseUI>();

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        // 입력 이벤트 구독
        if (_backAction != null)
        {
            _backAction.action.Enable();
            _backAction.action.performed += OnBackInputPerformed;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (_backAction != null)
        {
            _backAction.action.performed -= OnBackInputPerformed;
        }
    }

    private void OnBackInputPerformed(InputAction.CallbackContext context)
    {
        if (_popupStack.Count > 0)
        {
            // 최상단 팝업 닫기
            _popupStack.Peek().OnBackButtonPressed();
        }
        else
        {
            // 팝업이 없을 때 로직 (예: 로비에서 게임 종료 팝업 띄우기)
        }
    }

    #region 윈도우 형 UI 관리

    public void OpenWindow(GameObject windowPrefab)
    {
        //기존 윈도우 비활성화하는 로직 추가 가능
        Instantiate(windowPrefab,_windowRoot);
    }

    #endregion

    #region 팝업 형 UI 관리
    public T ShowPopup<T>(GameObject prefab) where T : BaseUI
    {
        if (prefab == null) return null;

        //스택에 죽은 참조(파괴된 오브젝트)가 있다면 정리
        while (_popupStack.Count > 0 && _popupStack.Peek() == null)
        {
            _popupStack.Pop();
        }

        //토글로직, 같은버튼 한번더 누르면 닫기
        if (_popupStack.Count > 0)
        {
            BaseUI topUI = _popupStack.Peek();
            // 프리팹의 이름이나 클래스 타입을 비교 (여기서는 간단하게 클래스 타입으로 비교)
            if (topUI is T)
            {
                CloseTopPopup();
                return null;
            }
        }

        GameObject obj = Instantiate(prefab, _popupRoot);
        T ui = obj.GetComponent<T>();

        if (ui != null)
        {
            ui.Open();
            _popupStack.Push(ui);
        }

        return ui;
    }

    public void CloseTopPopup()
    {
        if (_popupStack.Count > 0)
        {
            BaseUI ui = _popupStack.Pop();
            if (ui != null)
            {
                ui.Close();
            }
        }
    }
    #endregion

    // 1. 로그인 성공 후 로비 진입 시 호출
    //public void InitLobbyUI( 여기다 데이타 )
    //{
    //    // 여기에 계정레벨, 재화 UI 업데이트 로직 연결
    //   
    //}

    // 2. 매칭 시작 시 UI 처리
    public void ShowMatchingUI(bool isMatching)
    {
        // 매칭 취소 버튼이 포함된 UI 출력/숨김
    }

    // 3. 인게임 증강 선택
    public void ShowAugmentSelection()
    {
        // 게임 일시정지 상태에서 증강 UI 출력
    }

    // 4. 게임 결과창
    public void ShowResultUI(bool isWin)
    {
        // 승패 결과 및 보상 UI 출력
    }
}
