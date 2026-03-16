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
    private Transform _topContainer;

    public Transform TopContainer => _topContainer; //탑컨테이너 참조가능하게 프로퍼티로

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        InitRoot();
    }

    //UI 루트 구성
    private void InitRoot()
    {
        if (_uiRoot != null) return;

        GameObject rootObj = GameObject.FindWithTag("MainCanvas"); //태그로 메인캔버스찾기

        if (rootObj != null)
        {
            // 씬에 이미 캔버스가 있다면 그걸 루트로 사용
            Canvas canvas = rootObj.GetOrAddComponent<Canvas>();
            rootObj.name = "@UI_Root";
        }
        else
        {
            // 없다면 새로 생성
            rootObj = new GameObject("@UI_Root");
            Canvas canvas = rootObj.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootObj.GetOrAddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            rootObj.GetOrAddComponent<GraphicRaycaster>();
        }

        _uiRoot = rootObj.transform;

        //컨테이너 생성 (이미 있으면 찾고, 없으면 생성)
        _windowContainer = FindOrCreateContainer("Windows", _uiRoot);
        _popupContainer = FindOrCreateContainer("Popups", _uiRoot);
        _topContainer = FindOrCreateContainer("TopLayer", _uiRoot);
    }
    private Transform FindOrCreateContainer(string name, Transform parent)
    {
        // 이미 자식으로 해당 컨테이너가 있는지 확인
        Transform container = parent.Find(name);
        if (container != null) return container;

        // 없으면 생성
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();

        // 화면에 꽉 차도록 앵커 설정
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;

        return obj.transform;
    }
    private void EnsureContainers()
    {
        // 씬이 바뀌어 참조가 깨졌거나 아직 초기화 전이라면 다시 잡음
        if (_uiRoot == null || _windowContainer == null || _popupContainer == null)
        {
            InitRoot();
        }
    }

    #region 통합 UI 관리
    public T ShowUI<T>(GameObject prefab,bool isPopup = true) where T : BaseUI
    {
        EnsureContainers(); //컨테이너랑 루트있는가 확인

        if (prefab == null) return null;

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

    public T ShowToast<T>(GameObject prefab,bool isTop = true ,float duration = 2.0f) where T : ToastUI
    {
        EnsureContainers(); //컨테이너랑 루트있는가 확인

        if (prefab == null) return null;

        
        Transform parent = isTop ? _topContainer : _popupContainer;
        GameObject obj = Instantiate(prefab, parent);
        T ui = obj.GetComponent<T>();

        if (ui != null)
        {
            ui.Show(duration);
        }

        return ui;
    }
}
