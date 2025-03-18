using System;
using Drydock.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PoolTester : MonoBehaviour
{
    public float spacing = 1f;
    private int tally;
    
    [Pooled]
    public GameObject prefab;
    public GameObject normalPrefab;
    
    [Button]
    public void Spawn()
    {
        var spawnPos = transform.position + Vector3.forward * tally * spacing;
        
        prefab.GetAndPlay(spawnPos, Random.rotation);
        
        // Debug.LogWarning($"spawn at: {spawnPos}, rb pos: {prefab.GetComponent<Rigidbody>().position}");
        
        tally++;
    }

    private void Update()
    {
        Spawn();
    }
}
