using Unity.VisualScripting;
using UnityEngine;
using static Fusion.Editor.FusionHubWindow;


//런타임 카드 객체
//덱 매니저가 기존 테이블을 읽고 런타임에 조립, UI 및 네트워크 컨트롤러로 전달
public class AugmentCard
{

    //이번 뽑기에서 생성된 카드 식별용
    //어떤 카드를 골랐는 지 알리기 위해 임의 지정하는 인스턴스값
    public string CardInstanceId;

    public AugmentType Type;

    //원본 csv ID
    public string ReferenceId;

    //UI 표기용
    public string Title;
    public string Description;
    public Sprite Icon;

    public AugmentCard(string cardInstanceId, AugmentType type, string referenceId, string title, string description, Sprite icon)
    {
        CardInstanceId = cardInstanceId;
        Type = type;
        ReferenceId = referenceId;
        Title = title;
        Description = description;
        Icon = icon;
    }

}
