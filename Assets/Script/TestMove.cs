using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class TestMove : MonoBehaviour
{
   [Header("Movement Settings")]
    [SerializeField] private float swimSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float accelerationTime = 0.3f;
    [SerializeField] private float hoverSpeed = 3f;
    [SerializeField] private float hoverAcceleration = 2f;
    [SerializeField] private float currentHoverVelocity;
    
    [Header("Camera Settings")]
    public CinemachineVirtualCamera PlayerPOV;
    private float normalFOV = 40f;
    private float SwimFastFOV =50f;
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float cameraSmoothing = 5f;
    
    [Header("Camera Reset Settings")]
    [SerializeField] private float cameraResetSpeed = 5f;
    
    [Header("Debug Info")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private bool isResettingToCamera;
    
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction cameraControlAction;
    private InputAction sprintAction;
    private InputAction cameraResetAction;
    private InputAction GoUp;
    private InputAction GoDown;
    private float targetHoverSpeed;
    
    private Vector2 moveInput;
    private Vector2 cameraInput;
    private bool isSprinting;
    private float currentVelocity;
    private bool GoUpAction;
    private bool GoDownAction;
    
    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;
    
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        
        moveAction = playerInput.actions["Move"];
        cameraControlAction = playerInput.actions["CameraControl"];
        sprintAction = playerInput.actions["SwimFast"];
        cameraResetAction = playerInput.actions["CameraReset"];
        GoUp = playerInput.actions["HoverUP"];
        GoDown = playerInput.actions["HoverDown"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Start()
    {
        if (cameraRig != null)
        {
            currentYaw = cameraRig.eulerAngles.y;
        }
        
        if (cameraPivot != null)
        {
            currentPitch = cameraPivot.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;
        }

        if (PlayerPOV != null)
        { 
            PlayerPOV.m_Lens.FieldOfView = normalFOV;    
        }
    }
    
    void Update()
    {
        ReadInput();
        HandleCameraReset();
        HandleCameraRotation();
        HandleFishRotation();
        HandleMovement();
        HandleHover();
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        cameraInput = cameraControlAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();
        isResettingToCamera = cameraResetAction.IsPressed();
        GoUpAction = GoUp.IsPressed();
        GoDownAction = GoDown.IsPressed();
    }
    
    void HandleCameraRotation()
    {
        if (isResettingToCamera) return;
        
        targetYaw += cameraInput.x * mouseSensitivity;
        targetPitch -= cameraInput.y * mouseSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, cameraSmoothing * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, cameraSmoothing * Time.deltaTime);
        
        if (cameraRig != null)
        {
            cameraRig.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }
        
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }
    
    void HandleCameraReset()
    {
        if (!isResettingToCamera || cameraRig == null) return;
        
        Vector3 cameraForward = cameraRig.forward;
        cameraForward.y = 0f;
        
        if (cameraForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                cameraResetSpeed * Time.deltaTime
            );
        }
    }
    
    void HandleFishRotation()
    {
        if (isResettingToCamera) return;
        
        float turnInput = moveInput.x;
        
        float rotationAmount = turnInput * rotationSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationAmount,0f, Space.Self);
    }
    
    void HandleMovement()
    {
        float forwardInput = moveInput.y;
        
        float targetSpeed = isSprinting ? swimSpeed * sprintMultiplier : swimSpeed;
        targetSpeed *= forwardInput;
        
        currentSpeed = Mathf.SmoothDamp(
            currentSpeed,
            targetSpeed,
            ref currentVelocity,
            accelerationTime
        );
        
        Vector3 moveDirection = transform.right;
        transform.position += moveDirection * currentSpeed * Time.deltaTime;

        if (isSprinting)
        {
            PlayerPOV.m_Lens.FieldOfView = Mathf.Lerp(PlayerPOV.m_Lens.FieldOfView, SwimFastFOV, Time.deltaTime);
        } else
        {
            PlayerPOV.m_Lens.FieldOfView = Mathf.Lerp(PlayerPOV.m_Lens.FieldOfView, normalFOV, Time.deltaTime);
        }
    }

    void HandleHover()
{
    // Reset target hover speed
    targetHoverSpeed = 0f;
    
    // Set target berdasarkan input
    if (GoUpAction && !GoDownAction)
    {
        targetHoverSpeed = hoverSpeed;
    }
    else if (GoDownAction && !GoUpAction)
    {
        targetHoverSpeed = -hoverSpeed;
    }
    
    // Smooth movement
    currentHoverVelocity = Mathf.SmoothDamp(
        currentHoverVelocity,
        targetHoverSpeed,
        ref currentHoverVelocity, // Anda perlu tambahkan variable ini
        hoverAcceleration * Time.deltaTime
    );
    
    // Apply hover movement
    Vector3 hoverMovement = Vector3.up * currentHoverVelocity * Time.deltaTime;
    transform.localPosition += hoverMovement;
}
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 2f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 1.5f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}
