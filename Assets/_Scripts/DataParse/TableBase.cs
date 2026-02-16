using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets; //어드레서블 추가
using UnityEngine.ResourceManagement.AsyncOperations; //AsyncOperationStatus용, 작업상태확인

//T는 클래스, ID가 필요, 생성 가능(인자값 없이)
public class TableBase<T> where T : class, ITableData, new()
{
    //데이터 저장 딕셔너리 (Key: id스트링, Val: 데이터 객체)
    public Dictionary<string, T> dataMap = new Dictionary<string, T>();

    //인자값으로 파일 경로를 받아서 데이터를 채워넣는 메서드
    //02.14 변경사항
    //1. 반환 타입 코루틴으로 변경
    //2. 파일 경로가 아닌 어드레서블 주소로 인자값 변경
    public IEnumerator LoadAsync(string address)
    {
        //어드레서블에게 로드 요청
        var handle = Addressables.LoadAssetAsync<TextAsset>(address);

        //로드될 때까지 대기
        yield return handle;

        //로드 결과 확인
        if (handle.Status == AsyncOperationStatus.Succeeded) 
        {
            TextAsset textAsset = handle.Result; 
             
            if (textAsset != null)
            {
                //로드된 텍스트 내용을 파서에게 전달
                List<T> list = CsvParser.Parse<T>(textAsset.text); 
                 
                //딕셔너리 초기화
                dataMap.Clear();  

                //리스트 => 딕셔너리 변환
                foreach (T item in list)
                {

                    if (!string.IsNullOrEmpty(item.PrimaryID) && !dataMap.ContainsKey(item.PrimaryID)) 
                    {
                        dataMap.Add(item.PrimaryID, item); 
                    }
                }

                Debug.Log($"[TableBase] 로드 성공: {address} (개수: {dataMap.Count})");
            }
            //데이터 파싱 끝났으니 텍스트 해제해도 됨
            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError($"어드레서블 로드 실패, 주소: {address}");
        }
    }

    //ID로 데이터 꺼내는 함수
    public T Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        //딕셔너리 확인
        if (dataMap.TryGetValue(id, out T value))
        {
            return value;
        }
        return null; // 못 찾으면 null 반환
    }

    //도감 같은 곳에서 전체 목록을 반환하는 메서드
    public List<T> GetAll()
    {
        return new List<T>(dataMap.Values);
    }
}
