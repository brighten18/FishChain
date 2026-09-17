using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishFoodMovement : MonoBehaviour
{
        [Header("Circular Movement Settings")]
    [Tooltip("Pusat rotasi (kosongkan untuk menggunakan posisi objek ini)")]
    public Transform centerPoint;
    
    [Tooltip("Radius pergerakan melingkar")]
    public float radius = 5f;
    
    [Tooltip("Kecepatan rotasi (derajat per detik)")]
    public float rotationSpeed = 30f;
    
    [Tooltip("Tinggi gelombang (untuk efek berenang naik turun)")]
    public float waveHeight = 0.5f;
    
    [Tooltip("Frekuensi gelombang")]
    public float waveFrequency = 1f;
    
    [Header("Forward Direction Settings")]
    [Tooltip("Arah depan objek (orientasi awal)")]
    public ForwardDirection forwardDirection = ForwardDirection.Z;
    
    [Tooltip("Kustom arah depan (jika menggunakan Custom)")]
    public Vector3 customForwardDirection = Vector3.forward;
    
    [Tooltip("Kustom arah atas (up)")]
    public Vector3 customUpDirection = Vector3.up;
    
    [Header("Rotation Settings")]
    [Tooltip("Apakah ikan menghadap arah gerakan?")]
    public bool faceMovementDirection = true;
    
    [Tooltip("Kecepatan rotasi menghadap arah gerakan")]
    public float rotationDamping = 5f;
    
    [Header("Auto Start Settings")]
    [Tooltip("Mulai bergerak otomatis saat Start")]
    public bool autoStart = true;
    
    [Tooltip("Delay sebelum mulai bergerak")]
    public float startDelay = 0f;
    
    [Header("Visualization")]
    [Tooltip("Tampilkan gizmos untuk debug")]
    public bool showGizmos = true;
    
    [Tooltip("Warna untuk panah arah depan")]
    public Color forwardArrowColor = Color.green;
    
    [Tooltip("Warna untuk panah arah atas")]
    public Color upArrowColor = Color.red;
    
    // Enum untuk pilihan arah depan
    public enum ForwardDirection
    {
        Z,     // Forward = (0, 0, 1)
        X,     // Forward = (1, 0, 0)
        Y,     // Forward = (0, 1, 0)
        NegZ,  // Forward = (0, 0, -1)
        NegX,  // Forward = (-1, 0, 0)
        NegY,  // Forward = (0, -1, 0)
        Custom // Gunakan customForwardDirection
    }
    
    // Variabel privat
    private float currentAngle = 0f;
    private float yOffset = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isMoving = false;
    private Vector3 currentForward = Vector3.forward;
    private Vector3 currentUp = Vector3.up;
    
    void Start()
    {
        // Simpan posisi dan rotasi awal
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Jika centerPoint tidak diassign, gunakan posisi objek ini
        if (centerPoint == null)
        {
            GameObject centerObj = new GameObject("CircleCenter_" + gameObject.name);
            centerPoint = centerObj.transform;
            centerPoint.position = transform.position;
        }
        
        // Setup arah depan berdasarkan pilihan
        SetupForwardDirection();
        
        // Mulai pergerakan jika autoStart
        if (autoStart)
        {
            StartMovement(startDelay);
        }
    }
    
    void Update()
    {
        if (isMoving)
        {
            MoveInCircle();
        }
    }
    
    /// <summary>
    /// Setup arah depan berdasarkan pilihan di Inspector
    /// </summary>
    private void SetupForwardDirection()
    {
        // Tentukan arah depan berdasarkan enum
        switch (forwardDirection)
        {
            case ForwardDirection.Z:
                currentForward = Vector3.forward;
                currentUp = Vector3.up;
                break;
                
            case ForwardDirection.X:
                currentForward = Vector3.right;
                currentUp = Vector3.up;
                break;
                
            case ForwardDirection.Y:
                currentForward = Vector3.up;
                currentUp = Vector3.forward;
                break;
                
            case ForwardDirection.NegZ:
                currentForward = Vector3.back;
                currentUp = Vector3.up;
                break;
                
            case ForwardDirection.NegX:
                currentForward = Vector3.left;
                currentUp = Vector3.up;
                break;
                
            case ForwardDirection.NegY:
                currentForward = Vector3.down;
                currentUp = Vector3.forward;
                break;
                
            case ForwardDirection.Custom:
                currentForward = customForwardDirection.normalized;
                currentUp = customUpDirection.normalized;
                break;
        }
        
        Debug.Log($"Forward direction set to: {currentForward}, Up: {currentUp}");
    }
    
    /// <summary>
    /// Ubah arah depan secara runtime
    /// </summary>
    public void ChangeForwardDirection(ForwardDirection newDirection, float transitionTime = 1f)
    {
        StartCoroutine(ChangeForwardDirectionRoutine(newDirection, transitionTime));
    }
    
    private IEnumerator ChangeForwardDirectionRoutine(ForwardDirection newDirection, float transitionTime)
    {
        Vector3 oldForward = currentForward;
        Vector3 oldUp = currentUp;
        Vector3 newForward = Vector3.forward;
        Vector3 newUp = Vector3.up;
        
        // Tentukan arah baru
        switch (newDirection)
        {
            case ForwardDirection.Z:
                newForward = Vector3.forward;
                newUp = Vector3.up;
                break;
            case ForwardDirection.X:
                newForward = Vector3.right;
                newUp = Vector3.up;
                break;
            case ForwardDirection.Y:
                newForward = Vector3.up;
                newUp = Vector3.forward;
                break;
            case ForwardDirection.NegZ:
                newForward = Vector3.back;
                newUp = Vector3.up;
                break;
            case ForwardDirection.NegX:
                newForward = Vector3.left;
                newUp = Vector3.up;
                break;
            case ForwardDirection.NegY:
                newForward = Vector3.down;
                newUp = Vector3.forward;
                break;
        }
        
        // Transisi bertahap
        float elapsedTime = 0f;
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            
            currentForward = Vector3.Slerp(oldForward, newForward, t);
            currentUp = Vector3.Slerp(oldUp, newUp, t);
            
            yield return null;
        }
        
        currentForward = newForward;
        currentUp = newUp;
        forwardDirection = newDirection;
    }
    
    /// <summary>
    /// Atur arah depan custom secara runtime
    /// </summary>
    public void SetCustomForwardDirection(Vector3 forward, Vector3 up, float transitionTime = 1f)
    {
        StartCoroutine(SetCustomForwardDirectionRoutine(forward, up, transitionTime));
    }
    
    private IEnumerator SetCustomForwardDirectionRoutine(Vector3 forward, Vector3 up, float transitionTime)
    {
        Vector3 oldForward = currentForward;
        Vector3 oldUp = currentUp;
        
        float elapsedTime = 0f;
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            
            currentForward = Vector3.Slerp(oldForward, forward.normalized, t);
            currentUp = Vector3.Slerp(oldUp, up.normalized, t);
            
            yield return null;
        }
        
        currentForward = forward.normalized;
        currentUp = up.normalized;
        forwardDirection = ForwardDirection.Custom;
        customForwardDirection = forward;
        customUpDirection = up;
    }
    
    /// <summary>
    /// Dapatkan vektor arah depan saat ini
    /// </summary>
    public Vector3 GetCurrentForward()
    {
        return currentForward;
    }
    
    /// <summary>
    /// Dapatkan vektor arah atas saat ini
    /// </summary>
    public Vector3 GetCurrentUp()
    {
        return currentUp;
    }

    public void StartMovement(float delay = 0f)
    {
        StartCoroutine(StartMovementRoutine(delay));
    }
    
    private IEnumerator StartMovementRoutine(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        
        isMoving = true;
        
        // Inisialisasi posisi acak di lingkaran
        currentAngle = Random.Range(0f, 360f);
        
        Debug.Log($"Fish {gameObject.name} started circular movement with radius {radius}");
    }

    public void StopMovement()
    {
        isMoving = false;
    }

    private void MoveInCircle()
    {
        // Update sudut
        currentAngle += rotationSpeed * Time.deltaTime;
        
        // Normalisasi sudut
        if (currentAngle > 360f) currentAngle -= 360f;
        if (currentAngle < 0f) currentAngle += 360f;
        
        // Hitung posisi baru di lingkaran
        float radianAngle = currentAngle * Mathf.Deg2Rad;
        
        // Posisi X dan Z berdasarkan lingkaran
        float x = Mathf.Cos(radianAngle) * radius;
        float z = Mathf.Sin(radianAngle) * radius;
        
        // Efek gelombang naik turun
        yOffset = Mathf.Sin(Time.time * waveFrequency) * waveHeight;
        
        // Hitung posisi akhir
        Vector3 targetPosition = centerPoint.position + new Vector3(x, yOffset, z);
        
        // Apply posisi
        transform.position = targetPosition;
        
        // Rotasi menghadap arah gerakan
        if (faceMovementDirection)
        {
            // Hitung arah gerakan di dunia (global)
            Vector3 movementDirection = new Vector3(
                -Mathf.Sin(radianAngle),  // Turunan dari cos adalah -sin
                0,
                Mathf.Cos(radianAngle)     // Turunan dari sin adalah cos
            ).normalized;
            
            // Jika ada gelombang, tambahkan sedikit rotasi vertikal
            if (waveHeight > 0)
            {
                float verticalInfluence = Mathf.Cos(Time.time * waveFrequency) * 0.3f;
                movementDirection.y = verticalInfluence;
                movementDirection.Normalize();
            }
            
            // Rotasi ikan menghadap arah gerakan
            if (movementDirection != Vector3.zero)
            {
                // Buat rotasi target dengan arah depan yang telah diset
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, currentUp);
                
                // Jika arah depan bukan Vector3.forward, kita perlu adjust rotasi
                if (currentForward != Vector3.forward)
                {
                    // Hitung offset rotasi dari forward default ke forward custom
                    Quaternion forwardOffset = Quaternion.FromToRotation(Vector3.forward, currentForward);
                    targetRotation = targetRotation * forwardOffset;
                }
                
                // Smooth rotation
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationDamping * Time.deltaTime
                );
            }
        }
        else
        {
            // Jika tidak menghadap arah gerakan, gunakan rotasi berdasarkan arah depan yang ditentukan
            Quaternion baseRotation = Quaternion.LookRotation(currentForward, currentUp);
            transform.rotation = baseRotation;
        }
    }

    public void ChangeRadius(float newRadius, float duration = 1f)
    {
        StartCoroutine(ChangeRadiusRoutine(newRadius, duration));
    }
    
    private IEnumerator ChangeRadiusRoutine(float newRadius, float duration)
    {
        float startRadius = radius;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            radius = Mathf.Lerp(startRadius, newRadius, t);
            yield return null;
        }
        
        radius = newRadius;
    }
    
    public void ChangeSpeed(float newSpeed, float duration = 1f)
    {
        StartCoroutine(ChangeSpeedRoutine(newSpeed, duration));
    }
    
    private IEnumerator ChangeSpeedRoutine(float newSpeed, float duration)
    {
        float startSpeed = rotationSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            rotationSpeed = Mathf.Lerp(startSpeed, newSpeed, t);
            yield return null;
        }
        
        rotationSpeed = newSpeed;
    }
    
    public void SetCenterPoint(Transform newCenter)
    {
        centerPoint = newCenter;
    }
    
    public Vector3 GetCenterPosition()
    {
        return centerPoint.position;
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        currentAngle = 0f;
        
        // Reset arah depan ke setting awal
        SetupForwardDirection();
    }
    
    /// <summary>
    /// Debug visualization untuk arah depan dan atas
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
        Vector3 position = transform.position;
        
        // Gambar lingkaran radius
        Gizmos.color = Color.cyan;
        const int segments = 36;
        float anglePerSegment = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * anglePerSegment * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        
        // Gambar garis ke pusat
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, center);
        
        // Gambar arah depan (hijau)
        Gizmos.color = forwardArrowColor;
        Vector3 forwardDir = transform.TransformDirection(currentForward);
        Gizmos.DrawRay(position, forwardDir * 2f);
        
        // Gambar arah atas (merah)
        Gizmos.color = upArrowColor;
        Vector3 upDir = transform.TransformDirection(currentUp);
        Gizmos.DrawRay(position, upDir * 1.5f);
        
        // Gambar label arah
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(position + Vector3.up * 0.3f, $"Fwd: {currentForward}");
        UnityEditor.Handles.Label(position + Vector3.up * 0.1f, $"Up: {currentUp}");
        #endif
    }
}
