using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public GameObject[] spawnPrefabs;
    public int spawnCount = 1024;
    public float gridSize = 0.5f;

    private List<GameObject> _instances;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0 || spawnCount <= 0)
        {
            return;
        }
        
        var totalSpawnCount = spawnPrefabs.Length * spawnCount;
        var gridCount = Mathf.CeilToInt(Mathf.Sqrt(totalSpawnCount));
        var halfGridCount = Mathf.CeilToInt(gridCount / 2f);
        var spawnIndex = 0;

        if (_instances == null)
        {
            _instances = new List<GameObject>(totalSpawnCount);
        }
        else
        {
            _instances.Clear();
            _instances.Capacity = totalSpawnCount;
        }

        var spawnCounts = new int[spawnPrefabs.Length];
        
        for (int x = -halfGridCount; x < halfGridCount; x++)
        {
            for (int z = -halfGridCount; z < halfGridCount; z++)
            {
                if (spawnIndex++ >= totalSpawnCount)
                    break;
                
                Vector3 spawnPosition = new Vector3(x * gridSize, 0, z * gridSize);

                var spawnPrefabIndex = GetRandomSpawnPrefabIndex(spawnCounts);
                spawnCounts[spawnPrefabIndex]++;

                var instance = Instantiate(
                    spawnPrefabs[spawnPrefabIndex], 
                    spawnPosition, 
                    Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
                
                _instances.Add(instance);

                var vaInstance = instance.GetComponentInChildren<VertexAnimation.VertexAnimationInstance>();
                if (vaInstance != null)
                {
                    vaInstance.Play(Random.Range(0, vaInstance.m_VAData.availableVACount));
                }
            }
        }
    }

    void OnDisable()
    {
        if (_instances == null)
            return;

        foreach (var instance in _instances)
        {
            Destroy(instance);
        }

        _instances.Clear();
    }

    private int GetRandomSpawnPrefabIndex(int[] spawnCounts)
    {
        var availableCount = 0;
        for (int i = 0; i < spawnCounts.Length; i++)
        {
            if (spawnCounts[i] < spawnCount)
            {
                availableCount++;
            }
        }

        var randomIndex = Random.Range(0, availableCount);
        for (int i = 0; i < spawnCounts.Length; i++)
        {
            if (spawnCounts[i] < spawnCount)
            {
                if (--randomIndex < 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
