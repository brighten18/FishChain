using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class SimpleAfterGame : MonoBehaviour
{
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private float delay = 0.5f;
    
    private bool _isLoading = false;
    
    void Update()
    {
        if (_isLoading) return;
        
        // Accept multiple input types
        if (Input.anyKeyDown || 
            (Input.GetMouseButtonDown(0)) ||
            (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame))
        {
            StartCoroutine(LoadHomeCoroutine());
        }
    }
    
    System.Collections.IEnumerator LoadHomeCoroutine()
    {
        _isLoading = true;
        
        // Optional: Play sound/effect here
        Debug.Log("Loading home scene...");
        
        yield return new WaitForSeconds(delay);
        
        // Reset game state
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        SceneManager.LoadScene(homeSceneName);
    }
    
    // Called from UI button
    public void LoadHome()
    {
        if (!_isLoading)
        {
            StartCoroutine(LoadHomeCoroutine());
        }
        SceneManager.LoadScene(homeSceneName);
    }
}