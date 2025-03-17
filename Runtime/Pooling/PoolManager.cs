using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;
using System.Reflection;
#endif

namespace Drydock.Tools
{

// [InitializeOnLoad]
    public static class PoolManager
    {
        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void RefreshPooledClasses()
        {
            Debug.Log("Assemblies loaded for pool manager...");
            pooledTypes.Clear();
            pooledMembers.Clear();
            FetchFromTypeCache();
        }

        private static void FetchFromTypeCache()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var typeCollection = TypeCache.GetTypesWithAttribute<PooledAttribute>();
            foreach (var field in typeCollection)
            {
                Debug.Log(field.BaseType.Name);
            }

            var fieldCollection = TypeCache.GetFieldsWithAttribute<PooledAttribute>();
            foreach (var field in fieldCollection)
            {
                Debug.LogWarning(field.ReflectedType.FullName);
                Debug.LogWarning(field.Name);

                if (!typeToFieldInfoLookup.ContainsKey(field.FieldType))
                {
                    typeToFieldInfoLookup.Add(field.ReflectedType, new List<FieldInfo>());
                }

                if (typeToFieldInfoLookup.TryGetValue(field.ReflectedType, out List<FieldInfo> memberList))
                {
                    memberList.Add(field);
                }
            }

            stopwatch.Stop();

            Debug.LogWarning($"{stopwatch.Elapsed}");

            foreach (var pooledType in typeToFieldInfoLookup)
            {
                Debug.LogWarning($"{pooledType.Key.FullName} has pooled fields.");
                foreach (var fieldInfo in pooledType.Value)
                {
                    Debug.LogWarning($"... {fieldInfo.Name}.");
                    if (fieldInfo.FieldType == typeof(GameObject))
                    {
                        Debug.LogWarning("........ was a game object. which we could prewarm.");
                        // var go  = fieldInfo.GetValue()
                    }
                }
            }
        }
        #endif

        private static List<Type> pooledTypes = new List<Type>();

        private static List<MemberInfo> pooledMembers = new List<MemberInfo>();

        private static Dictionary<Type, List<FieldInfo>>
            typeToFieldInfoLookup = new Dictionary<Type, List<FieldInfo>>();

        private static Dictionary<GameObject, Pool> prefabToPoolLookup = new Dictionary<GameObject, Pool>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void PrewarmAfterSceneLoad()
        {
            /*
         *  1. have a list of types that have pooled field. 
         *
         *  2. try and find the first instance of each of these types.
         *
         *  3. loop through their pooled fields and try to create a pool for that prefab.
         */

            foreach (var kvp in typeToFieldInfoLookup)
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

                    Debug.Log(
                        $"Found first object of type {kvp.Key.FullName} in scene: " +
                        $"{(firstFoundComponent)}"
                    );

                    if (typeToFieldInfoLookup.TryGetValue(kvp.Key, out List<FieldInfo> memberList))
                    {
                        foreach (FieldInfo fieldInfo in memberList)
                        {
                            var prefabAsset = fieldInfo.GetValue(firstFoundComponent) as GameObject;
                            if (prefabAsset == null)
                            {
                                Debug.LogWarning("pooled prefab wasn't assigned");
                                continue;
                            }

                            Debug.LogWarning($"ref to actual prefab : {prefabAsset.name}");

                            CreatePool(prefabAsset);
                        }
                    }
                }
            }
        }

        private static Pool GetPool(GameObject prefab)
        {
            if (prefabToPoolLookup.TryGetValue(prefab, out Pool foundPool))
                return foundPool;

            // var foundPoolHandle = prefab.GetComponent<PoolHandle>();
            // int initialSize = 20;
            // bool growOnExhaustion = true;
            // if (foundPoolHandle != null)
            // {
            //     initialSize = foundPoolHandle.initialPoolSize;
            //     growOnExhaustion = foundPoolHandle.growOnExhaustion;
            //     //... if we find a pool-handle, we use its settings for the pool.
            // }

            var newPool = CreatePool(prefab);
            prefabToPoolLookup.Add(prefab, newPool);

            return newPool;
        }

        //... we prewarm here:
        public static Pool CreatePool(GameObject prefab)
        {
            var foundPoolHandle = prefab.GetComponent<PoolHandle>();
            int initialSize = 20;
            bool growOnExhaustion = true;
            if (foundPoolHandle != null)
            {
                //... if we find a pool-handle, we use its settings for the pool.
                initialSize = foundPoolHandle.initialPoolSize;
                growOnExhaustion = foundPoolHandle.growOnExhaustion;
            }

            var pool = new GameObject($"[POOL - {prefab.name}]").AddComponent<Pool>();

            pool.OnExhaustion += growOnExhaustion ? pool.GrowPoolOnExhaustion : pool.ClearPool;
            pool.initialSize = initialSize;
            pool.prefab = prefab;
            pool.GrowPoolOnCreation();

            return pool;

            // CreatePool(prefab, initialSize, growOnExhaustion);
        }

        private static Pool CreatePool(
            GameObject prefab,
            int initialPoolsize = 20,
            bool growOnExhaustion = true
        )
        {
            var pool = new GameObject($"[POOL - {prefab.name}]").AddComponent<Pool>();

            pool.OnExhaustion += growOnExhaustion ? pool.GrowPoolOnExhaustion : pool.ClearPool;
            pool.initialSize = initialPoolsize;
            pool.prefab = prefab;
            pool.GrowPoolOnCreation();

            return pool;
        }

        public static T GetPooledInstance<T>(this GameObject prefab) where T : PoolHandle
        {
            var pool = GetPool(prefab);
            var pooledPrefabInstance = pool.Get<T>();

            return pooledPrefabInstance;
        }

        public static T GetAndPlay<T>(
            this GameObject prefab,
            Vector3 pos,
            Quaternion rot = default,
            Transform parent = null
        ) where T : PoolHandle
        {
            var pooledInstance = GetPooledInstance<T>(prefab);
            pooledInstance.transform.SetPositionAndRotation(pos, rot);
            if (parent != null)
                pooledInstance.transform.SetParent(parent, true);
            return pooledInstance;
        }

        public static PoolHandle GetAndPlay(
            this GameObject prefab,
            Vector3 pos,
            Quaternion rot = default,
            Transform parent = null
        )
        {
            var pooledInstance = GetPooledInstance<PoolHandle>(prefab);
            pooledInstance.transform.SetPositionAndRotation(pos, rot);
            return pooledInstance;
        }

    }

}