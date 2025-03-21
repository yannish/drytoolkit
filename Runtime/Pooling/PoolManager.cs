using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Drydock.Tools
{
    [InitializeOnLoad]
    public static class PoolManager
    {
        static PoolManager() => EditorApplication.delayCall += RuntimeTypeCacheBuilder.BuildRuntimeTypeCache;

        private static Dictionary<Type, List<FieldInfo>> cachedTypeToFieldInfoLookup = new Dictionary<Type, List<FieldInfo>>();
        
        private static Dictionary<GameObject, Pool> prefabToPoolLookup = new Dictionary<GameObject, Pool>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void FetchFromRuntimeTypeCache()
        {
            // Debug.Log("Building type-to-field-info lookup from cache...");
            
            // var runtimeTypeCache = AssetDatabase.LoadAssetAtPath<RuntimeTypeCache>(RuntimeTypeCacheBuilder.path);
            // if (runtimeTypeCache == null)
            // {
            //     // Debug.LogWarning("... couldn't find runtime type cache.");
            //     return;
            // }

            var runtimeTypeCache = RuntimeTypeCacheBuilder.LoadCache();
            if (runtimeTypeCache == null)
            {
                Debug.LogWarning("... couldn't find runtime type cache.");
                return;
            }

            cachedTypeToFieldInfoLookup.Clear();
            
            foreach (var cachedType in runtimeTypeCache.cachedFields)
            {
                Type type = Type.GetType(cachedType.assemblyQualifiedName);
                if (type != null)
                {
                    if(!cachedTypeToFieldInfoLookup.ContainsKey(type))
                        cachedTypeToFieldInfoLookup.Add(type, new List<FieldInfo>());

                    foreach (var fieldname in cachedType.fieldNames)
                    {
                        var fieldInfo = type.GetField(fieldname);
                        if(fieldInfo != null)
                            cachedTypeToFieldInfoLookup[type].Add(fieldInfo);
                    }
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PrewarmPrefabsByPooledAttribute()
        {
            // Debug.Log("Prewarming...");
            
            /*
             *  1. have a list of types that have pooled field.
             *
             *  2. try and find the first instance of each of these types.
             *
             *  3. loop through their pooled fields and try to create a pool for that prefab.
             */
            
            foreach (var kvp in cachedTypeToFieldInfoLookup)
            {
                var method = typeof(UnityEngine.Object)
                    .GetMethod(
                        nameof(UnityEngine.Object.FindFirstObjectByType),
                        BindingFlags.Public | BindingFlags.Static,
                        null, // No custom binder
                        new Type[] { typeof(FindObjectsInactive) }, // Exact parameter types
                        null) // Get generic method
                    ?.MakeGenericMethod(kvp.Key); // Set runtime type as generic argument

                if (method != null)
                {
                    var firstFoundComponent = (UnityEngine.Object)method.Invoke(null, new object[]
                    {
                        FindObjectsInactive.Include,
                    });

                    if (firstFoundComponent == null)
                        continue;

                    // Debug.Log($"Found first object of type {kvp.Key.FullName} in scene:{(firstFoundComponent)}");

                    if (cachedTypeToFieldInfoLookup.TryGetValue(kvp.Key, out List<FieldInfo> memberList))
                    {
                        foreach (FieldInfo fieldInfo in memberList)
                        {
                            var prefabAsset = fieldInfo.GetValue(firstFoundComponent) as GameObject;
                            if (prefabAsset == null)
                            {
                                // Debug.LogWarning("pooled prefab wasn't assigned");
                                continue;
                            }
                            // Debug.LogWarning($"ref to actual prefab : {prefabAsset.name}");
                            GetPool(prefabAsset);
                        }
                    }
                }
            }
        }
        

        private static Pool GetPool(GameObject prefab) //... we prewarm here
        {
            if (prefabToPoolLookup.TryGetValue(prefab, out Pool foundPool))
                return foundPool;

            var newPool = CreatePool(prefab);
            prefabToPoolLookup.Add(prefab, newPool);

            return newPool;
        }

        public static Pool CreatePool(GameObject prefab)
        {
            var pool = new GameObject($"[POOL - {prefab.name}]").AddComponent<Pool>();
            
            var foundPoolHandle = prefab.GetComponent<PoolHandle>();
            if (foundPoolHandle != null)
            {
                //... if we find a pool-handle, we use its settings for the pool.
                pool.initialSize = foundPoolHandle.initialPoolSize;
                pool.growOnExhaustion = foundPoolHandle.growOnExhaustion;
            }

            pool.OnExhaustion += pool.growOnExhaustion ? () => pool.GrowPool(pool.exhaustionGrowSize) : pool.ClearPool;
            pool.prefab = prefab;
            pool.GrowPool(pool.initialSize);

            return pool;
        }

        public static T GetPooledInstance<T>(this GameObject prefab) where T : PoolHandle
        {
            var pool = GetPool(prefab);
            var pooledInstance = pool.GetPooledInstance<T>();
            pooledInstance.gameObject.SetActive(true);
            return pooledInstance;
        }

        public static T GetAndPlay<T>(
            this GameObject prefab,
            Vector3 pos,
            Quaternion rot = default,
            Transform parent = null
            ) where T : PoolHandle
        {
            var pooledInstance = GetPooledInstance<T>(prefab);
            pooledInstance.transform.SetParent(parent, true);
            foreach (var poolable in (pooledInstance as PoolHandle).poolables)
            {
                poolable.OnGetFromPool(pos, rot);
            }
            pooledInstance.transform.SetPositionAndRotation(pos, rot);
            return pooledInstance;
        }

        public static PoolHandle GetAndPlay(
            this GameObject prefab,
            Vector3 pos,
            Quaternion rot = default,
            Transform parent = null
            )
        {
            PoolHandle pooledInstance = GetAndPlay<PoolHandle>(prefab, pos, rot, parent);
            // // var pooledInstance = GetPooledInstance<PoolHandle>(prefab);
            // pooledInstance.transform.SetPositionAndRotation(pos, rot);
            return pooledInstance;
        }
    }
}