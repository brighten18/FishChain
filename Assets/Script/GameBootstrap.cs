using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    [Header("Persistent Manager Prefabs")]
    public GameObject gameManagerPrefab;
    public GameObject pauseManagerPrefab;
    public GameObject uiReferenceManagerPrefab;
    public GameObject dontDestroyObjPrefab;

    [Header("Settings")]
    public bool autoCreateManagers = true;

    private static bool hasBootstrapped = false;

    void Awake()
    {
        if (hasBootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        hasBootstrapped = true;
        
        Debug.Log("=== GameBootstrap: Initializing persistent managers ===");
        
        CreatePersistentManagers();
        
        Destroy(gameObject);
    }

    private void CreatePersistentManagers()
    {
        if (!autoCreateManagers) return;

        if (GameManager.Instance == null)
        {
            if (gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
                Debug.Log("GameBootstrap: Created GameManager from prefab");
            }
            else
            {
                GameObject gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
                Debug.Log("GameBootstrap: Created GameManager");
            }
        }

        if (PauseManager.Instance == null)
        {
            if (pauseManagerPrefab != null)
            {
                Instantiate(pauseManagerPrefab);
                Debug.Log("GameBootstrap: Created PauseManager from prefab");
            }
            else
            {
                GameObject pm = new GameObject("PauseManager");
                pm.AddComponent<PauseManager>();
                Debug.Log("GameBootstrap: Created PauseManager");
            }
        }

        if (UIReferenceManager.Instance == null)
        {
            if (uiReferenceManagerPrefab != null)
            {
                Instantiate(uiReferenceManagerPrefab);
                Debug.Log("GameBootstrap: Created UIReferenceManager from prefab");
            }
            else
            {
                GameObject ui = new GameObject("UIReferenceManager");
                ui.AddComponent<UIReferenceManager>();
                Debug.Log("GameBootstrap: Created UIReferenceManager");
            }
        }

        if (DontDestoyObj.Instance == null)
        {
            if (dontDestroyObjPrefab != null)
            {
                Instantiate(dontDestroyObjPrefab);
                Debug.Log("GameBootstrap: Created DontDestoyObj from prefab");
            }
            else
            {
                GameObject dd = new GameObject("DontDestroyObj");
                dd.AddComponent<DontDestoyObj>();
                Debug.Log("GameBootstrap: Created DontDestroyObj");
            }
        }

        Debug.Log("=== GameBootstrap: All managers initialized ===");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (!hasBootstrapped)
        {
            Debug.Log("GameBootstrap: Auto-creating via RuntimeInitialize");
            GameObject bootstrap = new GameObject("_GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();
        }
    }
}
