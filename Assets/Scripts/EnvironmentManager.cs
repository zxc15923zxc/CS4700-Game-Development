using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Environment Setup")]
    public bool autoSpawnEnvironment = true;
    public int numberOfTrees = 10;
    public int numberOfChoppingTrees = 10;
    public int numberOfFuelItems = 15;
    public int numberOfRuins = 15;
    public int numberOfMushrooms = 40;
    public int numberOfStones = 30;

    [Header("Spawn Areas")]
    public Transform spawnArea;
    public float spawnRadius = 50f;
    
    [Header("Prefabs")]
    public GameObject treePrefab;
    public GameObject choppingtreePrefab;
    public GameObject woodPrefab;
    public GameObject stickPrefab;
    public GameObject ruinsPrefab;
    public GameObject mushroomPrefab;
    public GameObject stonePrefab;

    [Header("Enemies")]
    public GameObject wolfPrefab;
    public int numberOfWolves = 5;

    [Header("Spawn Settings")]
    public float minSpawnDistance = 5f;
    public LayerMask groundLayer = 1;
    
    void Start()
    {
        if (autoSpawnEnvironment)
        {
            SpawnEnvironment();
        }
    }
    
    public void SpawnEnvironment()
    {
        Vector3 center = spawnArea != null ? spawnArea.position : Vector3.zero;
        
        // Spawn trees
        SpawnObjects(treePrefab, numberOfTrees, center, spawnRadius, "Tree");

        // Spawn chopping trees
        SpawnObjects(choppingtreePrefab, numberOfChoppingTrees, center, spawnRadius, "Chopping Tree");

        // Spawn fuel items
        SpawnFuelItems(center, spawnRadius);

        // Spawns ruins decor
        SpawnObjects(ruinsPrefab, numberOfRuins, center, spawnRadius, "Ruins");

        // Spawns mushroom decor
        SpawnObjects(mushroomPrefab, numberOfMushrooms, center, spawnRadius, "Mushrooms");

        // Spawns stone decor
        SpawnObjects(stonePrefab, numberOfStones, center, spawnRadius, "Stones");

        // Wolf
        SpawnEnemies(center, spawnRadius);

        Debug.Log("Environment spawned successfully!");
    }

    void SpawnObjects(GameObject prefab, int count, Vector3 center, float radius, string objectName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"No {objectName} prefab assigned!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition(center, radius);

            if (spawnPosition != Vector3.zero)
            {
                GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
                obj.name = $"{objectName}_{i}";

                // Random rotation for variety
                obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }
    }

    void SpawnEnemies(Vector3 center, float radius)
    {
	if (wolfPrefab == null)
	{
	    Debug.LogWarning("No wolf prefab assigned!");
	    return;
	}

	for (int i = 0; i < numberOfWolves; i++)
	{
	    Vector3 spawnPosition = GetRandomSpawnPosition(center, radius);

	    if (spawnPosition != Vector3.zero)
	    {
		GameObject wolf = Instantiate(wolfPrefab, spawnPosition, Quaternion.identity);
		wolf.name = $"Wolf_{i}";


		wolf.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
	    }
	}
    }


    void SpawnFuelItems(Vector3 center, float radius)
    {
        for (int i = 0; i < numberOfFuelItems; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition(center, radius);
            
            if (spawnPosition != Vector3.zero)
            {
                GameObject fuelPrefab = GetRandomFuelPrefab();
                if (fuelPrefab != null)
                {
                    GameObject fuel = Instantiate(fuelPrefab, spawnPosition, Quaternion.identity);
                    fuel.name = $"Fuel_{i}";
                    
                    // Random rotation
                    fuel.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                }
            }
        }
    }
    
    GameObject GetRandomFuelPrefab()
    {
        GameObject[] fuelPrefabs = { woodPrefab, stickPrefab };
        List<GameObject> availablePrefabs = new List<GameObject>();
        
        foreach (GameObject prefab in fuelPrefabs)
        {
            if (prefab != null)
            {
                availablePrefabs.Add(prefab);
            }
        }
        
        if (availablePrefabs.Count > 0)
        {
            return availablePrefabs[Random.Range(0, availablePrefabs.Count)];
        }
        
        return null;
    }
    
    Vector3 GetRandomSpawnPosition(Vector3 center, float radius)
    {
        int attempts = 0;
        int maxAttempts = 50;
        
        while (attempts < maxAttempts)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection.y = 0; // Keep on ground level
            Vector3 spawnPosition = center + randomDirection;
            
            // Check if position is valid (not too close to other objects)
            if (IsValidSpawnPosition(spawnPosition))
            {
                // Raycast to find ground
                RaycastHit hit;
                if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
                {
                    return hit.point;
                }
            }
            
            attempts++;
        }
        
        return Vector3.zero;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, minSpawnDistance);
        
        // Check if any colliders are too close
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Tree") || col.CompareTag("Grass") || col.CompareTag("Fuel"))
            {
                return false;
            }
        }
        
        return true;
    }
    
    [ContextMenu("Clear Environment")]
    public void ClearEnvironment()
    {
        // Find and destroy all spawned objects
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        GameObject[] grass = GameObject.FindGameObjectsWithTag("Grass");
        GameObject[] fuel = GameObject.FindGameObjectsWithTag("Fuel");
        GameObject[] fires = GameObject.FindGameObjectsWithTag("Fire");
        
        foreach (GameObject obj in trees)
        {
            if (obj.name.Contains("Tree_"))
            {
                DestroyImmediate(obj);
            }
        }
        
        foreach (GameObject obj in grass)
        {
            if (obj.name.Contains("Grass_"))
            {
                DestroyImmediate(obj);
            }
        }
        
        foreach (GameObject obj in fuel)
        {
            if (obj.name.Contains("Fuel_"))
            {
                DestroyImmediate(obj);
            }
        }
        
        foreach (GameObject obj in fires)
        {
            if (obj.name.Contains("Campfire"))
            {
                DestroyImmediate(obj);
            }
        }
        
        Debug.Log("Environment cleared!");
    }
    
    [ContextMenu("Respawn Environment")]
    public void RespawnEnvironment()
    {
        ClearEnvironment();
        SpawnEnvironment();
    }
    
    void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnArea.position, spawnRadius);
        }
    }
}
