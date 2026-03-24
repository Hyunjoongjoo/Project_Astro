using UnityEngine;

public enum MacroType { Text,Emoticon}
[System.Serializable]
public class ChatMacroData
{
    public string id;       // 매크로 고유 ID
    public MacroType type;  // 텍스트, 이모티콘 구분

    [Header("Texts")]
    public string textKor;      // 한국어 텍스트
    public string textEng;      // 영어 텍스트

    public Sprite emoticonSprite; // 이모티콘 이미지

    // 현재 설정된 언어에 맞는 텍스트 반환
    public string GetText()
    {
        if (type == MacroType.Emoticon) return ""; // 이모티콘이면 텍스트 불필요

        // TableManager의 현재 언어 상태를 확인
        if (TableManager.Instance.CurrentLanguage == LanguageType.Eng)
        {
            return textEng;
        }
        return textKor;
    }
}
