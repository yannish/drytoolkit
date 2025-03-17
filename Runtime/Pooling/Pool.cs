using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Pool : MonoBehaviour
{
    public GameObject prefab;

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

    public T Get<T>() where T : PoolHandle
    {
        if (handleQueue.Count == 0)
        {
            Debug.LogWarning("Pool exhausted.");
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
    }
    
    private void GrowPool(int growSize)
    {
        for (int i = 0; i < growSize; i++)
        {
            var pooledPrefabInstance = Instantiate(prefab);
            pooledPrefabInstance.gameObject.name += " " + (i + currPoolSize).ToString();
            
            var poolHandle = pooledPrefabInstance.GetOrAddComponent<PoolHandle>();
            poolHandle.CachePoolables();
            //... cache any lil IPoolables that might respond to pooling callbacks..?
            //... those would get stored on poolHandle.
            poolHandle.gameObject.SetActive(false);
            inactiveObjects.Add(poolHandle);
            poolHandle.transform.SetParent(transform);
            poolHandle.OnAwakened += AddObjectToActive;
            poolHandle.OnDisabled += AddObjectToInactive;
            poolHandle.pool = this;
        }

        currPoolSize += growSize;
    }

    public void GrowPoolOnCreation() => GrowPool(initialSize);
    
    public void GrowPoolOnExhaustion() => GrowPool(exhaustionGrowSize);

    public void ClearPool()
    {
        var listCopy = activeObjects.ToArray();
        foreach(var handle in listCopy)
            handle.gameObject.SetActive(false);
    }
}