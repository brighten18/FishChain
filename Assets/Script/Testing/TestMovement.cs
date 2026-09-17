using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
public class TestMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveForce = 40f;
    public float maxSpeed = 10f;
    public float dashSpeed = 25f;
    public Transform cameraHolder;
    
    [Header("Dash Settings")]
    public float dashDuration = 0.5f;
    public float dashCooldown = 1.5f;
    public float dashForceMultiplier = 3f;
    
    [Header("Input Settings")]
    public PlayerInput playerInput;
    
    // Components
    private Rigidbody rb;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction hoverUpAction;
    private InputAction hoverDownAction;
    private InputAction sprintAction;
    
    // State
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private float currentMaxSpeed;
    private bool canDash = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        rb.useGravity = false;
        rb.drag = 0.5f;
        rb.angularDrag = 0f;
    }

    private InputActionAsset FindInputActionAsset()
{
    InputActionAsset asset = Resources.Load<InputActionAsset>("PlayerControl/PlayerInput");
    
    if (asset != null)
    {
        Debug.Log($"PauseManager: Loaded Input Action Asset from Resources: {asset.name}");
        return asset;
    }
    
    InputActionAsset[] allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
    
    if (allAssets.Length > 0)
    {
        Debug.Log($"PauseManager: Using first available Input Action Asset: {allAssets[0].name}");
        return allAssets[0];
    }
    
    return null;
}


    void Start()
    {
        currentMaxSpeed = maxSpeed;
        InitializeInput();
    }

    void InitializeInput()
    {
        // Cari PlayerInput jika belum di-assign
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = FindObjectOfType<PlayerInput>();
                if (playerInput == null)
                {
                    Debug.LogWarning("PlayerInput not found. Using legacy input only.");
                    return;
                }
            }
        }

        // Dapatkan actions dari Input Action Asset
        if (playerInput.actions != null)
        {
            // Get actions
            moveAction = playerInput.actions.FindAction("Move");
            hoverUpAction = playerInput.actions.FindAction("HoverUP");
            hoverDownAction = playerInput.actions.FindAction("HoverDown");
            sprintAction = playerInput.actions.FindAction("SwimFast");
            
            // Enable actions
            if (moveAction != null) moveAction.Enable();
            if (hoverUpAction != null) hoverUpAction.Enable();
            if (hoverDownAction != null) hoverDownAction.Enable();
            if (sprintAction != null) sprintAction.Enable();
            
            // Setup dash input
            if (sprintAction != null)
            {
                sprintAction.performed += OnDashPerformed;
            }
            
            Debug.Log("Input Actions initialized successfully");
        }
        else
        {
            Debug.LogWarning("Input Actions asset not found in PlayerInput!");
        }
    }

    void Update()
    {
        if (rb == null) return;
        
        // Update dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) StopDash();
        }
        
        // Update cooldown
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        
        // Debug input (bisa dihapus)
        if (moveAction != null && moveAction.IsPressed())
        {
            // Debug.Log("Move input: " + moveAction.ReadValue<Vector2>());
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        
        // Get movement input
        Vector2 moveInput = GetMoveInput();
        float verticalInput = GetVerticalInput();
        
        // Calculate direction relative to camera
        Vector3 forward = cameraHolder.forward;
        Vector3 right = cameraHolder.right;
        Vector3 up = cameraHolder.up;
        
        Vector3 dir = (forward * moveInput.y) + 
                     (right * moveInput.x) + 
                     (up * verticalInput);
        
        if (dir.magnitude > 0.1f) dir.Normalize();
        
        // Apply force with dash multiplier
        float force = moveForce * (isDashing ? dashForceMultiplier : 1f);
        rb.AddForce(dir * force, ForceMode.Acceleration);
        
        // Update max speed
        currentMaxSpeed = isDashing ? dashSpeed : maxSpeed;
        
        // Speed cap
        if (rb.velocity.magnitude > currentMaxSpeed)
        {
            rb.velocity = rb.velocity.normalized * currentMaxSpeed;
        }
        FindInputActionAsset();
    }

    // INPUT GETTERS
    Vector2 GetMoveInput()
    {
        // Prioritize Input Action System
        if (moveAction != null)
        {
            return moveAction.ReadValue<Vector2>();
        }
        
        // Fallback to legacy input
        return new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
    }

    float GetVerticalInput()
    {
        float vertical = 0f;
        
        // Prioritize Input Action System
        if (hoverUpAction != null && hoverUpAction.IsPressed()) vertical = 1f;
        if (hoverDownAction != null && hoverDownAction.IsPressed()) vertical = -1f;
        
        // Fallback to legacy input
        if (vertical == 0f)
        {
            if (Input.GetKey(KeyCode.Space)) vertical = 1f;
            if (Input.GetKey(KeyCode.LeftControl)) vertical = -1f;
        }
        
        return vertical;
    }

    // DASH SYSTEM
    void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (canDash && cooldownTimer <= 0 && !isDashing)
        {
            StartDash();
        }
    }

    void StartDash()
    {
        if (rb == null) return;
        
        isDashing = true;
        dashTimer = dashDuration;
        canDash = false;
        
        // Get current movement direction
        Vector2 moveInput = GetMoveInput();
        float verticalInput = GetVerticalInput();
        
        Vector3 forward = cameraHolder.forward;
        Vector3 right = cameraHolder.right;
        Vector3 up = cameraHolder.up;
        
        Vector3 dashDir = (forward * moveInput.y) + 
                         (right * moveInput.x) + 
                         (up * verticalInput);
        
        // Jika tidak ada input, dash ke depan
        if (dashDir.magnitude < 0.1f) dashDir = forward;
        
        dashDir.Normalize();
        
        // Apply dash force
        rb.AddForce(dashDir * moveForce * 5f, ForceMode.Impulse);
        
        Debug.Log("DASH ACTIVATED!");
    }

    void StopDash()
    {
        isDashing = false;
        cooldownTimer = dashCooldown;
        
        // Reset dash setelah cooldown
        Invoke(nameof(ResetDash), dashCooldown);
    }

    void ResetDash()
    {
        canDash = true;
        Debug.Log("Dash ready!");
    }

    // PUBLIC METHODS (untuk kompatibilitas)
    public Vector3 GetCurrentVelocity() => rb != null ? rb.velocity : Vector3.zero;
    public float GetCurrentSpeed() => rb != null ? rb.velocity.magnitude : 0f;
    public bool IsMoving() => rb != null && rb.velocity.magnitude > 0.1f;
    public bool IsDashing() => isDashing;
    public bool CanDash() => canDash && cooldownTimer <= 0;
    
    public Vector2 GetMoveInputPublic() => GetMoveInput();
    public float GetVerticalInputPublic() => GetVerticalInput();

    void OnDestroy()
    {
        // Cleanup event listeners
        if (sprintAction != null)
        {
            sprintAction.performed -= OnDashPerformed;
        }
    }
}
