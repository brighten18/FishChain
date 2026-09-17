using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.PlayerLoop;

public class FishCam : MonoBehaviour
{
   [Header("Fish Components")] 
   public CinemachineVirtualCamera PlayerPOV;
   public float normalFOV = 40f;
   public float swimFastFOV = 55f;
   public GameObject FishModel;
   public Transform titikB;
   public Transform ModelA;
   
   [Header("Fish Controls Imput System")]
   private PlayerInput inputManager;
   private InputAction Move;
   private InputAction SwimFast;
   private InputAction CameraControl;

   [Header("Fish Status")]
   public bool isSwimmingFast;
   public float SwimSpeed = 3f;
   public float sprintMultiplier = 2f;
   public float MouseSens = 2f;
   public float TurnSpeed = 2f;
   public Transform CameraPivot;

   //local data
   private Transform T;   
   private Rigidbody rb; 
   private float baseSwimSpeed;
   private Rigidbody RbChildren;



   //testing
   [Header("Camera References")]
    public Transform cameraPivot;
    public Transform yawTransform;
    
    [Header("Rotation Settings")]
    public float mouseSensitivity = 2f;
    public float rotationSmoothness = 5f;
    public float maxPitch = 80f;
    public float minPitch = -80f;
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;

   // Start is called before the first frame update
    void Awake()
    {
        // rb = GetComponent<Rigidbody>();
        inputManager = GetComponent<PlayerInput>();
        RbChildren = GetComponentInChildren<Rigidbody>();
        rb =  GetComponent <Rigidbody>();
        Move = inputManager.actions["Move"];
        SwimFast = inputManager.actions["SwimFast"];
        CameraControl = inputManager.actions["CameraControl"];
    
    } 
    void Start() 
    { 
        if (PlayerPOV != null)
        { 
            PlayerPOV.m_Lens.FieldOfView = normalFOV;    
        }

        baseSwimSpeed = SwimSpeed;
        Cursor.lockState = CursorLockMode.Locked;

        T = this.transform;
    } 
    
    void Update()
    {
        SwimFastPerform();
        MovementChar();
        CamControl();

        //Testing
        Vector3 direction = (titikB.position - ModelA.position).normalized;
        float distance = Vector3.Distance(ModelA.position, titikB.position);

        Debug.DrawRay(ModelA.position, direction * distance, Color.red);
    }

    private void MovementChar()
    {
        Vector3 movedirection =  Move.ReadValue<Vector2>();
        RbChildren.transform.Translate(movedirection.y * SwimSpeed * Time.deltaTime,0,movedirection.x * SwimSpeed * Time.deltaTime *-1, Space.World);
        
    }

    private void CamControl()
    {
        Vector2 SeeTarget = CameraControl.ReadValue<Vector2>();
    
    // 🎯 DEBUG: Check input values
    Debug.Log($"Input - X: {SeeTarget.x}, Y: {SeeTarget.y}");
    
    // Accumulate target rotation
    targetYaw += SeeTarget.x * mouseSensitivity;
    targetPitch -= SeeTarget.y * mouseSensitivity; // Minus untuk natural feel
    
    // Clamp pitch
    targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    
    // Smooth rotation
    currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationSmoothness * Time.deltaTime);
    currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationSmoothness * Time.deltaTime);
    
    // 🎯 DEBUG: Check rotation values before apply
    Debug.Log($"Before Apply - Yaw: {currentYaw}, Pitch: {currentPitch}");
    
    // Apply rotations
    if (yawTransform != null)
    {
        yawTransform.rotation = Quaternion.Euler(0, currentYaw, 0);
        Debug.Log($"Yaw Transform Rotation: {yawTransform.eulerAngles}");
    }
    else
    {
        Debug.LogError("yawTransform is NULL!");
    }
        
    if (cameraPivot != null)
    {
        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        Debug.Log($"Camera Pivot Local Rotation: {cameraPivot.localEulerAngles}");
    }
    else
    {
        Debug.LogError("cameraPivot is NULL!");
    }
    
    Debug.Log($"Final - Yaw: {currentYaw}, Pitch: {currentPitch}");


        // Vector2 SeeTarget = CameraControl.ReadValue<Vector2>();

        // float yaw = SeeTarget.x * MouseSens;
        // float pitch = -SeeTarget.y * MouseSens;     // minus agar natural  // ★ CHANGED

        // yaw = Mathf.Clamp(yaw, -50f, 50f);
        // pitch = Mathf.Clamp(pitch, -50f, 50f);

        // T.localRotation = Quaternion.Euler(pitch, yaw, 0);

        // // // Rotasi seluruh ikan
        // // transform.Rotate(0, yaw, 0);     

        // // // Batasi pitch kamera
        // CameraPivot.localRotation *= Quaternion.Euler(0, yaw, 0);
        // CameraPivot.localRotation *= Quaternion.Euler(pitch, 0, 0);

        // Debug.Log(yaw + " | " + pitch);
    }

    private void SwimFastPerform()
    {
        isSwimmingFast = SwimFast.ReadValue<float>() > 0;
    
        if (isSwimmingFast)
        {
            PlayerPOV.m_Lens.FieldOfView = Mathf.Lerp(PlayerPOV.m_Lens.FieldOfView, swimFastFOV, Time.deltaTime);
            SwimSpeed = baseSwimSpeed * sprintMultiplier;
        }
        else
        {
            PlayerPOV.m_Lens.FieldOfView = Mathf.Lerp(PlayerPOV.m_Lens.FieldOfView, normalFOV, Time.deltaTime);
            SwimSpeed = baseSwimSpeed;
        }
    }
}