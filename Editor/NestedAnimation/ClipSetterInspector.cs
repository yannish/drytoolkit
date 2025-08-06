using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipSetter))]
public class ClipSetterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var clipSetter = (ClipSetter)target;

        DrawDefaultInspector();
        
        if (!Application.isPlaying)
            return;

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (int i = 0; i < clipSetter.cachedParentClips.Length; i++)
            {
                var parentClip = clipSetter.cachedParentClips[i];
                // using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    if (GUILayout.Button(parentClip.name.ToUpper()))
                    {
                        clipSetter.PlayParentClip(parentClip);
                    }
            }
        }
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (int i = 0; i < clipSetter.cachedNestedClips.Length; i++)
            {
                var nestedClip = clipSetter.cachedNestedClips[i];
                // using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    if (GUILayout.Button(nestedClip.name.ToUpper()))
                    {
                        clipSetter.PlayNestedClip(nestedClip);
                    }
            }
        }
    }
}
