using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace drytoolkit.Runtime.Pooling
{
    public class Pool : MonoBehaviour
    {
        public GameObject prefab;

        public bool growOnExhaustion = true;

        public int initialSize = 20;

        public int exhaustionGrowSize = 10;

        public int currPoolSize = 0;

        /* TODO:
     * don't really want to pull a "PoolHandle" from out the queue.
     * You'll want something specific you can use right away.
     * PooledParticle, PooledProjectile, PooledRigidbody, etc.
     *
     * Derive those things from poolHandle i guess...?
     */

        private Queue<PoolHandle> handleQueue = new Queue<PoolHandle>();

        private List<PoolHandle> activeObjects = new List<PoolHandle>();

        private List<PoolHandle> inactiveObjects = new List<PoolHandle>();

        public event Action OnExhaustion;

        private readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        
        public T GetPooledInstance<T>() where T : PoolHandle
        {
            if (handleQueue.Count == 0)
            {
                // Debug.LogWarning("Pool exhausted.");
                OnExhaustion.Invoke();
            }

            return handleQueue.Dequeue() as T;
        }

        private void AddObjectToActive(PoolHandle handle)
        {
            activeObjects.Add(handle);
            inactiveObjects.Remove(handle);
        }

        private void AddObjectToInactive(PoolHandle handle)
        {
            activeObjects.Remove(handle);
            inactiveObjects.Add(handle);
            handleQueue.Enqueue(handle);
            foreach (var poolable in handle.poolables)
            {
                poolable.OnReturnToPool();
            }
            // TODO:
            //... can't reparent here w/o complaint in the console from Unity (we've disabled this frame)
            // handle.transform.SetParent(handle.pool.transform); 
        }

        public void GrowPool(int growSize)
        {
            for (int i = 0; i < growSize; i++)
            {
                var pooledPrefabInstance = Instantiate(prefab);
                pooledPrefabInstance.gameObject.name += " " + (i + currPoolSize).ToString();

                var poolHandle = pooledPrefabInstance.GetOrAddComponent<PoolHandle>();
                poolHandle.pool = this;
                poolHandle.CachePoolables();
                poolHandle.transform.SetParent(transform);
                poolHandle.gameObject.SetActive(false); //.. get the callback here to fire, adding to queue
                activeObjects.Add(poolHandle);
                AddObjectToInactive(poolHandle);
                poolHandle.OnAwakened += AddObjectToActive;
                poolHandle.OnDisabled += AddObjectToInactive;
            }

            currPoolSize += growSize;
        }

        public void ClearPool()
        {
            var listCopy = activeObjects.ToArray();
            foreach (var handle in listCopy)
                handle.gameObject.SetActive(false);
        }

        public void Reparent(PoolHandle handle)
        {
            if (!Application.isPlaying || PlaymodeStateTracker.IsExitingPlayMode)
                return;
            
            if (handle == null || !isActiveAndEnabled || !enabled)
                return;
            
            StartCoroutine(DelayedReparent(handle));
        }

        public IEnumerator DelayedReparent(PoolHandle handle)
        {
            yield return waitForEndOfFrame;
            yield return waitForEndOfFrame;
            handle.transform.SetParent(transform);
        }
    }
}