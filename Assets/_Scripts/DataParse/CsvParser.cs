using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

//CSV 파일을 읽어서 C# 객체로 찍어내기

//02.14 어드레서블 도입 이후 텍스트를 받아와서 리스트로 변경만 하는 역할로 변경
public static class CsvParser
{
    //Parse<T> 메서드
    //어떤 데이터 타입이든 처리, where new T()로 만들 수 있는 클래스만
    //02.14 인자값 변경
    public static List<T> Parse<T>(string csvContent) where T : new()
    {
        List<T> list = new List<T>();

        //02.14 이제 안 읽어옴
        ////Resources에서 파일 읽어오기
        //TextAsset csvData = Resources.Load<TextAsset>(csvFileName);


        if (string.IsNullOrEmpty(csvContent))
        {
            return list;
        }

        //줄바꿈 처리
        string[] lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        //헤더가 3줄이니까 4줄은 넘겨야 데이터가 존재
        if (lines.Length < 4)
        {
            return list;
        }

        //Csv 2번째 줄 = 변수명 = Header = 컬럼명
        string[] headers = SplitCsvLine(lines[1]);

        //캐싱 해두기
        FieldInfo[] fieldCache = new FieldInfo[headers.Length];
        Type type = typeof(T);

        for (int i = 0; i < headers.Length; i++)
        {
            //Trim()도 여기서 미리 수행하여 GC 스파이크 방지
            string fieldName = headers[i].Trim();
            //딱 한 번만 찾기
            fieldCache[i] = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        }

        //4번째 줄(Index 3)부터 실제 데이터
        for (int i = 3; i < lines.Length; i++)
        {
            //쉼표 자르기
            string[] values = SplitCsvLine(lines[i]);
            if (values.Length == 0)
            {
                continue;
            }

            //빈 껍데기 객체 생성(ex. new MonsterData()) 
            T entry = new T();

            //값 채워넣기
            for (int j = 0; j < headers.Length; j++)
            {
                if (j >= values.Length)
                {
                    break;
                }
                //캐싱된 변수 쓰기
                FieldInfo field = fieldCache[j];

                //헤더와 일치하는 변수가 있고, 값도 있다면
                if (field != null)
                {
                    string value = values[j].Replace("\"\"\"", "").Replace("\"", "").Trim();

                    try
                    {
                        object finalValue = ConvertValue(value, field.FieldType);
                        field.SetValue(entry, finalValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CsvParser] 파싱 에러, 줄:{i + 1}, 컬럼:{headers[j]}, 값:{value}\n에러:{e}");
                    }
                }
            }
            //객체를 리스트에 추가
            list.Add(entry);
        }
        return list;
    }

    //CSV 쉼표 분리 (따옴표 내부 쉼표 무시)
    private static string[] SplitCsvLine(string line)
    {
        return Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    //타입 변환기
    //인자값 value는 string, type은 목표 타입
    //출력은 int bool 등 모든 타입이 될 수 있는 object
    //02.14 테이블 규격에 맞춰 변경
    private static object ConvertValue(string value, Type type)
    {
        if (type == typeof(int))
        {
            return int.TryParse(value, out int i) ? i : 0;
        }
        if (type == typeof(float))
        {
            return float.TryParse(value, out float f) ? f : 0f;
        }
        if (type == typeof(bool))
        {
            //02.14 테이블 규격에 맞춰 변경
            //공백 제거하고 소문자로 변환
            string lowerVal = value.Trim().ToLower();
            return lowerVal == "1" || lowerVal == "true" || lowerVal == "t";
        }
        if (type.IsEnum) //02.14 테이블 규격에 맞춰 변경
        {
            try
            {
                //CSV 값에서 언더바, 공백 제거
                string cleanValue = value.Replace("_", "").Trim();

                //대소문자 무시하고 Enum 찾기
                return Enum.Parse(type, cleanValue, true);
            }
            catch
            {
                Debug.LogWarning($"CSV 파싱 중, Enum 매칭 실패.   값: {value}");
                return 0;
            }
        }
        return value; // string
    }
}