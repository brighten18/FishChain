using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishRespawnManager : MonoBehaviour
{
    public static FishRespawnManager Instance;
    
    [Header("Settings")]
    public bool enableRespawn = true;
    public float defaultRespawnTime = 5f;
    
    [Header("Debug")]
    public bool showLogs = true;
    
    // Simple data class
    private class FishData
    {
        public GameObject fishObject;
        public FoodSystem foodSystem;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public float respawnTime;
        public float respawnTimer;
        public bool isRespawning;
        
        // Original properties
        public int originalGrowthStage;
        public int originalExpValue;
        public int originalHealAmount;
    }
    
    private List<FishData> allFish = new List<FishData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showLogs)
                Debug.Log("SimpleFishRespawnManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (!enableRespawn) return;
        
        // Update respawn timers
        for (int i = 0; i < allFish.Count; i++)
        {
            FishData fish = allFish[i];
            
            if (fish.isRespawning && fish.fishObject != null)
            {
                fish.respawnTimer -= Time.deltaTime;
                
                if (fish.respawnTimer <= 0)
                {
                    RespawnFish(fish);
                }
            }
        }
    }
    
    /// <summary>
    /// Register fish to respawn system
    /// </summary>
    public void RegisterFish(GameObject fish)
    {
        if (fish == null) return;
        
        // Check if already registered
        foreach (FishData data in allFish)
        {
            if (data.fishObject == fish)
                return;
        }
        
        // Create new fish data
        FishData newFish = new FishData
        {
            fishObject = fish,
            spawnPosition = fish.transform.position,
            spawnRotation = fish.transform.rotation,
            isRespawning = false
        };
        
        // Get FoodSystem component
        FoodSystem fs = fish.GetComponent<FoodSystem>();
        if (fs != null)
        {
            newFish.foodSystem = fs;
            newFish.respawnTime = fs.respawnTime;
            newFish.originalGrowthStage = fs.growthStage;
            newFish.originalExpValue = fs.experienceValue;
            newFish.originalHealAmount = fs.healAmount;
        }
        else
        {
            newFish.respawnTime = defaultRespawnTime;
        }
        
        allFish.Add(newFish);
        
        if (showLogs)
            Debug.Log($"Fish registered: {fish.name}");
    }
    
    /// <summary>
    /// Called when fish is eaten
    /// </summary>
    public void OnFishEaten(GameObject fish)
    {
        if (!enableRespawn || fish == null) return;
        
        // Find the fish data
        FishData fishData = null;
        foreach (FishData data in allFish)
        {
            if (data.fishObject == fish)
            {
                fishData = data;
                break;
            }
        }
        
        // If not found, register it
        if (fishData == null)
        {
            RegisterFish(fish);
            foreach (FishData data in allFish)
            {
                if (data.fishObject == fish)
                {
                    fishData = data;
                    break;
                }
            }
        }
        
        // Start respawn process
        if (fishData != null && !fishData.isRespawning)
        {
            fishData.isRespawning = true;
            fishData.respawnTimer = fishData.respawnTime;
            
            // Disable the fish
            fish.SetActive(false);
            
            if (showLogs)
                Debug.Log($"Fish scheduled for respawn: {fish.name} in {fishData.respawnTime}s");
        }
    }
    
    /// <summary>
    /// Respawn the fish
    /// </summary>
    private void RespawnFish(FishData fishData)
    {
        if (fishData.fishObject == null) return;
        
        // Reset position and rotation
        fishData.fishObject.transform.position = fishData.spawnPosition;
        fishData.fishObject.transform.rotation = fishData.spawnRotation;
        
        // Reset FoodSystem properties if exists
        if (fishData.foodSystem != null)
        {
            fishData.foodSystem.growthStage = fishData.originalGrowthStage;
            fishData.foodSystem.experienceValue = fishData.originalExpValue;
            fishData.foodSystem.healAmount = fishData.originalHealAmount;
            fishData.foodSystem.SetGrowthStage(fishData.originalGrowthStage);
        }
        
        // Activate the fish
        fishData.fishObject.SetActive(true);
        fishData.isRespawning = false;
        
        // Call OnRespawn on FoodSystem
        FoodSystem fs = fishData.fishObject.GetComponent<FoodSystem>();
        if (fs != null)
        {
            fs.OnRespawn();
        }
        
        if (showLogs)
            Debug.Log($"Fish respawned: {fishData.fishObject.name}");
    }
    
    /// <summary>
    /// Force respawn all fish immediately
    /// </summary>
    public void ForceRespawnAll()
    {
        foreach (FishData fishData in allFish)
        {
            if (fishData.isRespawning)
            {
                RespawnFish(fishData);
            }
        }
        
        if (showLogs)
            Debug.Log("All fish force respawned");
    }
    
    /// <summary>
    /// Get fish statistics
    /// </summary>
    public string GetFishStats()
    {
        int total = allFish.Count;
        int respawning = 0;
        int active = 0;
        
        foreach (FishData fish in allFish)
        {
            if (fish.isRespawning)
                respawning++;
            else
                active++;
        }
        
        return $"Total Fish: {total}\nActive: {active}\nRespawning: {respawning}";
    }
    
    /// <summary>
    /// Remove fish from management
    /// </summary>
    public void RemoveFish(GameObject fish)
    {
        for (int i = allFish.Count - 1; i >= 0; i--)
        {
            if (allFish[i].fishObject == fish)
            {
                allFish.RemoveAt(i);
                break;
            }
        }
    }
}
