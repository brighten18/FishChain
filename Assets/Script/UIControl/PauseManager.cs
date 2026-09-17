using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("UI Settings")]
    public string pausePanelName = "PausePanel";
    
    [Header("UI Manual Assignment (Optional)")]
    [Tooltip("Assign manual jika panel tidak ditemukan otomatis")]
    public GameObject pausePanelManual;
    [Tooltip("Assign manual jika button tidak ditemukan otomatis")]
    public Button resumeButtonManual;
    public Button quitButtonManual;
    
    [Header("Input Settings")]
    [Tooltip("Reference to PlayerInput component (usually on Player GameObject)")]
    public PlayerInput playerInput;
    public string pauseActionName = "Pause";
    
    [Header("Game Settings")]
    public bool canPause = true;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }
    
    private GameObject _pauseMenuPanel;
    private Button _resumeButton;
    private Button _quitButton;
    private GameObject _lastSelectedButton;
    private EventSystem _eventSystem;
    private InputAction _pauseAction;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (Instance != this) return;
        
        _eventSystem = EventSystem.current;
        IsPaused = false;
        Time.timeScale = 1f;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(InitializeUICoroutine());
        
        SetupInputActions();
        
        DebugLog("PauseManager initialized");
    }
    
    private void SetupInputActions()
    {
        if (playerInput == null)
        {
            DebugLog("PlayerInput not assigned, searching for it...");
            playerInput = FindObjectOfType<PlayerInput>();
            
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput component not found! Please assign it manually or ensure it exists in the scene.");
                return;
            }
            else
            {
                DebugLog($"Found PlayerInput on: {playerInput.gameObject.name}");
            }
        }
        
        _pauseAction = playerInput.actions.FindAction(pauseActionName);
        
        if (_pauseAction != null)
        {
            _pauseAction.Enable();
            _pauseAction.performed += OnPauseActionPerformed;
            DebugLog($"Pause action '{pauseActionName}' successfully bound!");
        }
        else
        {
            Debug.LogError($"Pause action '{pauseActionName}' not found in PlayerInput actions! Check the action name.");
            
            DebugLog("Available actions:");
            foreach (var action in playerInput.actions)
            {
                DebugLog($"  - {action.name}");
            }
        }
    }
    
    private void OnPauseActionPerformed(InputAction.CallbackContext context)
    {
        if (context.performed && canPause)
        {
            TogglePause();
        }
    }
    
    private IEnumerator InitializeUICoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        FindAndBindUI();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            SetupInputActions();
        }
        
        StartCoroutine(RebindUIAfterSceneLoad());
    }
    
    private IEnumerator RebindUIAfterSceneLoad()
    {
        yield return new WaitForSeconds(0.05f);
        FindAndBindUI();
    }
    
    private void FindAndBindUI()
    {
        DebugLog("=== UI SEARCH STARTED ===");
        
        if (pausePanelManual != null)
        {
            _pauseMenuPanel = pausePanelManual;
            DebugLog($"✓ Pause Panel assigned manually: {_pauseMenuPanel.name}");
        }
        else
        {
            try
            {
                _pauseMenuPanel = GameObject.FindGameObjectWithTag(pausePanelName);
                DebugLog($"✓ Found PausePanel by Tag: {_pauseMenuPanel.name}");
            }
            catch
            {
                DebugLog("Tag search failed, trying other methods...");
            }
            
            if (_pauseMenuPanel == null)
            {
                DebugLog("Searching in all Canvases...");
                Canvas[] canvases = FindObjectsOfType<Canvas>(true);
                DebugLog($"Found {canvases.Length} Canvas(es) in scene");
                
                foreach (var canvas in canvases)
                {
                    DebugLog($"  Checking Canvas: {canvas.name}");
                    
                    Transform panel = canvas.transform.Find(pausePanelName);
                    if (panel != null)
                    {
                        _pauseMenuPanel = panel.gameObject;
                        DebugLog($"✓ Found PausePanel as direct child of {canvas.name}");
                        break;
                    }
                    
                    panel = FindChildRecursive(canvas.transform, pausePanelName);
                    if (panel != null)
                    {
                        _pauseMenuPanel = panel.gameObject;
                        DebugLog($"✓ Found PausePanel in hierarchy of {canvas.name}");
                        break;
                    }
                }
            }
            
            if (_pauseMenuPanel == null)
            {
                DebugLog("Searching all GameObjects by name...");
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name == pausePanelName && obj.scene.isLoaded)
                    {
                        _pauseMenuPanel = obj;
                        DebugLog($"✓ Found PausePanel via Resources search");
                        break;
                    }
                }
            }
        }
        
        if (_pauseMenuPanel == null)
        {
            Debug.LogError($"✗ FAILED: Pause panel '{pausePanelName}' not found!");
            Debug.LogError("Solutions:");
            Debug.LogError("  1. Assign manually in 'pausePanelManual' field");
            Debug.LogError("  2. Ensure GameObject exists with exact name: " + pausePanelName);
            Debug.LogError("  3. Check that PausePanel has tag 'PausePanel'");
            LogSceneUIHierarchy();
            return;
        }
        
        DebugLog($"✓ Pause Panel found: {_pauseMenuPanel.name} at path: {GetGameObjectPath(_pauseMenuPanel)}");
        DebugLog($"  - Active: {_pauseMenuPanel.activeSelf}");
        DebugLog($"  - Tag: {_pauseMenuPanel.tag}");
        DebugLog($"  - Layer: {LayerMask.LayerToName(_pauseMenuPanel.layer)}");
        
        if (_pauseMenuPanel.activeSelf)
        {
            _pauseMenuPanel.SetActive(false);
        }
        
        FindButtons();
        SetupButtonListeners();
        
        DebugLog("=== UI SEARCH COMPLETED ===");
    }
    
    private void FindButtons()
    {
        if (_pauseMenuPanel == null) return;
        
        if (resumeButtonManual != null)
        {
            _resumeButton = resumeButtonManual;
            DebugLog($"Resume button manual: {_resumeButton.name}");
        }
        
        if (quitButtonManual != null)
        {
            _quitButton = quitButtonManual;
            DebugLog($"Quit button manual: {_quitButton.name}");
        }
        
        if (_resumeButton == null || _quitButton == null)
        {
            DebugLog("Searching for buttons...");
            var buttons = _pauseMenuPanel.GetComponentsInChildren<Button>(true);
            DebugLog($"Found {buttons.Length} button(s) in PausePanel");
            
            foreach (var button in buttons)
            {
                DebugLog($"  - Button: {button.name}");
                
                string buttonNameLower = button.name.ToLower();
                
                if (_resumeButton == null && (buttonNameLower.Contains("resume") || buttonNameLower.Contains("continue")))
                {
                    _resumeButton = button;
                    DebugLog($"    ✓ Assigned as Resume button");
                }
                else if (_quitButton == null && (buttonNameLower.Contains("quit") || buttonNameLower.Contains("exit")))
                {
                    _quitButton = button;
                    DebugLog($"    ✓ Assigned as Quit button");
                }
            }
        }
        
        if (_resumeButton == null)
            Debug.LogWarning("Resume button tidak ditemukan! Assign manual atau pastikan nama mengandung 'Resume'/'Continue'");
        
        if (_quitButton == null)
            Debug.LogWarning("Quit button tidak ditemukan! Assign manual atau pastikan nama mengandung 'Quit'/'Exit'");
    }
    
    private void SetupButtonListeners()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveAllListeners();
            _resumeButton.onClick.AddListener(ResumeGame);
            DebugLog($"Resume button listener ditambahkan: {_resumeButton.name}");
        }
        
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveAllListeners();
            _quitButton.onClick.AddListener(QuitToMenu);
            DebugLog($"Quit button listener ditambahkan: {_quitButton.name}");
        }
    }
    
    public void TogglePause()
    {
        if (!canPause) return;
        
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        if (!canPause || IsPaused) return;
        
        if (_pauseMenuPanel == null)
        {
            FindAndBindUI();
            
            if (_pauseMenuPanel == null)
            {
                Debug.LogError("Cannot pause: No pause panel found!");
                return;
            }
        }
        
        IsPaused = true;
        Time.timeScale = 0f;
        
        _pauseMenuPanel.SetActive(true);
        SetInitialButtonSelection();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        AudioListener.pause = true;
        
        DebugLog("Game Paused");
    }
    
    public void ResumeGame()
    {
        if (!IsPaused) return;
        
        IsPaused = false;
        Time.timeScale = 1f;
        
        if (_eventSystem != null && _eventSystem.currentSelectedGameObject != null)
        {
            _lastSelectedButton = _eventSystem.currentSelectedGameObject;
        }
        
        if (_pauseMenuPanel != null)
        {
            _pauseMenuPanel.SetActive(false);
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        AudioListener.pause = false;
        
        DebugLog("Game Resumed");
    }
    
    private void SetInitialButtonSelection()
    {
        if (_eventSystem == null)
        {
            _eventSystem = EventSystem.current;
            if (_eventSystem == null) return;
        }
        
        GameObject buttonToSelect = _lastSelectedButton ?? 
                                   (_resumeButton != null ? _resumeButton.gameObject : null) ?? 
                                   (_quitButton != null ? _quitButton.gameObject : null);
        
        if (buttonToSelect != null)
        {
            _eventSystem.SetSelectedGameObject(buttonToSelect);
        }
    }
    
    private void QuitToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        
        var dontDestroyObj = FindObjectOfType<DontDestoyObj>();
        if (dontDestroyObj != null)
        {
            dontDestroyObj.DestroyAllPersistentObjects();
        }
        else
        {
            SceneManager.LoadScene("Home");
        }
    }
    
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return "/" + path;
    }
    
    private void LogSceneUIHierarchy()
    {
        Debug.Log("=== SCENE UI HIERARCHY DEBUG ===");
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        
        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name} (Active: {canvas.gameObject.activeSelf})");
            LogChildrenRecursive(canvas.transform, 1);
        }
        
        Debug.Log("=== END UI HIERARCHY ===");
    }
    
    private void LogChildrenRecursive(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        
        foreach (Transform child in parent)
        {
            string activeStatus = child.gameObject.activeSelf ? "✓" : "✗";
            Debug.Log($"{indent}{activeStatus} {child.name} (Tag: {child.tag})");
            
            if (child.childCount > 0)
            {
                LogChildrenRecursive(child, depth + 1);
            }
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    [ContextMenu("Test Find UI")]
    public void TestFindUI()
    {
        Debug.Log("=== MANUAL UI TEST ===");
        FindAndBindUI();
        
        if (_pauseMenuPanel != null)
        {
            Debug.Log("SUCCESS: PausePanel found!");
        }
        else
        {
            Debug.LogError("FAILED: PausePanel not found!");
        }
    }
    
    [ContextMenu("Test Input Actions")]
    public void TestInputActions()
    {
        Debug.Log("=== INPUT ACTIONS TEST ===");
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput is NULL!");
            return;
        }
        
        Debug.Log($"PlayerInput found on: {playerInput.gameObject.name}");
        Debug.Log($"Actions asset: {playerInput.actions.name}");
        Debug.Log("Available actions:");
        
        foreach (var action in playerInput.actions)
        {
            Debug.Log($"  - {action.name} (Enabled: {action.enabled})");
        }
        
        var pauseAction = playerInput.actions.FindAction(pauseActionName);
        if (pauseAction != null)
        {
            Debug.Log($"✓ Pause action '{pauseActionName}' found!");
        }
        else
        {
            Debug.LogError($"✗ Pause action '{pauseActionName}' NOT found!");
        }
    }
    
    void OnDestroy()
    {
        if (_pauseAction != null)
        {
            _pauseAction.performed -= OnPauseActionPerformed;
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (IsPaused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
