using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FoodSystem : MonoBehaviour
{
    [Header("Food Properties")]
    [SerializeField] public int growthStage = 0;
    [SerializeField] public int experienceValue = 1;
    [SerializeField] public int healAmount = 0;
    public bool isConsumable = true;
    
    [Header("Experience & Healing Settings")]
    [Tooltip("Jika dicentang, nilai experience dan heal akan dihitung otomatis berdasarkan growth stage")]
    public bool autoCalculateExpHeal = false;
    [Tooltip("Multiplier untuk experience (hanya berlaku jika autoCalculateExpHeal aktif)")]
    public int expMultiplier = 1;
    [Tooltip("Multiplier untuk healing (hanya berlaku jika autoCalculateExpHeal aktif)")]
    public int healMultiplier = 2;
    
    [Header("Scale Growth System")]
    [Tooltip("Scale berdasarkan growth stage (bisa diatur di inspector)")]
    public Vector3[] growthStageScales = new Vector3[]
    {
        new Vector3(0.3f, 0.3f, 0.3f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0.8f, 0.8f, 0.8f),
        new Vector3(1.2f, 1.2f, 1.2f),
        new Vector3(1.6f, 1.6f, 1.6f),
        new Vector3(2.0f, 2.0f, 2.0f)
    };
    
    [Tooltip("Apakah menggunakan scale growth stage")]
    public bool useGrowthStageScale = true;
    
    [Header("Trigger System")]
    public int requiredHits = 3;
    [Tooltip("Cooldown antar hit (detik)")]
    public float hitCooldown = 0.1f;
    
    [Header("Respawn System")]
    public bool autoRespawn = true;
    public float respawnTime = 5f;
    
    [Header("Visual Feedback")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    public Color[] hitStageColors = new Color[]
    {
        new Color(1f, 0.5f, 0.5f, 1f),
        new Color(1f, 0.8f, 0.5f, 1f),
        new Color(1f, 1f, 0.5f, 1f),
        new Color(0.6f, 1f, 0.6f, 1f)
    };
    
    [Tooltip("Scale multiplier per hit")]
    public float scaleMultiplier = 1.1f;
    
    [Header("Performance")]
    public bool useTriggerCollider = true;
    
    // Private variables
    private int currentHitCount = 0;
    private bool isOnCooldown = false;
    private bool isReadyToEat = false;
    private Vector3 originalScale;
    private Color originalColor;
    private Renderer foodRenderer;
    private Collider foodCollider;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isRegistered = false;
    
    void Awake()
    {
        InitializeFood();
    }
    
    void Start()
    {
        // Simpan posisi asli untuk respawn
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Auto-register dengan SimpleFishRespawnManager
        if (FishRespawnManager.Instance != null && autoRespawn && !isRegistered)
        {
            FishRespawnManager.Instance.RegisterFish(gameObject);
            isRegistered = true;
        }
    }
    
    private void InitializeFood()
    {
        foodRenderer = GetComponent<Renderer>();
        foodCollider = GetComponent<Collider>();
        
        if (foodRenderer != null)
        {
            originalColor = foodRenderer.material.color;
        }
        
        // Setup collider
        if (foodCollider != null && useTriggerCollider)
        {
            foodCollider.isTrigger = true;
        }
        
        // Hanya kalkulasi otomatis jika autoCalculateExpHeal aktif
        if (autoCalculateExpHeal)
        {
            experienceValue = Mathf.Max(1, growthStage * expMultiplier);
            healAmount = growthStage * healMultiplier;
        }
    }
    
    void OnEnable()
    {
        ResetFood();
    }
    
    /// <summary>
    /// Reset status makanan
    /// </summary>
    public void ResetFood()
    {
        currentHitCount = 0;
        isReadyToEat = false;
        isOnCooldown = false;
        
        // Set original scale berdasarkan growth stage atau default
        SetScaleByGrowthStage();
        
        if (foodRenderer != null)
        {
            foodRenderer.material.color = originalColor;
            foodRenderer.enabled = true;
        }
        
        if (foodCollider != null)
        {
            foodCollider.enabled = true;
        }
        
        isConsumable = true;
    }
    
    /// <summary>
    /// Set scale berdasarkan growth stage
    /// </summary>
    private void SetScaleByGrowthStage()
    {
        if (useGrowthStageScale && growthStageScales.Length > 0)
        {
            int stageIndex = Mathf.Clamp(growthStage, 0, growthStageScales.Length - 1);
            originalScale = growthStageScales[stageIndex];
        }
        else
        {
            originalScale = transform.localScale;
        }
        
        transform.localScale = originalScale;
    }
    
    // ==================== TRIGGER/COLLISION ====================
    
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerCollider || !isConsumable) return;
        ProcessHit(other.gameObject);

        if (hitEffect != null)
        {
            Instantiate(hitEffect,transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (useTriggerCollider || !isConsumable) return;
        ProcessHit(collision.gameObject);

        if (hitEffect != null)
        {
            Instantiate(hitEffect,transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }
    
    private void ProcessHit(GameObject hitter)
    {
        if (isReadyToEat || isOnCooldown) return;
        if (!IsValidConsumer(hitter)) return;
        
        // Cek jika bisa dimakan berdasarkan stage
        LevelSystem levelSystem = hitter.GetComponent<LevelSystem>();
        if (levelSystem != null && !levelSystem.CanEatFood(growthStage)) return;
        
        currentHitCount++;
        UpdateVisuals();
        StartCoroutine(HitCooldown());
        
        if (currentHitCount >= requiredHits)
        {
            ReadyToEat();
            TryAutoEat(hitter);
        }
    }
    
    private bool IsValidConsumer(GameObject consumer)
    {
        if (consumer.GetComponent<LevelSystem>() != null) return true;
        if (consumer.CompareTag("Fish") || consumer.CompareTag("Player")) return true;
        return false;
    }
    
    private IEnumerator HitCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(hitCooldown);
        isOnCooldown = false;
    }
    
    private void TryAutoEat(GameObject consumer)
    {
        if (consumer != null)
        {
            StartCoroutine(DelayedEat(consumer, 0.1f));
        }
    }
    
    private IEnumerator DelayedEat(GameObject consumer, float delay)
    {
        yield return new WaitForSeconds(delay);
        Consume(consumer);
    }
    
    // ==================== CONSUME SYSTEM ====================
    
    public void Consume(GameObject consumer)
    {
        if (!isReadyToEat || !isConsumable) return;
        
        LevelSystem levelSystem = consumer.GetComponent<LevelSystem>();
        if (levelSystem != null)
        {
            if (!levelSystem.CanEatFood(growthStage)) return;
            
            levelSystem.AddExpDirectly(experienceValue);
            
            if (healAmount > 0)
            {
                levelSystem.Heal(healAmount);
            }
        }
        
        DeactivateFood();
    }
    
    private void DeactivateFood()
    {
        isConsumable = false;
        
        // Nonaktifkan renderer dan collider
        if (foodCollider != null) foodCollider.enabled = false;
        if (foodRenderer != null) foodRenderer.enabled = false;
        
        if (autoRespawn)
        {
            // Gunakan SimpleFishRespawnManager untuk respawn
            if (FishRespawnManager.Instance != null)
            {
                FishRespawnManager.Instance.OnFishEaten(gameObject);
            }
            else
            {
                // Fallback ke sistem lokal jika manager tidak ada
                Debug.LogWarning("SimpleFishRespawnManager not found, using direct destroy");
                Destroy(gameObject, 0.1f);
            }
        }
        else
        {
            // Jika tidak respawn, hancurkan objek
            Destroy(gameObject, 0.1f);
        }
    }
    
    /// <summary>
    /// Dipanggil oleh SimpleFishRespawnManager saat waktunya respawn
    /// </summary>
    public void OnRespawn()
    {
        // Reset ke posisi asli
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // Reset semua status
        ResetFood();
        
        // Aktifkan kembali GameObject
        gameObject.SetActive(true);
    }
    
    // ==================== VISUAL FEEDBACK ====================
    
    private void UpdateVisuals()
    {
        // Update color
        if (foodRenderer != null && hitStageColors.Length > 0)
        {
            int colorIndex = Mathf.Min(hitStageColors.Length - 1, currentHitCount - 1);
            if (colorIndex >= 0)
            {
                foodRenderer.material.color = hitStageColors[colorIndex];
            }
        }
        
        // Update scale dengan multiplier
        float scaleProgress = 1f + (Mathf.Min(currentHitCount, requiredHits) * (scaleMultiplier - 1f) / requiredHits);
        transform.localScale = originalScale * scaleProgress;
    }
    
    private void ReadyToEat()
    {
        isReadyToEat = true;
        
        if (foodRenderer != null)
        {
            foodRenderer.material.color = Color.green;
        }
        
        StartCoroutine(PulsateEffect());
    }
    
    private IEnumerator PulsateEffect()
    {
        float pulseSpeed = 3f;
        float timer = 0f;
        
        while (isReadyToEat && foodRenderer != null)
        {
            timer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(timer) * 0.1f + 1f;
            
            Color pulseColor = Color.Lerp(Color.green, Color.yellow, Mathf.Abs(Mathf.Sin(timer)));
            foodRenderer.material.color = pulseColor;
            
            transform.localScale = originalScale * pulse * 1.1f;
            
            yield return null;
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Set growth stage dan update scale
    /// </summary>
    public void SetGrowthStage(int stage)
    {
        growthStage = Mathf.Max(0, stage);
        
        // Update nilai experience dan heal jika autoCalculateExpHeal aktif
        if (autoCalculateExpHeal)
        {
            experienceValue = Mathf.Max(1, growthStage * expMultiplier);
            healAmount = growthStage * healMultiplier;
        }
        
        if (gameObject.activeInHierarchy)
        {
            SetScaleByGrowthStage();
            UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Set custom scale untuk growth stage tertentu
    /// </summary>
    public void SetGrowthStageScale(int stage, Vector3 scale)
    {
        if (stage >= 0 && stage < growthStageScales.Length)
        {
            growthStageScales[stage] = scale;
            
            // Update scale jika stage ini sedang aktif
            if (growthStage == stage && gameObject.activeInHierarchy)
            {
                SetScaleByGrowthStage();
            }
        }
    }
    
    /// <summary>
    /// Set semua scale sekaligus
    /// </summary>
    public void SetAllGrowthStageScales(Vector3[] scales)
    {
        if (scales != null && scales.Length > 0)
        {
            growthStageScales = scales;
            
            // Update scale jika objek aktif
            if (gameObject.activeInHierarchy)
            {
                SetScaleByGrowthStage();
            }
        }
    }
    
    /// <summary>
    /// Toggle penggunaan growth stage scale
    /// </summary>
    public void SetUseGrowthStageScale(bool use)
    {
        useGrowthStageScale = use;
        if (gameObject.activeInHierarchy)
        {
            SetScaleByGrowthStage();
        }
    }
    
    /// <summary>
    /// Get current scale berdasarkan growth stage
    /// </summary>
    public Vector3 GetCurrentStageScale()
    {
        if (useGrowthStageScale && growthStageScales.Length > 0)
        {
            int stageIndex = Mathf.Clamp(growthStage, 0, growthStageScales.Length - 1);
            return growthStageScales[stageIndex];
        }
        return originalScale;
    }
    
    public int GetGrowthStage()
    {
        return growthStage;
    }
    
    public void SetRequiredHits(int hits)
    {
        requiredHits = Mathf.Max(1, hits);
    }
    
    /// <summary>
    /// Set apakah menggunakan kalkulasi otomatis untuk experience dan healing
    /// </summary>
    public void SetAutoCalculateExpHeal(bool autoCalculate)
    {
        autoCalculateExpHeal = autoCalculate;
        if (autoCalculateExpHeal && gameObject.activeInHierarchy)
        {
            experienceValue = Mathf.Max(1, growthStage * expMultiplier);
            healAmount = growthStage * healMultiplier;
        }
    }
    
    /// <summary>
    /// Set multiplier untuk experience (hanya berlaku jika autoCalculateExpHeal aktif)
    /// </summary>
    public void SetExpMultiplier(int multiplier)
    {
        expMultiplier = multiplier;
        if (autoCalculateExpHeal && gameObject.activeInHierarchy)
        {
            experienceValue = Mathf.Max(1, growthStage * expMultiplier);
        }
    }
    
    /// <summary>
    /// Set multiplier untuk healing (hanya berlaku jika autoCalculateExpHeal aktif)
    /// </summary>
    public void SetHealMultiplier(int multiplier)
    {
        healMultiplier = multiplier;
        if (autoCalculateExpHeal && gameObject.activeInHierarchy)
        {
            healAmount = growthStage * healMultiplier;
        }
    }
    
    public void ForceConsume(GameObject consumer)
    {
        isReadyToEat = true;
        Consume(consumer);
    }
    
    public bool IsReadyToEat()
    {
        return isReadyToEat;
    }
    
    public float GetProgress()
    {
        return (float)currentHitCount / requiredHits;
    }
    
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
    
    public void ForceRespawn()
    {
        OnRespawn();
    }
}
