using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestoyObj : MonoBehaviour
{
    public static DontDestoyObj Instance;
    
    [Header("Scene Settings")]
    public string[] mainMenuScenes = { "Home", "MainMenu" };
    public string[] gameplayScenes = { "MainLevel" };
    
    private bool isCleaningUp = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("DontDestoyObj initialized and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("Duplicate DontDestoyObj found, destroying...");
            Destroy(gameObject);
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        
        bool isMainMenu = IsMainMenuScene(scene.name);
        
        if (isMainMenu)
        {
            Debug.Log("Returning to main menu, cleaning up all persistent objects...");
            DestroyAllPersistentObjects();
        }
    }
    
    private bool IsMainMenuScene(string sceneName)
    {
        foreach (string menuScene in mainMenuScenes)
        {
            if (sceneName == menuScene)
                return true;
        }
        return false;
    }
    
    public void DestroyAllPersistentObjects()
    {
        if (isCleaningUp)
        {
            Debug.Log("Already cleaning up, skipping duplicate cleanup call");
            return;
        }
        
        StartCoroutine(CleanupAndLoadScene("Home"));
    }
    
private IEnumerator CleanupAndLoadScene(string targetScene)
{
    isCleaningUp = true;
    
    Time.timeScale = 1f;
    AudioListener.pause = false;
    
    if (UIReferenceManager.Instance != null)
    {
        Debug.Log("Destroying UIReferenceManager instance");
        Destroy(UIReferenceManager.Instance.gameObject);
    }
    
    if (PauseManager.Instance != null)
    {
        Debug.Log("Destroying PauseManager instance");
        Destroy(PauseManager.Instance.gameObject);
    }
    
    if (GameManager.Instance != null)
    {
        Debug.Log("Destroying GameManager instance");
        Destroy(GameManager.Instance.gameObject);
        GameManager.Instance = null;
    }
    
    GameObject[] persistentObjects = GameObject.FindGameObjectsWithTag("Player");
    foreach (GameObject obj in persistentObjects)
    {
        if (obj != null)
        {
            Debug.Log($"Destroying persistent object: {obj.name}");
            Destroy(obj);
        }
    }
    
    yield return null;
    
    Debug.Log($"All persistent objects destroyed, loading {targetScene}");
    SceneManager.LoadScene(targetScene);
    
    yield return null;
    
    Destroy(gameObject);
}

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        Debug.Log("DontDestoyObj destroyed");
    }
}
