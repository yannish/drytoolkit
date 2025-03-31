using System.Collections.Generic;
using System.IO;
using drytoolkit.Runtime.Animation;
using UnityEditor;
using UnityEngine;

namespace drytoolkit.Runtime.Animation
{
    public class ClipConfigUtil
    {
        [MenuItem("Assets/Wrap Animation Clip", true)]
        private static bool ValidateCreateWrapper()
        {
            bool selectedOnlyAnimClips = true;
            foreach (var selectedObject in Selection.objects)
            {
                if (selectedObject is AnimationClip)
                    continue;
                selectedOnlyAnimClips = false;
                break;
            }

            return selectedOnlyAnimClips;
        }

        [MenuItem("Assets//Wrap Animation Clip")]
        private static void CreateAnimationWrapper()
        {
            List<Object> createdClipConfigs = new List<Object>();
            foreach (var selectedObject in Selection.objects)
            {
                if (selectedObject is AnimationClip selectedClip)
                {
                    string path = AssetDatabase.GetAssetPath(selectedClip);
                    string directory = Path.GetDirectoryName(path);
                    string fileName = selectedClip.name + "_cc.asset";
                    string fullPath = Path.Combine(directory, fileName);

                    // Create ScriptableObject instance
                    ClipConfig clipConfig = ScriptableObject.CreateInstance<ClipConfig>();
                    clipConfig.clip = selectedClip;

                    // Save as an asset
                    AssetDatabase.CreateAsset(clipConfig, fullPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // Select the new asset
                    createdClipConfigs.Add(clipConfig);
                }
            }
            
            EditorUtility.FocusProjectWindow();
            Selection.objects = createdClipConfigs.ToArray();
            // Selection.activeObject = clipConfig;
        }
    }
}