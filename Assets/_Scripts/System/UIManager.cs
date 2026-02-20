using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


/* 
모든 UI 패널들은 BaseUI를 상속받으며 
UI 매니저를 통해서 관리되어야 한다.
나중에 모바일 환경임을 고려해서 리팩토링을 해야한다.
*/

public class UIManager : Singleton<UIManager>
{

    private Transform _uiRoot; // 모든 UI 들어가는 루트

    //현재 열려있는 팝업들 관리하는 스택 (뒤로가기 등에 활용)
    private Stack<BaseUI> _popupStack = new Stack<BaseUI>();

    //팝업형이랑 그외에 UI가 들어갈 컨테이너들
    private Transform _windowContainer;
    private Transform _popupContainer;

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        InitRoot();
    }

    //UI 루트 구성
    private void InitRoot()
    {
        if (_uiRoot != null) return;

        //Root UI 생성
        GameObject rootObj = new GameObject("@UI_Root");
        _uiRoot = rootObj.transform;
        DontDestroyOnLoad(rootObj);

        //캔버스 추가
        Canvas canvas = rootObj.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootObj.GetOrAddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootObj.GetOrAddComponent<GraphicRaycaster>();

        // 컨테이너 분리 (윈도우는 밑에, 팝업은 위에 쌓이도록)
        _windowContainer = CreateContainer("Windows", _uiRoot);
        _popupContainer = CreateContainer("Popups", _uiRoot);
    }
    private Transform CreateContainer(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return obj.transform;
    }

    #region 통합 UI 관리
    public T ShowUI<T>(GameObject prefab,bool isPopup = true) where T : BaseUI
    {
        if (prefab == null) return null;

        // 중복 팝업 토글 체크
        if (isPopup && _popupStack.Count > 0)
        {
            if (_popupStack.Peek() is T)
            {
                CloseTopPopup();
                return null;
            }
        }

        // 팝업이면 팝업 컨테이너에, 일반 윈도우면 윈도우 컨테이너에 생성
        Transform parent = isPopup ? _popupContainer : _windowContainer;
        GameObject obj = Instantiate(prefab, parent);
        T ui = obj.GetComponent<T>();

        if (ui != null)
        {
            ui.Open();
            if (isPopup) _popupStack.Push(ui);
        }

        return ui;


    }

    public void CloseTopPopup()
    {
        if (_popupStack.Count > 0)
        {
            // 죽은 참조 정리
            while (_popupStack.Count > 0 && _popupStack.Peek() == null)
                _popupStack.Pop();

            if (_popupStack.Count > 0)
            {
                BaseUI ui = _popupStack.Pop();
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
