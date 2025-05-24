using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using File = System.IO.File;

public static class ClipEventGenerator
{
    private const string FILE_PATH = "Assets/ClipEvents.cs";
    private const string CACHE_CLASS_FILE_PATH = "Assets/ClipEventsCache.cs";
    private const string ASSET_PATH = "Assets";
    private const string CLASS_NAME = "ClipEvents";
    private const string CACHE_CLASS_NAME = "ClipEventsCache";
    
    private static readonly string fullFilePath = Path.Combine("Assets", FILE_PATH, ".cs");
    private static readonly string fullCacheFilePath = Path.Combine("Assets", CACHE_CLASS_FILE_PATH, ".cs");

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
    
    // [MenuItem("Tools/Check name")]
    public static void CheckName()
    {
        Debug.LogWarning($"searching by {nameof(ColorSwatches)}");
        var foundType = FindGeneratedScrObjType(nameof(ColorSwatches));
        if(foundType == null)
            Debug.LogWarning("... couldn't find type w/ name " + nameof(ColorSwatches));
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
        // Type FindGeneratedType(string className)
        // {
        //     foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        //     {
        //         Type type = assembly.GetType(className);
        //         if (type != null)
        //             return type;
        //     }
        //     return null;
        // }
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

        // string classContent = "";
        
        WriteCacheClass(clipEventDefinitions);
        WriteGetterClass(clipEventDefinitions);
        
        // string classContent = "using UnityEngine;\n\n";
        // classContent += "public static class " + CLASS_NAME + " : ScriptableObject\n{\n";
        // foreach (var foundEvent in clipEventDefinitions)
        // {
        //     Debug.LogWarning($"... line for {foundEvent.name}");
        //     classContent += $"\t public static ClipEventDefinition {foundEvent.name};\n";
        // }
        // classContent += "}\n";
        //
        // File.WriteAllText(FILE_PATH, classContent);
        
        CompilationPipeline.assemblyCompilationFinished -= InstantiateSwatchAfterCompilation;
        CompilationPipeline.assemblyCompilationFinished += InstantiateSwatchAfterCompilation;
        // CompilationPipeline.
        
        AssetDatabase.Refresh();
    }

    private static void WriteGetterClass(List<ClipEventDefinition> clipEventDefinitions)
    {
        string classContent = "using UnityEngine;\n\n";
        classContent += "public static class " + CLASS_NAME + "\n{\n";
        // classContent += "\t public static " + CACHE_CLASS_NAME + " Cache;\n";
        foreach (var foundEvent in clipEventDefinitions)
        {
            Debug.LogWarning($"... line for {foundEvent.name}");
            classContent += $"\t public static ClipEventDefinition {foundEvent.name};\n";
        }
        classContent += "}\n";
        
        File.WriteAllText(FILE_PATH, classContent);
    }

    private static void WriteCacheClass(List<ClipEventDefinition> clipEventDefinitions)
    {
        string classContent = "using UnityEngine;\n\n";
        classContent += "public class static " + CACHE_CLASS_NAME + " : ScriptableObject\n{\n";
        
        foreach (var foundEvent in clipEventDefinitions)
        {
            Debug.LogWarning($"... line for {foundEvent.name}");
            classContent += $"\t public ClipEventDefinition {foundEvent.name};\n";
        }
        
        classContent += "}\n";
        
        File.WriteAllText(CACHE_CLASS_FILE_PATH, classContent);
    }

    private static void InstantiateSwatchAfterCompilation(string arg1, CompilerMessage[] messages)
    {
        // Now that compilation is done, we can instantiate the ScriptableObject
        Debug.Log($"Compilation finished with {messages.Length} messages.");

        bool hasErrors = false;
        foreach (var msg in messages)
        {
            Debug.LogError($"Compilation error w/ scriptGen : {msg.message}");// in {msg.file}");
            Debug.LogWarning($"... {msg.file}");
            if(msg.file == fullFilePath)
                Debug.LogWarning("FILE ISSUE CAUGHT");
            if(msg.file == fullCacheFilePath)
                Debug.LogWarning("CACHE FILE ISSUE CAUGHT");
            
            // if (msg.type == CompilerMessageType.Error && msg.file == CACHE_CLASS_FILE_PATH)
            // {
            //     hasErrors = true;
            //     Debug.LogError($"Compilation error w/ scriptGen : {msg.message} in {msg.file}");
            // }
        }

        if (hasErrors)
        {
            if (File.Exists(FILE_PATH))
            {
                File.Delete(FILE_PATH);
                File.Delete(FILE_PATH + ".meta");
                AssetDatabase.Refresh();
            }
            
            if (File.Exists(CACHE_CLASS_FILE_PATH))
            {
                File.Delete(CACHE_CLASS_FILE_PATH);
                File.Delete(CACHE_CLASS_FILE_PATH + ".meta");
                AssetDatabase.Refresh();
            }
            
            Debug.LogWarning("Generated script was deleted.");
        }
        
        CompilationPipeline.assemblyCompilationFinished -= InstantiateSwatchAfterCompilation;
        
        return;
        
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
