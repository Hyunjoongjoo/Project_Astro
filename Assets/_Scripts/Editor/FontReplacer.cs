using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FontReplacer : EditorWindow
{
    [MenuItem("Tools/Replace Font")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    // TMP 사용 시
    public TMP_FontAsset oldFont;
    public TMP_FontAsset newFont;

    void OnGUI()
    {
        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField("기존 폰트", oldFont, typeof(TMP_FontAsset), false);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("새 폰트", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace All"))
        {
            ReplaceInPrefabs();
            ReplaceInScenes();
        }
    }

    void ReplaceInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            bool changed = false;
            foreach (var tmp in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.font == oldFont)
                {
                    tmp.font = newFont;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }
    }

    void ReplaceInScenes()
    {
        // 현재 열린 씬 내 오브젝트 순회
        var allTMP = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var tmp in allTMP)
        {
            if (tmp.font == oldFont)
            {
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
            }
        }
    }
}
