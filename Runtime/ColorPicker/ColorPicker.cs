using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ColorPicker<T> where T : ColorSwatches
{
    private const string path = "Assets/Resources";
    
    private const string folderName = "ColorPicker";
    
    private const string assetName = "ColorSwatches";
    
    private const string assetNameFull = "ColorSwatches.asset";

    
    public const string resourcesLoadPath = folderName + "/" + assetName;
    
    private static T _swatches;
    public static T Swatches
    {
        get
        {
            if(_swatches == null)
                _swatches = Resources.Load(resourcesLoadPath, typeof(T)) as T;

            #if UNITY_EDITOR
            if (_swatches == null)
            {
                if(!AssetDatabase.IsValidFolder($"{path}"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                if (!AssetDatabase.IsValidFolder($"{path}/{folderName}"))
                    AssetDatabase.CreateFolder(path, folderName);
                
                var swatches = ScriptableObject.CreateInstance(typeof(T));
                
                AssetDatabase.CreateAsset(swatches,$"{path}/{folderName}/{assetNameFull}");

                return swatches as T;
            }
            #endif
            
            return _swatches;
        }
    }
}