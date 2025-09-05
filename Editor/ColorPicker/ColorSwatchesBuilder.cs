using UnityEngine;

// #if UNITY_EDITOR
using UnityEditor;
// #endif

// #if UNITY_EDITOR
[InitializeOnLoad]
// #endif
public static class ColorSwatchesBuilder
{
	private const string path = "Assets/Resources";

	private const string folderName = "ColorPicker";

	// private const string assetName = "ColorSwatches";

	private const string assetNameFull = "ColorSwatches.asset";

	private const string toolMenuPath = "Tools/ColorPicker/";

	// public const string resourcesLoadPath = folderName + "/" + assetName;


	// #if UNITY_EDITOR
	static ColorSwatchesBuilder() => EditorApplication.delayCall += CheckColorSwatches;
	// #endif	

	// #if UNITY_EDITOR
	[MenuItem(toolMenuPath + "Check Swatches at Runtime")]
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static void CheckColorSwatches()
	{
		var colorSwatchTypes = TypeCache.GetTypesDerivedFrom(typeof(ColorSwatches));
		if (colorSwatchTypes.Count == 0)
		{
			Debug.LogWarning("no classes derive from ColorSwatches, so we can't generate an instance.");
			return;
		}
		
		var foundSwatches = Resources.FindObjectsOfTypeAll<ColorSwatches>();
		if (foundSwatches != null && foundSwatches.Length > 0)
		{
			Debug.LogWarning("Found existing swatches.");
			return;
		}
		
		// var swatches = ScriptableObject.CreateInstance(colorSwatchTypes[0]);
		// // swatches.name = "ColorSwatches ---";
		//
		// if(!AssetDatabase.IsValidFolder($"{path}"))
		// 	AssetDatabase.CreateFolder("Assets", "Resources");
		//
		// if (!AssetDatabase.IsValidFolder($"{path}/{folderName}"))
		// 	AssetDatabase.CreateFolder(path, folderName);
		//
		// //...TODO: error here if there's no asset to delete...?
		// AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetNameFull}");
		// AssetDatabase.CreateAsset(swatches,$"{path}/{folderName}/{assetNameFull}");
		// AssetDatabase.SaveAssets();
		// AssetDatabase.Refresh();
	}
// #endif

	// #if UNITY_EDITOR
	[MenuItem(toolMenuPath + "Delete Color Swatches")]
	public static void DeleteColorSwatches() => AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetNameFull}");
	// #endif
}
