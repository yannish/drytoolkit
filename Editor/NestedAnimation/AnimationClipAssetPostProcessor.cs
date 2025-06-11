using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationClipAssetPostProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
        bool hasNewClip = importedAssets.Any(path => path.EndsWith(".anim"));
        bool deletedClip = deletedAssets.Any(path => path.EndsWith(".anim"));
        if (hasNewClip || deletedClip)
        {
            AnimatorControllerExtension.NotifyClipsChanged(); // As in Option 1
        }
    }
}
