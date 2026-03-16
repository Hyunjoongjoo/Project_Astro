using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class InputValidator
{
    // 비속어 리스트 - 나중에 아마 DB든 Table이든 외부 관리 할듯?
    //private static readonly HashSet<string> BadWords = new HashSet<string> { "ㅅㅂ", "시발", "슈발" };
    private static readonly HashSet<string> BadWords = new HashSet<string>();

    // 2. 정규식 패턴들
    private static readonly string ScriptInjectionPattern = @"(<script.*?>|javascript:|on\w+=)"; // 기초적인 코드 공격 방지

    public static void InitializeBadWords()
    {
        BadWords.Clear();

        TextAsset csvFile = Resources.Load<TextAsset>("FilterData/slang");

        if (csvFile == null)
        {
            Debug.LogError("비속어 파일을 찾을 수 없습니다.");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string word = line.Trim().Replace("\"", "").Replace(",", "").ToLower();
            if (!string.IsNullOrEmpty(word))
            {
                BadWords.Add(word);
            }
        }
        Debug.Log($"비속어 리스트 로드 완료: {BadWords.Count}개 단어");
    }

    // string 필터링하기
    public static string ValidateAndClean(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        string cleaned = rawText;
        // 인젝션 공격 필터
        cleaned = Regex.Replace(cleaned, ScriptInjectionPattern, "", RegexOptions.IgnoreCase);
        // 이모지 필터
        cleaned = Regex.Replace(cleaned, @"[^a-zA-Z가-힣ㄱ-ㅎㅏ-ㅣ]", "");

        return cleaned;
    }

    // 필터링 된 string 받아서 검증
    public static void ValidateOrThrow(string rawText)
    {
        if (BadWords.Count == 0) InitializeBadWords();

        string filter = ValidateAndClean(rawText);

        string filterText = Regex.Replace(filter, @"[ㄱ-ㅎㅏ-ㅣ]", "");

        // 필터 후 빈값이 됐다면 문제있는 상황인것
        if (string.IsNullOrEmpty(filterText))
        {
            throw new Exception("허용되지 않는 문자(이모지/특수문자 등)만 포함되어 있습니다.");
        }

        // 인젝션 공격
        if (Regex.IsMatch(filterText, ScriptInjectionPattern))
            throw new Exception("허용되지 않는 문자가 포함되어 있습니다. (Code Injection)");

        // 비속어(리스트업된것들 중)
        foreach (var badWord in BadWords)
        {
            if (filterText.Contains(badWord))
                throw new Exception($"부적절한 단어가 포함되어 있습니다: {badWord}");
        }
    }
}