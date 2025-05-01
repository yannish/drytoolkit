using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorControllerExtension : Editor
{
    // Adds a right-click option to generate an animation clip for an Animator Controller asset
    [MenuItem("Assets/Generate Clip", false, 2000)]
    private static void GenerateClip()
    {
        // Get the selected Animator Controller
        UnityEngine.Object selectedObject = Selection.activeObject;

        // Check if the selected asset is an Animator Controller
        if (selectedObject is AnimatorController animatorController)
        {
            // Get the directory where the Animator Controller is located
            string path = AssetDatabase.GetAssetPath(animatorController);
            string directoryPath = Path.GetDirectoryName(path);

            // Create a Clips folder if it doesn't exist
            string clipsFolderPath = Path.Combine(directoryPath, "Clips");
            if (!Directory.Exists(clipsFolderPath))
            {
                Directory.CreateDirectory(clipsFolderPath);
            }

            // Open a file save dialog to name the new clip
            string clipName = EditorUtility.SaveFilePanelInProject("Save New Clip", "NewClip", "anim", "Choose a name for the animation clip", clipsFolderPath);
            if (string.IsNullOrEmpty(clipName)) return;

            // Create a new animation clip
            AnimationClip newClip = new AnimationClip();
            AssetDatabase.CreateAsset(newClip, clipName);
            AssetDatabase.SaveAssets();

            // Add the new clip to the Animator Controller's base layer
            AddClipToAnimator(animatorController, newClip);

            // Focus the Animator window to view the new clip
            // EditorWindow.GetWindow(typeof(AnimatorControllerInspector));
        }
        else
        {
            Debug.LogError("Selected asset is not an Animator Controller.");
        }
    }

    // Adds the animation clip to the Animator Controller's base layer
    private static void AddClipToAnimator(AnimatorController animatorController, AnimationClip clip)
    {
        // Access the base layer of the Animator Controller
        AnimatorControllerLayer[] layers = animatorController.layers;
        AnimatorControllerLayer baseLayer = layers[0]; // Assuming you want to add to the first (base) layer

        // Create a new animation state with the clip
        AnimatorState newState = baseLayer.stateMachine.AddState(clip.name);

        // Set the clip to the new state
        newState.motion = clip;

        // Save the changes to the Animator Controller
        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
    }

    // This ensures that the "Generate Clip" option only appears for Animator Controllers
    [MenuItem("Assets/Generate Clip", true)]
    private static bool ValidateGenerateClip()
    {
        // Validate that the selected asset is an Animator Controller
        UnityEngine.Object selectedObject = Selection.activeObject;
        return selectedObject is AnimatorController;
    }
}