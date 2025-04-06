using System;
using System.Collections.Generic;
using drytoolkit.Runtime.Animation;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Windows;
using File = System.IO.File;

public static class ClipEventGenerator
{
    private const string FILE_PATH = "Assets/ClipEvents.cs";
    private const string ASSET_PATH = "Assets";
    private const string CLASS_NAME = "ClipEvents";

    [MenuItem("Tools/Find All Clip Events")]
    public static void FindAllClipEvents()
    {
        var eventDefGUIDs = AssetDatabase.FindAssets("t:ClipEventDefinition");
        List<ClipEventDefinition> clipEventDefinitions = new List<ClipEventDefinition>();
        Debug.LogWarning($"found: {eventDefGUIDs.Length}");
        foreach (var guid in eventDefGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clipEventDef = AssetDatabase.LoadAssetAtPath<ClipEventDefinition>(path);
            clipEventDefinitions.Add(clipEventDef);
            Debug.LogWarning($"... def: {clipEventDef.name}");
        }
    }
    
    

    [MenuItem("Tools/Check name")]
    public static void CheckName()
    {
        Debug.LogWarning($"searching by {nameof(ClipHandler)}");
        var foundType = FindGeneratedScrObjType(nameof(ColorSwatches));
        if(foundType == null)
            Debug.LogWarning("... couldn't find type w/ name " + nameof(ClipHandler));
        else
        {
            Debug.LogWarning($"... found by {nameof(ColorSwatches)}");
        }
        
        Type FindGeneratedScrObjType(string className)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.Name == className)
                    return type;
            }
            return null;
        }
        
        
        // Debug.LogWarning(nameof(ClipHandler));
        Type FindGeneratedType(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(className);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
    
    [MenuItem("Tools/Generate Clip Events")]
    public static void GenerateClipEvents()
    {
        var eventDefGUIDs = AssetDatabase.FindAssets("t:ClipEventDefinition");
        List<ClipEventDefinition> clipEventDefinitions = new List<ClipEventDefinition>();
        foreach (var guid in eventDefGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clipEventDef = AssetDatabase.LoadAssetAtPath<ClipEventDefinition>(path);
            if(clipEventDefinitions.Contains(clipEventDef))
                continue;
            clipEventDefinitions.Add(clipEventDef);
        }

        string classContent = "using UnityEngine;\n\n";
        classContent += "public static class " + CLASS_NAME + "\n{\n";
        foreach (var foundEvent in clipEventDefinitions)
        {
            Debug.LogWarning($"... line for {foundEvent.name}");
            classContent += $"\t public static ClipEventDefinition {foundEvent.name};\n";
        }
        classContent += "}\n";
        
        File.WriteAllText(FILE_PATH, classContent);
        AssetDatabase.Refresh();

        CompilationPipeline.assemblyCompilationFinished -= InstantiateSwatchAfterCompilation;
        CompilationPipeline.assemblyCompilationFinished += InstantiateSwatchAfterCompilation;
    }

    private static void InstantiateSwatchAfterCompilation(string arg1, CompilerMessage[] arg2)
    {
        // Now that compilation is done, we can instantiate the ScriptableObject
        Debug.Log("Compilation finished! Instantiating the generated ScriptableObject...");

        Type generatedType = FindGeneratedType(CLASS_NAME);
        if (generatedType == null)
        {
            Debug.LogError($"Could not find class {CLASS_NAME}. Make sure Unity finished compiling.");
            return;
        }

        // Create and save the ScriptableObject
        ScriptableObject instance = ScriptableObject.CreateInstance(generatedType);
        if (instance == null)
        {
            Debug.LogError("Failed to instantiate the generated ScriptableObject.");
            return;
        }

        AssetDatabase.CreateAsset(instance, ASSET_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully created and saved {CLASS_NAME} at {ASSET_PATH}");
        
        Type FindGeneratedType(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(className);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
