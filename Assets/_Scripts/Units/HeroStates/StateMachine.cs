using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

// 상태를 관리해줄 StateMachine 클래스
public class StateMachine
{
    public IState CurrentState { get; private set; }
    public IState PreviousState { get; private set; }

    public void ChangeState(IState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void SavePreviousState()
    {
        PreviousState = CurrentState;
    }

    public void ChangePreviousState()
    {
        if (PreviousState == null) return;

        ChangeState(PreviousState);
    }

    public void Update()
    {
        CurrentState.Update();
    }
}
