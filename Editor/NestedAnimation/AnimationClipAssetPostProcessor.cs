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
            AnimatorControllerExtension.NotifyClipsChanged(); 
        }

        var all = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedAssets).Concat(movedFromPath).ToArray();
        
        foreach (var path in all)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ClipSet>(path);
            if(asset != null)
                Debug.LogWarning("this was a clipset!");
            AnimatorControllerExtension.NotifyClipSetsChanged();
        }
    }
}
