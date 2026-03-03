//모든 데이터 클래스는 ID를 반출하도록
public interface ITableData
{
    string PrimaryID { get; } //읽기 전용
}