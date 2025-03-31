using UnityEngine;
using Random = UnityEngine.Random;
using drytoolkit.Runtime.Pooling;

public class PoolTester : MonoBehaviour
{
    public bool spawnContinuosly = false;
    public float spacing = 1f;
    private int tally;
    
    [Pooled, Expandable] public GameObject prefab;
    public GameObject normalPrefab;
    
    public void Spawn()
    {
        var spawnPos = transform.position + Vector3.forward * tally * spacing;
        
        prefab.GetAndPlay(spawnPos, Random.rotation);
        
        // Debug.LogWarning($"spawn at: {spawnPos}, rb pos: {prefab.GetComponent<Rigidbody>().position}");
        
        tally++;
    }

    private void Update()
    {
        if(spawnContinuosly)
            Spawn();
    }
}
