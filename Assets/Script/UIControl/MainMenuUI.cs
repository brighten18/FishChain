using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    public Text startText;
    public Text quitText;

    int selectedIndex = 0;
    float inputCooldown = 0.25f;
    float lastInputTime = 0f;

    void Update()
    {
        HandleNavigation();
        HandleConfirm();
    }

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");

        if (Time.time - lastInputTime > inputCooldown)
        {
            if (v > 0.5f)   // Up (W / Stick Up)
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = 1;
                lastInputTime = Time.time;
            }
            else if (v < -0.5f) // Down (S / Stick Down)
            {
                selectedIndex++;
                if (selectedIndex > 1) selectedIndex = 0;
                lastInputTime = Time.time;
            }
        }
    }

    void HandleConfirm()
    {
        if (Input.GetButtonDown("Submit"))
        {
            if (selectedIndex == 0)     // START
            {
                SceneManager.LoadScene("MainLevel"); 
            }
            else if (selectedIndex == 1) // QUIT
            {
                Application.Quit();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
        }
    }

    public void MulaiGame()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void KeluarGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
