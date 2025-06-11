using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorControllerExtension : Editor
{
    public static event Action OnClipAssetsChanged;
    
    public static void NotifyClipsChanged() => OnClipAssetsChanged?.Invoke();

    
    [MenuItem("Assets/Generate Clip #&c", false, 2000)]
    private static void GenerateClip()
    {
        UnityEngine.Object selectedObject = Selection.activeObject;

        if (selectedObject is AnimatorController animatorController)
        {
            string path = AssetDatabase.GetAssetPath(animatorController);
            string directoryPath = Path.GetDirectoryName(path);

            string clipsFolderPath = Path.Combine(directoryPath, "Clips");
            if (!Directory.Exists(clipsFolderPath))
            {
                Directory.CreateDirectory(clipsFolderPath);
            }

            // Open a file save dialog to name the new clip
            string clipName = EditorUtility.SaveFilePanelInProject(
                "Save New Clip", 
                "NewClip",
                "anim",
                "Choose a name for the animation clip", 
                clipsFolderPath
                );
            
            if (string.IsNullOrEmpty(clipName)) 
                return;

            AnimationClip newClip = new AnimationClip();
            AssetDatabase.CreateAsset(newClip, clipName);
            AssetDatabase.SaveAssets();

            AddClipToAnimator(animatorController, newClip);

            // Focus the Animator window to view the new clip
            // EditorWindow.GetWindow(typeof(AnimatorControllerInspector));
            
            OnClipAssetsChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("Select an animator before trying to generate a clip for it.");
        }
    }

    private static void AddClipToAnimator(AnimatorController animatorController, AnimationClip clip)
    {
        AnimatorControllerLayer[] layers = animatorController.layers;
        AnimatorControllerLayer baseLayer = layers[0]; // Assuming you want to add to the first (base) layer

        AnimatorState newState = baseLayer.stateMachine.AddState(clip.name);

        newState.motion = clip;

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/Generate Clip", true)]
    private static bool ValidateGenerateClip()
    {
        UnityEngine.Object selectedObject = Selection.activeObject;
        return selectedObject is AnimatorController;
    }
}