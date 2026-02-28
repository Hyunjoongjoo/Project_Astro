using UnityEngine;

//증강 카드를 선택했을 때 실행될 공통 커맨드

public interface IAugmentAction
{
    /// <summary>
    ///카드를 선택했을 때 즉시 실행될 로직
    /// </summary>
    /// <param name="cardData">선택한 카드의 런타임 데이터</param>
    /// <param name="playerId">이 카드를 선택한 유저의 고유 ID ViewID를 써야하나? 일단 고민</param>
    void Execute(AugmentCard cardData, string playerId);
}