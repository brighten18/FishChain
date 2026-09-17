using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSystem : MonoBehaviour
{
    [Header("Growth System Reference")]
    public GrowSystem growSystem; // Reference ke GrowSystem

    [Header("Level System")]
    public int currentExperience = 0;
    public int[] experienceToNextStage = new int[] { 3, 5, 8, 12, 15 }; // EXP needed for each stage

    [Header("HP System")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int healthPerStage = 30; // HP bertambah per stage
    public float healthRegenRate = 1f; // HP per detik
    public float damageCooldown = 1f; // Cooldown antar damage
    public bool isInvincible = false;
    
    [Header("UI References")]
    public Text stageText;
    public Slider expSlider;
    public Slider healthSlider;
    public Image healthFillImage;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    [Header("Visual Feedback")]
    public GameObject damageEffect;
    public GameObject deathEffect;
    public AudioClip damageSound;
    public AudioClip deathSound;
    
    [Header("Death & Respawn Settings")]
    public bool canRespawn = true;
    public float respawnTime = 3f;
    public Vector3 respawnPosition;
    public int maxDeathCount = 3; // Maksimal mati sebelum game over
    public bool resetExperienceOnRespawn = false;
    public bool resetLevelOnRespawn = false;
    
    [Header("Win/Lose Conditions")]
    public bool checkWinCondition = true;
    public int winMaxLevel = 5; 
    public int winMaxExp = 20;

    private bool _hasWon = false;
    private bool _hasLost = false;

    // Events
    public System.Action<int> OnLevelUp;
    public System.Action<int> OnExperienceGained;
    public System.Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public System.Action OnDeath;
    public System.Action OnRespawn;
    
    // Private variables
    private float lastDamageTime;
    private float healthRegenTimer;
    public bool isDead = false;
    private int deathCount = 0;
    private Collider playerCollider;
    private Renderer playerRenderer;
    
    private void Start()
    {
        // Register ke GameManager sebagai persistent object
        RegisterToGameManager();
        
        // Cari GrowSystem jika belum diassign
        if (growSystem == null)
        {
            growSystem = GetComponent<GrowSystem>();
        }

        if (growSystem == null)
        {
            Debug.LogError("GrowSystem not found! Please assign it in the inspector.");
        }

        // Get components
        playerCollider = GetComponent<Collider>();
        playerRenderer = GetComponent<Renderer>();
        
        // Setup respawn position
        if (respawnPosition == Vector3.zero)
        {
            respawnPosition = transform.position;
        }
        
        // Initialize health based on stage
        UpdateMaxHealth();
        currentHealth = maxHealth;
        
        UpdateUI();
    }

void OnEnable()
{
    SceneManager.sceneLoaded += OnLevelSystemSceneLoaded;
}

void OnDisable()
{
    SceneManager.sceneLoaded -= OnLevelSystemSceneLoaded;
}

private void OnLevelSystemSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name == "MainLevel")
    {
        StartCoroutine(RebindUIAfterSceneLoad());
    }
}

private System.Collections.IEnumerator RebindUIAfterSceneLoad()
{
    yield return new WaitForSeconds(0.1f);
    
    RegisterToGameManager();
    FindAndBindUI();
    UpdateUI();
}

private void FindAndBindUI()
{
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null) return;
    
    Transform canvasTransform = canvas.transform;
    
    stageText = canvasTransform.Find("Lvl_Int")?.GetComponent<Text>();
    expSlider = canvasTransform.Find("SliderExp")?.GetComponent<Slider>();
    healthSlider = canvasTransform.Find("SliderHP")?.GetComponent<Slider>();
    
    if (healthSlider != null)
    {
        Transform fillArea = healthSlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null)
            {
                healthFillImage = fill.GetComponent<Image>();
            }
        }
    }
}



private System.Collections.IEnumerator WaitAndRebindUI()
{
    yield return new WaitForSeconds(0.2f);
    
    RegisterToGameManager();
    
    if (UIReferenceManager.Instance != null)
    {
        UIReferenceManager.Instance.RebindAllUIReferences();
    }
    
    UpdateUI();
}

public void RegisterToGameManager()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.RegisterPlayer(this);
        
        if (GameManager.Instance.GetCurrentCheckpoint() != Vector3.zero)
        {
            respawnPosition = GameManager.Instance.GetCurrentCheckpoint();
        }
        
        Debug.Log("LevelSystem registered to GameManager successfully");
    }
    else
    {
        Debug.LogWarning("GameManager not found in scene");
    }
}

    
    void Update()
    {
        // Health regeneration
        if (!isDead && currentHealth < maxHealth && Time.time > lastDamageTime + 3f)
        {
            healthRegenTimer += Time.deltaTime;
            
            if (healthRegenTimer >= 1f / healthRegenRate)
            {
                Heal(1);
                healthRegenTimer = 0f;
            }
        }
        
        // Debug input untuk testing
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(50);
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            AddExperience(5);
        }
    }

    // ==================== FOOD SYSTEM INTERACTION ====================

    /// <summary>
    /// Cek apakah bisa makan makanan berdasarkan stage
    /// </summary>
    public bool CanEatFood(int foodGrowthStage)
    {
        // Pastikan growSystem ada
        if (growSystem == null)
        {
            Debug.LogError("GrowSystem is null!");
            return false;
        }
        
        // Player bisa makan jika food stage <= player stage
        bool canEat = foodGrowthStage <= growSystem.currentGrowthStage;
        
        if (!canEat)
        {
            Debug.Log($"Cannot eat! Player Stage: {growSystem.currentGrowthStage}, Food Stage: {foodGrowthStage}");
        }
        
        return canEat;
    }

    /// <summary>
    /// Coba makan makanan (otomatis dipanggil dari trigger)
    /// </summary>
    public void TryEatFood(FoodSystem food)
    {
        if (food == null || !food.isConsumable) return;

        // Check jika makanan sudah siap dimakan
        if (!food.IsReadyToEat())
        {
            Debug.Log("Food is not ready to eat yet!");
            return;
        }

        // Check jika bisa memakan makanan tersebut berdasarkan stage
        if (CanEatFood(food.GetGrowthStage()))
        {
            EatFoodSuccess(food);
        }
        else
        {
            // Feedback bahwa tidak bisa makan
            Debug.LogWarning($"Cannot eat this food! Player is too small. Player Stage: {GetCurrentGrowthStage()}, Food Stage: {food.GetGrowthStage()}");
            ShowCannotEatFeedback();
        }
    }

    /// <summary>
    /// Proses ketika berhasil makan makanan
    /// </summary>
    private void EatFoodSuccess(FoodSystem food)
    {
        // Berikan experience berdasarkan food stage
        int expGained = food.experienceValue;
        AddExperience(expGained);
        
        // Heal jika makanan memberikan heal
        if (food.healAmount > 0)
        {
            Heal(food.healAmount);
        }
        
        Debug.Log($"Successfully ate food! Stage: {food.GetGrowthStage()}, Gained {expGained} EXP");
        
        // Consume food (akan memanggil DeactivateFood() di FoodSystem)
        food.Consume(gameObject);
    }

    // ==================== COLLISION & TRIGGER ====================

    private void OnTriggerEnter(Collider other)
    {
        // Auto-detect collision dengan makanan
        FoodSystem food = other.GetComponent<FoodSystem>();
        if (food != null)
        {
            // Untuk makanan yang memerlukan multiple hits, proses hit
            if (!food.IsReadyToEat())
            {
                // Makanan akan menangani hit secara internal
                return;
            }
            
            // Jika makanan sudah ready, coba makan
            TryEatFood(food);
        }
        
        // Damage dari enemy/obstacle
        EnemyDamage enemy = other.GetComponent<EnemyDamage>();
        if (enemy != null && !isDead)
        {
            TakeDamage(enemy.damageAmount);
        }
        
        // Checkpoint detection
        CheckPoint checkpoint = other.GetComponent<CheckPoint>();
        if (checkpoint != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(checkpoint.transform.position);
            respawnPosition = checkpoint.transform.position;
            checkpoint.Activate();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Alternative: jika menggunakan collision bukan trigger
        FoodSystem food = collision.gameObject.GetComponent<FoodSystem>();
        if (food != null)
        {
            // Untuk makanan yang memerlukan multiple hits, proses hit
            if (!food.IsReadyToEat())
            {
                return;
            }
            
            // Jika makanan sudah ready, coba makan
            TryEatFood(food);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        DamageZone damageZone = other.GetComponent<DamageZone>();
        if (damageZone != null && !isDead && Time.time > lastDamageTime + damageCooldown)
        {
            TakeDamage(damageZone.damagePerSecond);
        }
    }

    // ==================== HP SYSTEM METHODS ====================
    
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible || Time.time < lastDamageTime + damageCooldown) return;
        
        lastDamageTime = Time.time;
        healthRegenTimer = 0f;
        
        // Kurangi health
        currentHealth -= damage;
        
        // Clamp health
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Visual feedback
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }
        
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }
        
        // Camera shake effect
        StartCoroutine(DamageShake());
        
        // Notify GameManager tentang damage
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDamaged(damage, currentHealth, maxHealth);
        }
        
        Debug.Log($"Took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        // Cek kematian
        if (currentHealth <= 0)
        {
            Die();
        }
        
        UpdateUI();
    }
    
    public void Heal(int amount)
    {
        if (isDead) return;
        
        // Tambah health
        int oldHealth = currentHealth;
        currentHealth += amount;
        
        // Clamp health
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Trigger event jika ada perubahan
        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"Healed {amount} HP! Health: {currentHealth}/{maxHealth}");
        }
        
        UpdateUI();
    }
    
    public void Die()
    {
        if (isDead || _hasWon || _hasLost) return;
        
        isDead = true;
        deathCount++;
        
        Debug.Log($"Player died! Death count: {deathCount}/{maxDeathCount}");
        
        // Death visual effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.7f);
        }
        
        // Disable player
        DisablePlayer();
        
        // Trigger event
        OnDeath?.Invoke();
        
        // Notify GameManager tentang kematian
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied(deathCount);
            
            // Cek kondisi kekalahan melalui GameManager
            if (GameManager.Instance.CheckLoseCondition(deathCount, maxDeathCount))
            {
                _hasLost = true;
                return; // Stop di sini jika kalah
            }
        }
        
        // Jika belum kalah, lanjutkan dengan respawn
        if (deathCount >= maxDeathCount)
        {
            GameOver();
        }
        else if (canRespawn)
        {
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            GameOver();
        }
    }
    
    private void DisablePlayer()
    {
        // Disable movement
        TestMovement movement = GetComponent<TestMovement>();
        if (movement != null) 
        {
            movement.enabled = false;
            // Reset velocity jika ada Rigidbody
            Rigidbody rb = movement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        // Disable collider dan renderer
        if (playerCollider != null) playerCollider.enabled = false;
        if (playerRenderer != null) playerRenderer.enabled = false;
        
        // Disable semua script kecuali LevelSystem
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script.enabled)
            {
                script.enabled = false;
            }
        }
    }
    
    private void EnablePlayer()
    {
        // Enable movement
        TestMovement movement = GetComponent<TestMovement>();
        if (movement != null) movement.enabled = true;
        
        // Enable collider dan renderer
        if (playerCollider != null) playerCollider.enabled = true;
        if (playerRenderer != null) playerRenderer.enabled = true;
        
        // Enable semua script
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
            {
                script.enabled = true;
            }
        }
    }
    
    private IEnumerator RespawnAfterDelay()
    {
        Debug.Log($"Respawning in {respawnTime} seconds...");
        yield return new WaitForSeconds(respawnTime);
        
        Respawn();
    }
    
    public void Respawn()
    {
        // Reset position ke checkpoint terakhir atau default respawn position
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentCheckpoint() != Vector3.zero)
        {
            transform.position = GameManager.Instance.GetCurrentCheckpoint();
        }
        else
        {
            transform.position = respawnPosition;
        }
        
        // Reset stats jika diperlukan
        if (resetExperienceOnRespawn)
        {
            currentExperience = 0;
        }
        
        if (resetLevelOnRespawn && growSystem != null)
        {
            growSystem.ResetToInitialStage();
            UpdateMaxHealth();
        }
        
        // Reset health
        currentHealth = maxHealth;
        
        // Enable player
        EnablePlayer();
        
        // Reset state
        isDead = false;
        
        // Trigger event
        OnRespawn?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerRespawned();
        }
        
        Debug.Log($"Player respawned! Deaths: {deathCount}/{maxDeathCount}");
        
        UpdateUI();
    }
    
    private void GameOver()
    {
        Debug.Log("GAME OVER - Loading lose scene");
        
        // Panggil GameManager untuk handle game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerLost();
        }
        else
        {
            Debug.LogError("GameManager not found! Cannot load lose scene.");
            // Fallback: langsung respawn
            Respawn();
        }
    }
    
    public void UpdateMaxHealth()
    {
        if (growSystem == null) return;
        
        // HP bertambah berdasarkan growth stage
        maxHealth = 100 + (growSystem.currentGrowthStage * healthPerStage);
        
        // Jika current health lebih dari max health, kurangi
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        // Update UI
        UpdateUI();
    }
    
    private IEnumerator DamageShake()
    {
        // Shake effect ketika terkena damage
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.3f;
        float shakeMagnitude = 0.2f;
        
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = originalPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            float z = originalPosition.z + Random.Range(-shakeMagnitude, shakeMagnitude);
            
            transform.position = new Vector3(x, y, z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }
    
    public void SetInvincible(bool invincible, float duration = 0f)
    {
        isInvincible = invincible;
        
        if (duration > 0)
        {
            StartCoroutine(InvincibilityTimer(duration));
        }
    }
    
    private IEnumerator InvincibilityTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }
    
    // ==================== EXPERIENCE & LEVELING ====================

    public void AddExperience(int exp)
    {
        currentExperience += exp;
        
        // Trigger event
        OnExperienceGained?.Invoke(exp);
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnExperienceGained(exp);
        }

        // Check level up
        while (CanLevelUp())
        {
            LevelUp();
        }

        UpdateUI();
    }

    private bool CanLevelUp()
    {
        if (growSystem == null) return false;
        if (growSystem.currentGrowthStage >= growSystem.maxGrowthStage) return false;
        
        int requiredExp = experienceToNextStage[growSystem.currentGrowthStage];
        return currentExperience >= requiredExp;
    }

    private void LevelUp()
    {
        if (growSystem == null) return;
        if (growSystem.currentGrowthStage >= growSystem.maxGrowthStage) return;

        // Kurangi experience
        int requiredExp = experienceToNextStage[growSystem.currentGrowthStage];
        currentExperience -= requiredExp;

        // Naik stage menggunakan GrowSystem
        growSystem.GrowToNextStage();
        
        // Update max health setelah level up
        UpdateMaxHealth();
        
        // Full heal saat level up (optional)
        Heal(maxHealth);

        // Trigger event
        OnLevelUp?.Invoke(growSystem.currentGrowthStage);
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerLevelUp(growSystem.currentGrowthStage);
        }

        Debug.Log($"Level Up! Now at Stage {growSystem.currentGrowthStage}, Max HP: {maxHealth}");
    }

    // ==================== UI METHODS ====================
public void UpdateUI()
{
  if (stageText == null || expSlider == null || healthSlider == null)
    {
        FindAndBindUI();
    }
    
    if (stageText != null && growSystem != null)
        stageText.text = $"Level: {growSystem.currentGrowthStage}";

    if (expSlider != null && growSystem != null)
    {
        if (growSystem.currentGrowthStage < growSystem.maxGrowthStage)
        {
            int requiredExp = experienceToNextStage[growSystem.currentGrowthStage];
            expSlider.maxValue = requiredExp;
            expSlider.value = currentExperience;
        }
        else
        {
            expSlider.value = expSlider.maxValue;
        }
    }
    
    if (healthSlider != null)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        
        if (healthFillImage != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }
    }
}

    private void ShowCannotEatFeedback()
    {
        // Visual feedback bahwa tidak bisa makan
        StartCoroutine(ShakePlayer());
    }

    private IEnumerator ShakePlayer()
    {
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.5f;
        float shakeMagnitude = 0.1f;
        
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = originalPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            float z = originalPosition.z + Random.Range(-shakeMagnitude, shakeMagnitude);
            
            transform.position = new Vector3(x, y, z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }

    // ==================== PUBLIC METHODS ====================

    public int GetCurrentGrowthStage()
    {
        if (growSystem == null) return 0;
        return growSystem.currentGrowthStage;
    }

    public int GetCurrentExperience()
    {
        return currentExperience;
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    public int GetDeathCount()
    {
        return deathCount;
    }
    
    public int GetRemainingLives()
    {
        return Mathf.Max(0, maxDeathCount - deathCount);
    }

    public void AddExpDirectly(int exp)
    {
        AddExperience(exp);
    }
    
    public void SetFullHealth()
    {
        currentHealth = maxHealth;
        UpdateUI();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        UpdateUI();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void ResetDeathCount()
    {
        deathCount = 0;
    }

    // ==================== GIZMOS ====================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
        
        // Draw respawn position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(respawnPosition, 1f);
        Gizmos.DrawLine(transform.position, respawnPosition);
        
        // Draw health bar in scene view
        if (Application.isPlaying)
        {
            Vector3 barPosition = transform.position + Vector3.up * 2f;
            float barWidth = 2f;
            float barHeight = 0.2f;
            
            // Background
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));
            
            // Health fill
            float healthPercent = (float)currentHealth / maxHealth;
            Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
            Gizmos.DrawCube(
                barPosition - new Vector3((barWidth / 2) * (1 - healthPercent), 0, 0),
                new Vector3(barWidth * healthPercent, barHeight, 0.2f)
            );
            
            // Stage indicator
            if (growSystem != null)
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2.5f,
                    $"Stage: {growSystem.currentGrowthStage}",
                    new GUIStyle { normal = { textColor = Color.yellow } }
                );
                #endif
            }
            
            // Death count indicator
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                $"Lives: {GetRemainingLives()}/{maxDeathCount}",
                new GUIStyle { normal = { textColor = Color.cyan } }
            );
            #endif
        }
    }

}