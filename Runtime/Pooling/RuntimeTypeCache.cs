using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

// [CreateAssetMenu(fileName = "RuntimeTypeCache", menuName = "RuntimeTypeCache", order = 1)]
namespace Drydock.Tools
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

        public const string assetName = "RuntimeTypeCache.asset";


        private static readonly Type[] CachedAttributes = new Type[]
        {
            typeof(PooledAttribute),
        };


        [MenuItem("Tools/Delete Runtime Type Cache")]
        public static void DeleteCache()
        {
            AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetName}");
        }

        // [MenuItem("Tools/Load Runtime Type Cache")]
        public static RuntimeTypeCache LoadCache()
        {
            var runtimeTypeCache = AssetDatabase.LoadAssetAtPath<RuntimeTypeCache>($"{path}/{folderName}/{assetName}");
            if (runtimeTypeCache == null)
            {
                Debug.LogWarning("... couldn't find runtime type cache.");
                return null;
            }
            else
            {
                Debug.LogWarning("... found runtime type cache.");
            }
            return runtimeTypeCache;
        }


        [MenuItem("Tools/Build Runtime Type Cache")]
        public static void BuildRuntimeTypeCache()
        {
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

            // var fullPath = path + folderName + assetName;

            if (!AssetDatabase.IsValidFolder($"{path}/{folderName}"))
                AssetDatabase.CreateFolder(path, folderName);

            AssetDatabase.DeleteAsset($"{path}/{folderName}/{assetName}");
            AssetDatabase.CreateAsset(cache, $"{path}/{folderName}/{assetName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
