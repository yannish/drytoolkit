using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;

[InitializeOnLoad]
public static class AnimationSystemIcons
{
    // Assets/drytoolkit/Editor/Resources/d_VideoPlayer Icon.png
    
    static AnimationSystemIcons()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/drytoolkit/Editor/Resources/d_VideoPlayer Icon.png");
        if (icon == null) 
        {
            Debug.LogWarning("Custom icon not found! Make sure the path is correct.");
            return;
        }

        // Find all instances of the ScriptableObject type
        string[] guids = AssetDatabase.FindAssets("t:ClipConfig"); // Change to your class name
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj != null)
            {
                EditorGUIUtility.SetIconForObject(obj, icon);
                EditorUtility.SetDirty(obj); // Mark the asset as modified
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Try Find Animation Icon")]
    static void TryFindIcon()
    {
        Texture2D foundTex = EditorGUIUtility.Load("Assets/drytoolkit/Editor/Resources/d_VideoPlayer Icon.png") as Texture2D;
        if (foundTex != null)
        {
            Debug.LogWarning("Found tex");
            return;
        }
        
        string packagePath = "Packages/com.drydock.drytoolkit/Editor/Resources/d_VideoPlayer Icon.png";
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(packagePath);
        if (icon != null)
        {
            Debug.LogWarning("... found tex the other way.");
            return;
        }
    }
}