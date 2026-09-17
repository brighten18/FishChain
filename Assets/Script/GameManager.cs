using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Scene Management")]
    public string mainMenuScene = "Home";
    public string gameScene = "MainLevel";
    public string loseScene = "Lose";
    public string winScene = "Win";
    
    [Header("Game State")]
    public bool isGameOver = false;
    public bool isGamePaused = false;
    
    [Header("Win/Lose Settings")]
    public int winMaxLevel = 5;
    public int winMaxExp = 20;
    public float winSceneDelay = 2f;
    public float loseSceneDelay = 1f;
    
    private Vector3 currentCheckpoint = Vector3.zero;
    private LevelSystem playerLevelSystem;
    private bool isInitialized = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Debug.Log("Duplicate GameManager found, destroying...");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        isInitialized = true;
        
        Debug.Log("GameManager initialized");
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager detected scene load: {scene.name}");
        
        if (scene.name == "MainLevel" || scene.name == gameScene)
        {
            isGameOver = false;
            playerLevelSystem = null;
            
            StartCoroutine(WaitAndRegisterPlayer());
            
            Debug.Log("GameManager reset for gameplay scene");
        }
        else if (scene.name == mainMenuScene || scene.name == "Home")
        {
            ResetCheckpoint();
            isGameOver = false;
            
            // Cleanup saat kembali ke menu
            CleanupDontDestroyObjects();
            
            Debug.Log("GameManager reset for main menu");
        }
    }
    
    private IEnumerator WaitAndRegisterPlayer()
    {
        yield return new WaitForSeconds(0.1f);
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            LevelSystem levelSystem = playerObj.GetComponent<LevelSystem>();
            if (levelSystem != null)
            {
                RegisterPlayer(levelSystem);
            }
        }
    }
    
    public void RegisterPlayer(LevelSystem levelSystem)
    {
        playerLevelSystem = levelSystem;
        Debug.Log("Player registered to GameManager");
    }
    
    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        currentCheckpoint = checkpointPosition;
        Debug.Log($"Checkpoint set at: {checkpointPosition}");
    }
    
    public Vector3 GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }
    
    public void ResetCheckpoint()
    {
        currentCheckpoint = Vector3.zero;
    }
    
    public void OnPlayerDamaged(int damage, int currentHealth, int maxHealth)
    {
        Debug.Log($"Player damaged: {damage}, Health: {currentHealth}/{maxHealth}");
    }
    
    public void OnPlayerDied(int deathCount)
    {
        Debug.Log($"Player died! Total deaths: {deathCount}");
    }
    
    public void OnPlayerRespawned()
    {
        Debug.Log("Player respawned");
        
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
    
    public void OnExperienceGained(int exp)
    {
        Debug.Log($"Experience gained: {exp}");
    }
    
    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"Player leveled up to: {newLevel}");
    }
    
    // ==================== WIN/LOSE METHODS ====================
    
    public bool CheckWinCondition(int playerLevel, int playerExp, int maxLevel = 5, int maxExp = 20)
    {
        if (isGameOver) return false;
        
        bool hasWon = (playerLevel >= maxLevel) || (playerExp >= maxExp);
        
        if (hasWon)
        {
            PlayerWon();
            return true;
        }
        
        return false;
    }
    
    public bool CheckLoseCondition(int deathCount, int maxDeaths = 3)
    {
        if (isGameOver) return false;
        
        bool hasLost = (deathCount >= maxDeaths);
        
        if (hasLost)
        {
            PlayerLost();
            return true;
        }
        
        return false;
    }
    
    public void PlayerWon()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0f;
        
        Debug.Log("Player Won! Loading win scene...");
        
        CleanupDontDestroyObjects();
        StartCoroutine(LoadSceneWithDelay(winScene, winSceneDelay));
    }
    
    public void PlayerLost()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0f;
        
        Debug.Log("Player Lost! Loading lose scene...");
        
        CleanupDontDestroyObjects();
        StartCoroutine(LoadSceneWithDelay(loseScene, loseSceneDelay));
    }
    
    // ==================== CLEANUP SYSTEM ====================
    
    private void CleanupDontDestroyObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == this.gameObject) continue;
            
            if (obj.scene.name == "DontDestroyOnLoad")
            {
                // Skip essential objects
                if (obj.name.Contains("EventSystem") || 
                    obj.name.Contains("Audio") || 
                    obj.name.Contains("Camera"))
                {
                    continue;
                }
                
                Destroy(obj);
                Debug.Log($"Destroyed: {obj.name}");
            }
        }
        
        // Cleanup PauseManager jika ada
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        if (pauseManager != null && pauseManager.gameObject != this.gameObject)
        {
            Destroy(pauseManager.gameObject);
            Debug.Log("Destroyed PauseManager");
        }
    }
    
    // ==================== SCENE MANAGEMENT ====================
    
    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneName);
    }
    
    public void RestartGame()
    {
        ResetCheckpoint();
        isGameOver = false;
        Time.timeScale = 1f;
        
        CleanupDontDestroyObjects();
        SceneManager.LoadScene(gameScene);
    }
    
    public void GoToMainMenu()
    {
        ResetCheckpoint();
        isGameOver = false;
        Time.timeScale = 1f;
        
        CleanupDontDestroyObjects();
        SceneManager.LoadScene(mainMenuScene);
    }
    
    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;
        
        Debug.Log($"Game {(isGamePaused ? "Paused" : "Resumed")}");
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting application");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        Debug.Log("GameManager destroyed");
    }
    
    // ==================== DEBUG METHODS ====================
    
    [ContextMenu("Test Win")]
    public void TestWin()
    {
        PlayerWon();
    }
    
    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        PlayerLost();
    }
}