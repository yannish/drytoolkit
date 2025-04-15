using UnityEngine;

public class ColorPicker<T> where T : ColorSwatches
{
    private const string folderName = "ColorPicker";
    
    private const string assetName = "ColorSwatches";
    
    public const string resourcesLoadPath = folderName + "/" + assetName;
    
    private static T _swatches;
    public static T Swatches
    {
        get
        {
            if(_swatches == null)
                _swatches = Resources.Load(resourcesLoadPath, typeof(T)) as T;
            return _swatches;
        }
    }
}