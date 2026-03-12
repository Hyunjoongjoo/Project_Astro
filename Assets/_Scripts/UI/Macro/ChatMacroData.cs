using UnityEngine;

public enum MacroType { Text,Emoticon}
[System.Serializable]
public class ChatMacroData
{
    public string id;       // 매크로 고유 ID
    public MacroType type;  // 텍스트, 이모티콘 구분
    public string text;     // 실제 채팅 텍스트
    public Sprite emoticonSprite; // 이모티콘 이미지
}
