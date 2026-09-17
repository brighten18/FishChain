using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestCamera : MonoBehaviour
{
     [Header("Camera Settings")]
    public float sensitivity = 150f;
    public Transform playerRoot;
    
    [Header("Input Settings")]
    public PlayerInput playerInput;
    public TestMovement parentMovement; // Reference ke parent script

    // Input Actions
    private InputAction cameraControlAction;
    private InputAction cameraResetAction;

    // Camera variables
    private float xRot = 0f;
    private Quaternion initialRotation;
    private Vector2 cameraInput;

    void Start()
    {
        // Store initial rotation
        initialRotation = transform.localRotation;

        // Get parent movement script
        if (parentMovement == null)
        {
            parentMovement = GetComponentInParent<TestMovement>();
            
            if (parentMovement == null)
            {
                Debug.LogError("TestMovement (parent) not found!");
            }
        }

        // Initialize input
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
            
            if (playerInput == null && parentMovement != null)
            {
                // Try to get from parent
                playerInput = parentMovement.playerInput;
            }
            
            // If still null, find in scene
            if (playerInput == null)
            {
                playerInput = FindObjectOfType<PlayerInput>();
            }
        }

        // Setup input actions
        InitializeInputActions();
    }

    void InitializeInputActions()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            // Get input actions
            cameraControlAction = playerInput.actions["CameraControl"];
            cameraResetAction = playerInput.actions["CameraReset"];

            // Enable actions
            cameraControlAction.Enable();
            cameraResetAction.Enable();

            // Subscribe to reset action
            cameraResetAction.performed += OnCameraReset;
        }
        else
        {
            Debug.LogWarning("PlayerInput or Input Actions not found! Using legacy input.");
        }
    }

    void Update()
    {
        // Get camera input
        if (cameraControlAction != null)
        {
            // New Input System
            cameraInput = cameraControlAction.ReadValue<Vector2>();
        }
        else
        {
            // Fallback to legacy input
            cameraInput = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );
        }

        // Apply camera rotation
        ApplyCameraRotation();
    }

    void ApplyCameraRotation()
    {
        float mouseX = cameraInput.x * sensitivity * Time.deltaTime;
        float mouseY = cameraInput.y * sensitivity * Time.deltaTime;

        // Vertical camera rotation (pitch)
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -80f, 80f);
        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);

        // Horizontal rotation applied to PlayerRoot
        if (playerRoot != null)
        {
            playerRoot.Rotate(Vector3.up * mouseX);
        }
        else
        {
            Debug.LogWarning("PlayerRoot is not assigned!");
        }
    }

    void OnCameraReset(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ResetCamera();
        }
    }

    void ResetCamera()
    {
        // Reset vertical rotation
        xRot = 0f;
        transform.localRotation = initialRotation;

        // Reset player root rotation if needed
        if (playerRoot != null)
        {
            playerRoot.rotation = Quaternion.Euler(0f, playerRoot.eulerAngles.y, 0f);
        }

        Debug.Log("Camera reset to default position");
    }

    // Public methods for accessing camera state
    public Vector2 GetCameraInput()
    {
        return cameraInput;
    }

    public float GetXRotation()
    {
        return xRot;
    }

    public bool IsCameraMoving()
    {
        return cameraInput.magnitude > 0.01f;
    }

    // Method to get parent movement info
    public float GetParentSpeed()
    {
        if (parentMovement != null)
        {
            return parentMovement.GetCurrentSpeed();
        }
        return 0f;
    }

    public Vector3 GetParentVelocity()
    {
        if (parentMovement != null)
        {
            return parentMovement.GetCurrentVelocity();
        }
        return Vector3.zero;
    }

    // Cleanup
    void OnDestroy()
    {
        if (cameraResetAction != null)
        {
            cameraResetAction.performed -= OnCameraReset;
        }
    }
}
