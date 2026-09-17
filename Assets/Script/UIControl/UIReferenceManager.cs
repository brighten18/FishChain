using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIReferenceManager : MonoBehaviour
{
    public static UIReferenceManager Instance { get; private set; }

    [Header("Scene UI Settings")]
    public string[] uiScenes = { "MainLevel" };
    
    [Header("Current UI References")]
    public Text levelText;
    public Slider expSlider;
    public Slider healthSlider;
    public Image healthFillImage;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button quitButton;

    private bool hasUIInCurrentScene = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("UIReferenceManager: Initialized and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("UIReferenceManager: Duplicate found, destroying");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        CheckAndRebindUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"UIReferenceManager: Scene loaded - {scene.name}");
        
        CheckAndRebindUI();
    }

    private void CheckAndRebindUI()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        hasUIInCurrentScene = IsUIScene(currentScene);
        
        if (hasUIInCurrentScene)
        {
            Debug.Log($"UIReferenceManager: UI scene detected - {currentScene}");
            StartCoroutine(RebindUIAfterFrame());
        }
        else
        {
            Debug.Log($"UIReferenceManager: Non-UI scene - {currentScene}, clearing references");
            ClearAllReferences();
        }
    }

    private bool IsUIScene(string sceneName)
    {
        foreach (string uiScene in uiScenes)
        {
            if (sceneName == uiScene)
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator RebindUIAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        
        RebindAllUIReferences();
    }

    public void RebindAllUIReferences()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (!hasUIInCurrentScene && !IsUIScene(currentScene))
        {
            Debug.Log("UIReferenceManager: Not in UI scene, skipping rebind");
            return;
        }

        Debug.Log("UIReferenceManager: === Starting UI Rebind ===");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("UIReferenceManager: Canvas not found in scene!");
            return;
        }

        RebindLevelSystemUI(canvas.transform);
        RebindPauseMenuUI(canvas.transform);
        
        
        Debug.Log("UIReferenceManager: === UI Rebind Complete ===");
    }

    private void RebindLevelSystemUI(Transform canvasTransform)
    {
        levelText = FindUIComponent<Text>(canvasTransform, "Lvl_Int", "Level", "LevelText", "Stage");
        expSlider = FindUIComponent<Slider>(canvasTransform, "SliderExp", "ExpSlider", "ExperienceSlider");
        healthSlider = FindUIComponent<Slider>(canvasTransform, "SliderHP", "HealthSlider", "HPSlider");

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

        Debug.Log($"  LevelSystem UI - Level: {levelText != null}, Exp: {expSlider != null}, Health: {healthSlider != null}, HealthFill: {healthFillImage != null}");
    }

    private void RebindPauseMenuUI(Transform canvasTransform)
    {
        pausePanel = FindGameObject(canvasTransform, "PausePanel", "PauseMenu", "PauseMenuPanel");

        if (pausePanel != null)
        {
            Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
            
            resumeButton = null;
            quitButton = null;
            
            foreach (Button btn in buttons)
            {
                string btnName = btn.name.ToLower();
                
                if (btnName.Contains("resume") || btnName.Contains("continue"))
                {
                    resumeButton = btn;
                }
                else if (btnName.Contains("quit") || btnName.Contains("exit") || btnName.Contains("home"))
                {
                    quitButton = btn;
                }
            }
        }

        Debug.Log($"  PauseMenu UI - Panel: {pausePanel != null}, Resume: {resumeButton != null}, Quit: {quitButton != null}");
    }

    private T FindUIComponent<T>(Transform parent, params string[] possibleNames) where T : Component
    {
        foreach (string name in possibleNames)
        {
            Transform found = parent.Find(name);
            if (found != null)
            {
                T component = found.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }

        T[] allComponents = parent.GetComponentsInChildren<T>(true);
        foreach (T component in allComponents)
        {
            foreach (string name in possibleNames)
            {
                if (component.name.Contains(name))
                    return component;
            }
        }

        return null;
    }

    private GameObject FindGameObject(Transform parent, params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            Transform found = parent.Find(name);
            if (found != null)
                return found.gameObject;
        }

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in possibleNames)
            {
                if (child.name.Contains(name))
                    return child.gameObject;
            }
        }

        return null;
    }

    private void ClearAllReferences()
    {
        levelText = null;
        expSlider = null;
        healthSlider = null;
        healthFillImage = null;
        pausePanel = null;
        resumeButton = null;
        quitButton = null;

        Debug.Log("UIReferenceManager: All references cleared");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        Debug.Log("UIReferenceManager: Destroyed");
    }
}
