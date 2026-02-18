// 이동 기능이 없는 고정된 구조물 클래스
// 현재는 메인 포탑, 서브 포탑밖에 없지만
// 구조물을 소환하는 스킬을 가진 영웅 가능성 염두해서 만들어둠.

public class Structure : UnitBase
{

    public override void Spawned()
    {
        base.Spawned();
    }

    public void SetTeam(Team myTeam)
    {
        team = myTeam;
    }
}
