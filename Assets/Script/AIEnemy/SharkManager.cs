using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharkManager : MonoBehaviour
{
        [Header("Spawn Settings")]
    public GameObject sharkPrefab;
    public int maxSharks = 20;
    public float spawnRadius = 50f;
    
    [Header("Performance Settings")]
    public bool useObjectPooling = true;
    public int poolSize = 25;
    public float cullDistance = 100f;
    
    private List<GameObject> activeSharks = new List<GameObject>();
    private Queue<GameObject> sharkPool = new Queue<GameObject>();
    private Transform player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (useObjectPooling)
        {
            InitializeObjectPool();
        }
        
        StartCoroutine(SpawnManagementRoutine());
    }
    
    void InitializeObjectPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject shark = Instantiate(sharkPrefab, Vector3.zero, Quaternion.identity);
            shark.SetActive(false);
            sharkPool.Enqueue(shark);
        }
    }
    
    System.Collections.IEnumerator SpawnManagementRoutine()
    {
        while (true)
        {
            ManageSharkPopulation();
            CullDistantSharks();
            yield return new WaitForSeconds(5f); // Check every 5 seconds
        }
    }
    
    void ManageSharkPopulation()
    {
        if (activeSharks.Count >= maxSharks || player == null) return;
        
        // Spawn new shark
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject shark = GetSharkFromPool();
        
        // if (shark != null)
        // {
        //     shark.transform.position = spawnPos;
        //     shark.SetActive(true);
            
        //     // Setup AI
        //     SharkAI ai = shark.GetComponent<SharkAI>();
        //     if (ai != null)
        //     {
        //         ai.playerTarget = player;
        //         ai.updateInterval = 0.05f + (Random.Range(0, activeSharks.Count) * 0.002f); // Stagger updates
        //     }
            
        //     activeSharks.Add(shark);
        // }
    }
    
    GameObject GetSharkFromPool()
    {
        if (useObjectPooling && sharkPool.Count > 0)
        {
            return sharkPool.Dequeue();
        }
        else if (!useObjectPooling && activeSharks.Count < maxSharks)
        {
            return Instantiate(sharkPrefab);
        }
        return null;
    }
    
    void ReturnSharkToPool(GameObject shark)
    {
        shark.SetActive(false);
        if (useObjectPooling)
        {
            sharkPool.Enqueue(shark);
        }
        else
        {
            Destroy(shark);
        }
    }
    
    void CullDistantSharks()
    {
        if (player == null) return;
        
        for (int i = activeSharks.Count - 1; i >= 0; i--)
        {
            GameObject shark = activeSharks[i];
            if (shark == null) continue;
            
            float distance = Vector3.Distance(shark.transform.position, player.position);
            if (distance > cullDistance)
            {
                activeSharks.RemoveAt(i);
                ReturnSharkToPool(shark);
            }
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (player != null)
        {
            Gizmos.DrawWireSphere(player.position, spawnRadius);
            Gizmos.DrawWireSphere(player.position, cullDistance);
        }
    }
}
