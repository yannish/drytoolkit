using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace drytoolkit.Runtime.Pooling
{
    public class RuntimeTypeCache : ScriptableObject
    {
        [Serializable]
        public class CachedTypeEntry
        {
            public string assemblyQualifiedName;
        }

        [Serializable]
        public class CachedFieldEntry
        {
            public string assemblyQualifiedName;
            public List<string> fieldNames;
        }

        public List<CachedFieldEntry> cachedFields = new List<CachedFieldEntry>();
    }


    public static class RuntimeTypeCacheBuilder
    {
        public const string path = "Assets/Resources";

        public const string folderName = "TypeCache";
        
        public const string assetName = "RuntimeTypeCache";

        public const string assetNameFull = "RuntimeTypeCache.asset";
        
        public const string fullPath = path + folderName + assetNameFull;

        public const string resourceLoadPath = folderName + "/" + assetName;

        public const string toolMenuPath = "Tools/Pooling/";


        private static readonly Type[] CachedAttributes = new Type[]
        {
            typeof(PooledAttribute),
        };


        #if UNITY_EDITOR
        [MenuItem(toolMenuPath + "Delete Runtime Type Cache")]
        public static void DeleteCache() => AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetNameFull}");
        #endif
        
        #if UNITY_EDITOR
        [MenuItem(toolMenuPath + "Load Runtime Type Cache")]
        public static RuntimeTypeCache LoadCache()
        {
            var runtimeTypeCache = AssetDatabase.LoadAssetAtPath<RuntimeTypeCache>($"{path}/{folderName}/{assetNameFull}");
            if (runtimeTypeCache == null)
            {
                Debug.LogWarning("... couldn't find runtime type cache.");
                return null;
            }
            return runtimeTypeCache;
        }
        #endif

        //... this happens at edit time...
        #if UNITY_EDITOR
        [MenuItem(toolMenuPath + "Rebuild Runtime Type Cache")]
        public static void BuildRuntimeTypeCache()
        {
            Debug.LogWarning("Building runtime type cache");
            
            var cache = ScriptableObject.CreateInstance<RuntimeTypeCache>();

            Dictionary<Type, List<FieldInfo>> typeToFieldsLookup = new Dictionary<Type, List<FieldInfo>>();

            foreach (var attribute in CachedAttributes)
            {
                var foundFields = TypeCache.GetFieldsWithAttribute(attribute);
                foreach (var field in foundFields)
                {
                    if (!typeToFieldsLookup.ContainsKey(field.ReflectedType))
                        typeToFieldsLookup.Add(field.ReflectedType, new List<FieldInfo>());

                    typeToFieldsLookup[field.ReflectedType].Add(field);
                }
            }

            foreach (var kvp in typeToFieldsLookup)
            {
                foreach (var field in kvp.Value)
                {
                    var fieldNames = kvp.Value.Select(t => t.Name).ToList();
                    cache.cachedFields.Add(new RuntimeTypeCache.CachedFieldEntry()
                    {
                        assemblyQualifiedName = kvp.Key.AssemblyQualifiedName,
                        fieldNames = fieldNames
                    });
                }
            }

            if(!AssetDatabase.IsValidFolder($"{path}"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder($"{path}/{folderName}"))
                AssetDatabase.CreateFolder(path, folderName);

            AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetNameFull}");
            AssetDatabase.CreateAsset(cache, $"{path}/{folderName}/{assetNameFull}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        #endif
    }
}
