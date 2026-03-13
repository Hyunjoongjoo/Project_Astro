#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeroController))]
public class SOViewer : Editor
{
    public override void OnInspectorGUI()
    {
        HeroController controller = (HeroController)target;

        EditorGUILayout.LabelField("런타임 스킬 디버그 정보", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            if (controller.curUniqueSkill != null && controller.curUniqueSkill.Data != null)
            {
                //현재 장착된 스킬의 SO 가져옴
                BaseSkillSO currentSkillSO = controller.curUniqueSkill.Data;

                EditorGUILayout.HelpBox($"현재 장착된 스킬: {currentSkillSO.name}\n(타입: {currentSkillSO.skillType})", MessageType.Info);

            }
        }
        else
        {
            EditorGUILayout.HelpBox("런타임 스킬정보 표기용", MessageType.None);
        }

        EditorGUILayout.Space(15);
        base.OnInspectorGUI();
    }
}
#endif