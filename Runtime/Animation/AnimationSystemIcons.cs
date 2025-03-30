using UnityEditor;
using UnityEngine;

namespace drytoolkit.Runtime.Animation
{
    [InitializeOnLoad]
    public static class AnimationSystemIcons
    {
        static AnimationSystemIcons()
        {
            Texture2D icon = TryFindIcon();
            if (icon == null) 
            {
                Debug.LogWarning("Custom icon not found! Make sure the path is correct.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:ClipConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null)
                {
                    EditorGUIUtility.SetIconForObject(obj, icon);
                    EditorUtility.SetDirty(obj);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Texture2D TryFindIcon()
        {
            Texture2D icon = EditorGUIUtility.Load("Assets/drytoolkit/Editor/Resources/d_VideoPlayer Icon.png") as Texture2D;
            if (icon != null)
            {
                return icon;
            }
        
            string packagePath = "Packages/com.drydock.drytoolkit/Editor/Resources/d_VideoPlayer Icon.png";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(packagePath);
            if (icon != null)
            {
                return icon;
            }
        }
    }
}