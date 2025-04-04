using UnityEngine;

public class ColorPicker<T> where T : ColorSwatches
{
    private static T _swatches;
    public static T Swatches
    {
        get
        {
            if(_swatches == null)
                _swatches = Resources.Load(ColorSwatchesBuilder.resourcesLoadPath, typeof(T)) as T;
            return _swatches;
        }
    }
}