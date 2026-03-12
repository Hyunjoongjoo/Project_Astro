using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatMacroDataSO", menuName = "Scriptable Objects/ChatMacroDataSO")]
public class ChatMacroDataSO : ScriptableObject
{
    public List<ChatMacroData> allMacros;
}
