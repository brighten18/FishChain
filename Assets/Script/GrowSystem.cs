using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GrowSystem : MonoBehaviour
{
    [Header("Growth Stages Configuration")]
    public int currentGrowthStage = 0;
    public int maxGrowthStage = 5;
    
    [Header("Scale Settings per Stage")]
    public Vector3[] growthScales = new Vector3[]
    {
        new Vector3(1.0f, 1.0f, 1.0f),    // Stage 0
        new Vector3(1.2f, 1.2f, 1.2f),    // Stage 1  
        new Vector3(1.8f, 1.8f, 1.8f),    // Stage 2
        new Vector3(2.2f, 2.2f, 2.2f),    // Stage 3
        new Vector3(3.0f, 3.0f, 3.0f),    // Stage 4
        new Vector3(4.0f, 4.0f, 4.0f)     // Stage 5
    };

    [Header("Cinemachine Shoulder Offset Settings")]
    public CinemachineVirtualCamera virtualCamera;
    
    [Header("Shoulder Offset per Stage")]
    public Vector3[] shoulderOffsets = new Vector3[]
    {
        new Vector3(0.5f, 0.5f, 0),    // Stage 0 - dekat dan rendah
        new Vector3(0.8f, 0.8f, 0),    // Stage 1
        new Vector3(1.2f, 1.2f, 0),    // Stage 2  
        new Vector3(1.8f, 1.8f, 0),    // Stage 3
        new Vector3(2.5f, 2.5f, 0),    // Stage 4
        new Vector3(3.5f, 3.5f, 0)     // Stage 5 - jauh dan tinggi
    };

    [Header("Camera Distance per Stage")]
    public float[] cameraDistances = new float[]
    {
        2f,    // Stage 0
        3f,    // Stage 1
        4f,    // Stage 2
        6f,    // Stage 3  
        8f,    // Stage 4
        12f    // Stage 5
    };

    [Header("Growth Settings")]
    public float growthDuration = 2.0f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Events
    public System.Action<int> OnGrowthStageChanged;
    public System.Action<int> OnGrowthComplete;
    public System.Action OnGrowthReset;

    // Private variables
    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private Vector3 initialScale;
    private bool isGrowing = false;
    private bool isResetting = false;

    void Start()
    {
        InitializeCameraComponents();
        initialScale = transform.localScale;
        ApplyGrowthStage(currentGrowthStage, true);
        
        // Register events
        RegisterGameEvents();
    }

    void InitializeCameraComponents()
    {
        if (virtualCamera != null)
        {
            thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            
            if (thirdPersonFollow == null)
            {
                Debug.LogError("Cinemachine3rdPersonFollow component not found! Please add 3rd Person Follow to your virtual camera.");
            }
        }
        else
        {
            Debug.LogError("Virtual Camera not assigned!");
        }
    }

    void RegisterGameEvents()
    {
        // Subscribe to GameManager events jika ada
        if (GameManager.Instance != null)
        {
            // GameManager akan meng-handle reset via LevelSystem
        }
    }

    void Update()
    {
        // Debug controls untuk test growth system
        HandleDebugInput();
    }

    void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) GrowToStage(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GrowToStage(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GrowToStage(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GrowToStage(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) GrowToStage(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) GrowToStage(5);
        
        if (Input.GetKeyDown(KeyCode.Equals)) GrowToNextStage();  // Tombol +
        if (Input.GetKeyDown(KeyCode.Minus)) GrowToPreviousStage(); // Tombol -
        
        // Debug reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetToInitialStage();
        }
    }

    public void GrowToNextStage()
    {
        if (!isGrowing && !isResetting && currentGrowthStage < maxGrowthStage)
        {
            int previousStage = currentGrowthStage;
            currentGrowthStage++;
            StartCoroutine(AnimateGrowth(currentGrowthStage, previousStage));
        }
    }

    public void GrowToPreviousStage()
    {
        if (!isGrowing && !isResetting && currentGrowthStage > 0)
        {
            int previousStage = currentGrowthStage;
            currentGrowthStage--;
            StartCoroutine(AnimateGrowth(currentGrowthStage, previousStage));
        }
    }

    public void GrowToStage(int targetStage)
    {
        if (!isGrowing && !isResetting && targetStage >= 0 && targetStage <= maxGrowthStage)
        {
            int previousStage = currentGrowthStage;
            currentGrowthStage = targetStage;
            StartCoroutine(AnimateGrowth(targetStage, previousStage));
        }
    }

    private IEnumerator AnimateGrowth(int targetStage, int previousStage = -1)
    {
        isGrowing = true;

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = growthScales[targetStage];
        
        Vector3 startShoulderOffset = thirdPersonFollow != null ? thirdPersonFollow.ShoulderOffset : Vector3.zero;
        Vector3 targetShoulderOffset = shoulderOffsets[targetStage];
        
        float startCameraDistance = thirdPersonFollow != null ? thirdPersonFollow.CameraDistance : 0f;
        float targetCameraDistance = cameraDistances[targetStage];

        float elapsedTime = 0f;

        // Trigger event sebelum growth
        OnGrowthStageChanged?.Invoke(targetStage);

        while (elapsedTime < growthDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = growthCurve.Evaluate(elapsedTime / growthDuration);

            // Animate scale
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            // Animate shoulder offset
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.ShoulderOffset = Vector3.Lerp(startShoulderOffset, targetShoulderOffset, t);
                thirdPersonFollow.CameraDistance = Mathf.Lerp(startCameraDistance, targetCameraDistance, t);
            }

            yield return null;
        }

        // Ensure final values
        transform.localScale = targetScale;
        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.ShoulderOffset = targetShoulderOffset;
            thirdPersonFollow.CameraDistance = targetCameraDistance;
        }

        isGrowing = false;
        
        // Trigger event setelah growth complete
        OnGrowthComplete?.Invoke(targetStage);
        
        Debug.Log($"Growth completed! Stage: {targetStage}, Scale: {targetScale}, Shoulder Offset: {targetShoulderOffset}");
    }

    private void ApplyGrowthStage(int stage, bool immediate = false)
    {
        if (stage < 0 || stage > maxGrowthStage) return;

        if (immediate)
        {
            transform.localScale = growthScales[stage];
            
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.ShoulderOffset = shoulderOffsets[stage];
                thirdPersonFollow.CameraDistance = cameraDistances[stage];
            }
            
            // Trigger event
            OnGrowthStageChanged?.Invoke(stage);
        }
        else
        {
            StartCoroutine(AnimateGrowth(stage, currentGrowthStage));
        }
    }

    // ==================== RESET SYSTEM ====================
    
    /// <summary>
    /// Reset player ke stage awal (saat mati atau restart game)
    /// </summary>
    public void ResetToInitialStage()
    {
        if (isGrowing || isResetting) return;
        
        StartCoroutine(ResetGrowthAnimation(0));
    }
    
    /// <summary>
    /// Reset ke stage tertentu dengan animasi
    /// </summary>
    public void ResetToStage(int targetStage)
    {
        if (isGrowing || isResetting) return;
        
        StartCoroutine(ResetGrowthAnimation(targetStage));
    }
    
    private IEnumerator ResetGrowthAnimation(int targetStage)
    {
        isResetting = true;
        
        // Store current stage untuk event
        int previousStage = currentGrowthStage;
        
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = growthScales[targetStage];
        
        Vector3 startShoulderOffset = thirdPersonFollow != null ? thirdPersonFollow.ShoulderOffset : Vector3.zero;
        Vector3 targetShoulderOffset = shoulderOffsets[targetStage];
        
        float startCameraDistance = thirdPersonFollow != null ? thirdPersonFollow.CameraDistance : 0f;
        float targetCameraDistance = cameraDistances[targetStage];

        float elapsedTime = 0f;

        while (elapsedTime < growthDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = growthCurve.Evaluate(elapsedTime / growthDuration);

            // Animate scale
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            // Animate shoulder offset
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.ShoulderOffset = Vector3.Lerp(startShoulderOffset, targetShoulderOffset, t);
                thirdPersonFollow.CameraDistance = Mathf.Lerp(startCameraDistance, targetCameraDistance, t);
            }

            yield return null;
        }

        // Set final values
        currentGrowthStage = targetStage;
        transform.localScale = targetScale;
        
        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.ShoulderOffset = targetShoulderOffset;
            thirdPersonFollow.CameraDistance = targetCameraDistance;
        }

        isResetting = false;
        
        // Trigger reset event
        OnGrowthReset?.Invoke();
        OnGrowthStageChanged?.Invoke(targetStage);
        
        Debug.Log($"Growth reset to stage {targetStage}");
    }
    
    // ==================== DEATH HANDLER ====================
    
    /// <summary>
    /// Dipanggil saat player mati (bisa dari LevelSystem atau GameManager)
    /// </summary>
    public void OnPlayerDied()
    {
        Debug.Log("GrowSystem: Player died event received");
        
        // Disini bisa tambahkan efek visual saat mati
        // Contoh: shrink effect, particle effect, dll
    }
    
    /// <summary>
    /// Dipanggil saat player respawn
    /// </summary>
    public void OnPlayerRespawned(bool resetGrowth = false)
    {
        Debug.Log("GrowSystem: Player respawned");
        
        if (resetGrowth)
        {
            ResetToInitialStage();
        }
    }

    // ==================== PUBLIC METHODS ====================

    public void AddGrowthExperience(int exp = 1)
    {
        int targetStage = Mathf.Min(currentGrowthStage + exp, maxGrowthStage);
        GrowToStage(targetStage);
    }

    public void SetGrowthStage(int stage)
    {
        GrowToStage(stage);
    }

    public int GetCurrentGrowthStage()
    {
        return currentGrowthStage;
    }

    public float GetGrowthProgress()
    {
        return (float)currentGrowthStage / maxGrowthStage;
    }
    
    public bool IsGrowing()
    {
        return isGrowing;
    }
    
    public bool IsResetting()
    {
        return isResetting;
    }
    
    public bool CanGrow()
    {
        return currentGrowthStage < maxGrowthStage && !isGrowing && !isResetting;
    }
    
    public bool CanShrink()
    {
        return currentGrowthStage > 0 && !isGrowing && !isResetting;
    }

    // Method untuk mendapatkan setting camera saat ini
    public Vector3 GetCurrentShoulderOffset()
    {
        return thirdPersonFollow != null ? thirdPersonFollow.ShoulderOffset : Vector3.zero;
    }

    public float GetCurrentCameraDistance()
    {
        return thirdPersonFollow != null ? thirdPersonFollow.CameraDistance : 0f;
    }
    
    // Method untuk mendapatkan scale multiplier
    public float GetScaleMultiplier()
    {
        if (currentGrowthStage < 0 || currentGrowthStage >= growthScales.Length)
            return 1f;
            
        return growthScales[currentGrowthStage].x; // Assuming uniform scaling
    }
    
    // Method untuk mendapatkan stage berdasarkan scale
    public int GetStageFromScale(float scale)
    {
        for (int i = 0; i < growthScales.Length; i++)
        {
            if (Mathf.Approximately(growthScales[i].x, scale))
                return i;
        }
        return 0;
    }
    
    // ==================== GIZMOS & DEBUG ====================
    
    private void OnDrawGizmosSelected()
    {
        // Draw growth stage indicators
        Gizmos.color = Color.green;
        for (int i = 0; i <= maxGrowthStage; i++)
        {
            Vector3 stageScale = growthScales[i];
            Vector3 stageSize = stageScale * 1.5f;
            
            Gizmos.DrawWireSphere(transform.position + Vector3.up * (i * 2f), stageSize.x);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (i * 2f + 0.5f),
                $"Stage {i}: {stageScale}",
                new GUIStyle { normal = { textColor = Color.green } }
            );
            #endif
        }
        
        // Draw current stage highlight
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 currentScale = growthScales[currentGrowthStage];
            Gizmos.DrawWireSphere(transform.position, currentScale.x * 1.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"Current: Stage {currentGrowthStage}",
                new GUIStyle { normal = { textColor = Color.yellow } }
            );
            #endif
        }
    }
    
    // ==================== EDITOR HELPERS ====================
    
    #if UNITY_EDITOR
    [ContextMenu("Validate Growth Arrays")]
    void ValidateArrays()
    {
        if (growthScales.Length != maxGrowthStage + 1)
        {
            Debug.LogWarning($"growthScales array length ({growthScales.Length}) should be {maxGrowthStage + 1}");
            System.Array.Resize(ref growthScales, maxGrowthStage + 1);
        }
        
        if (shoulderOffsets.Length != maxGrowthStage + 1)
        {
            Debug.LogWarning($"shoulderOffsets array length ({shoulderOffsets.Length}) should be {maxGrowthStage + 1}");
            System.Array.Resize(ref shoulderOffsets, maxGrowthStage + 1);
        }
        
        if (cameraDistances.Length != maxGrowthStage + 1)
        {
            Debug.LogWarning($"cameraDistances array length ({cameraDistances.Length}) should be {maxGrowthStage + 1}");
            System.Array.Resize(ref cameraDistances, maxGrowthStage + 1);
        }
        
        Debug.Log("Growth arrays validated!");
    }
    
    [ContextMenu("Set All Arrays to Current Max Stage")]
    void SetArraysToMaxStage()
    {
        growthScales = new Vector3[maxGrowthStage + 1];
        shoulderOffsets = new Vector3[maxGrowthStage + 1];
        cameraDistances = new float[maxGrowthStage + 1];
        
        // Set default values
        for (int i = 0; i <= maxGrowthStage; i++)
        {
            float scaleMultiplier = 1.0f + (i * 0.5f);
            growthScales[i] = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
            shoulderOffsets[i] = new Vector3(0.5f + i * 0.5f, 0.5f + i * 0.5f, 0);
            cameraDistances[i] = 2f + i * 2f;
        }
        
        Debug.Log($"All arrays set to size {maxGrowthStage + 1}");
    }
    #endif
}