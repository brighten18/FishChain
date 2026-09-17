using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSchool : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject fishPrefab;
    [Range(1, 100)] public int fishCount = 20;
    public Vector3 areaSize = new Vector3(20, 5, 20);
    
    [Header("Movement Settings")]
    [Range(0.1f, 20f)] public float speed = 2f;
    [Range(0.1f, 20f)] public float rotationSpeed = 2f;
    [Range(0.1f, 10f)] public float neighborRange = 3f;
    
    [Header("Forward Direction")]
    public ForwardAxis forwardAxis = ForwardAxis.Z;
    
    [Header("Circle Movement")]
    public bool useCircularMovement = true;
    [Range(1f, 20f)] public float circleRadius = 10f;
    
    // =========== TAMBAHAN UNTUK ANT MILL + GELOMBANG ===========
    [Header("Ant Mill Settings")]
    [Range(0.5f, 5f)] public float millRotationSpeed = 1f;
    [Range(0f, 3f)] public float waveAmplitude = 1f;
    [Range(0.1f, 4f)] public float waveFrequency = 1f;
    public bool useWaveMovement = true;
    
    // =========== TAMBAHAN: KONTROL ARAH PUTARAN ===========
    [Header("Rotation Direction")]
    public RotationDirection rotationDirection = RotationDirection.Clockwise;
    public enum RotationDirection { Clockwise, CounterClockwise }
    // ==========================================================
    
    // Enum untuk arah depan
    public enum ForwardAxis { X, Y, Z, NegX, NegY, NegZ }
    
    // Private
    private List<Transform> fish = new List<Transform>();
    private Vector3 center;
    private Vector3 currentForward;
    
    // =========== TAMBAHAN UNTUK ANT MILL + GELOMBANG ===========
    private List<float> fishAngles = new List<float>();
    private List<float> fishPhases = new List<float>();
    private float timeCounter = 0f;
    // ==========================================================
    
    void Start()
    {
        center = transform.position;
        SetForwardDirection();
        SpawnFish();
        StartCoroutine(UpdateFlock());
    }
    
    void SetForwardDirection()
    {
        switch (forwardAxis)
        {
            case ForwardAxis.X: currentForward = Vector3.right; break;
            case ForwardAxis.Y: currentForward = Vector3.up; break;
            case ForwardAxis.Z: currentForward = Vector3.forward; break;
            case ForwardAxis.NegX: currentForward = Vector3.left; break;
            case ForwardAxis.NegY: currentForward = Vector3.down; break;
            case ForwardAxis.NegZ: currentForward = Vector3.back; break;
        }
    }
    
    void SpawnFish()
    {
        // Clear existing fish
        foreach (Transform child in transform)
            if (child.name.Contains("Fish"))
                Destroy(child.gameObject);
        
        fish.Clear();
        fishAngles.Clear();
        fishPhases.Clear();
        
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 pos = center + new Vector3(
                Random.Range(-areaSize.x, areaSize.x),
                Random.Range(-areaSize.y, areaSize.y),
                Random.Range(-areaSize.z, areaSize.z)
            );
            
            GameObject f = Instantiate(fishPrefab, pos, GetInitialRotation(), transform);
            f.name = "Fish_" + i;
            fish.Add(f.transform);
            
            // Set angle awal untuk setiap ikan dalam lingkaran
            // =========== PERUBAHAN: Sesuaikan dengan arah putaran ===========
            float angle = (360f / fishCount) * i;
            fishAngles.Add(angle);
            
            // Set phase awal untuk gelombang
            float phase = Random.Range(0f, 360f);
            fishPhases.Add(phase);
        }
    }
    
    Quaternion GetInitialRotation()
    {
        if (currentForward != Vector3.forward)
        {
            Vector3 up = (currentForward == Vector3.up || currentForward == Vector3.down) 
                ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(currentForward, up);
        }
        return Random.rotation;
    }
    
    IEnumerator UpdateFlock()
    {
        WaitForSeconds wait = new WaitForSeconds(0.05f);
        
        while (true)
        {
            timeCounter += Time.deltaTime;
            
            for (int i = 0; i < fish.Count; i++)
            {
                var f = fish[i];
                if (f == null) continue;
                
                // =========== ANT MILL MOVEMENT ===========
                if (useCircularMovement)
                {
                    // 1. Update angle untuk gerakan melingkar (Ant Mill)
                    // =========== PERUBAHAN: Tentukan arah putaran ===========
                    float rotationDirectionMultiplier = (rotationDirection == RotationDirection.Clockwise) ? 1f : -1f;
                    fishAngles[i] += (millRotationSpeed * rotationDirectionMultiplier * Time.deltaTime * 50f);
                    
                    // Normalize angle ke range 0-360
                    fishAngles[i] = Mathf.Repeat(fishAngles[i], 360f);
                    
                    // 2. Hitung posisi dasar di lingkaran
                    float angleRad = fishAngles[i] * Mathf.Deg2Rad;
                    Vector3 circlePos = new Vector3(
                        Mathf.Cos(angleRad) * circleRadius,
                        0f,
                        Mathf.Sin(angleRad) * circleRadius
                    );
                    
                    // 3. Tambahkan gelombang jika diaktifkan
                    if (useWaveMovement && waveAmplitude > 0)
                    {
                        float waveOffset = Mathf.Sin((timeCounter * waveFrequency) + fishPhases[i]) * waveAmplitude;
                        circlePos.y = waveOffset;
                    }
                    
                    // 4. Pindahkan ikan ke posisi lingkaran + pusat
                    Vector3 targetPosition = center + circlePos;
                    
                    // 5. Hitung arah tangensial untuk rotasi
                    // =========== PERUBAHAN: Balik arah tangensial untuk counter-clockwise ===========
                    Vector3 toCenter = (center - targetPosition).normalized;
                    Vector3 tangent = Vector3.Cross(toCenter, Vector3.up);
                    
                    // Jika counter-clockwise, balik arah tangensial
                    if (rotationDirection == RotationDirection.CounterClockwise)
                    {
                        tangent = -tangent;
                    }
                    
                    // 6. Apply movement dengan smoothing
                    f.position = Vector3.Lerp(f.position, targetPosition, speed * Time.deltaTime);
                    
                    // 7. Rotasi untuk menghadap arah gerakan
                    if (tangent != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(tangent, Vector3.up);
                        
                        if (currentForward != Vector3.forward)
                        {
                            Quaternion offset = Quaternion.FromToRotation(Vector3.forward, currentForward);
                            targetRot = targetRot * offset;
                        }
                        
                        f.rotation = Quaternion.Slerp(f.rotation, targetRot, rotationSpeed * Time.deltaTime);
                    }
                    
                    continue;
                }
                // ===========================================
                
                // KODE ASLI (untuk non-circular movement)
                // ... (kode flocking asli tetap sama) ...
                // 1. Find neighbors
                Vector3 avgPos = Vector3.zero;
                Vector3 avgDir = Vector3.zero;
                int count = 0;
                
                foreach (var other in fish)
                {
                    if (other == f) continue;
                    
                    float dist = Vector3.Distance(f.position, other.position);
                    if (dist < neighborRange)
                    {
                        avgPos += other.position;
                        avgDir += other.forward;
                        count++;
                    }
                }
                
                // 2. Calculate flock direction
                Vector3 direction = Vector3.zero;
                
                if (count > 0)
                {
                    direction += (avgPos / count - f.position).normalized;
                    direction += (avgDir / count).normalized;
                    if (count > 3) direction += (f.position - (avgPos / count)).normalized * 0.5f;
                }
                
                // 3. Stay in area
                direction += KeepInBounds(f);
                
                // 4. Circular movement lama (dengan penyesuaian arah)
                if (useCircularMovement && !useWaveMovement)
                {
                    Vector3 toCenter = center - f.position;
                    toCenter.y = 0;
                    if (toCenter.magnitude > 0.1f)
                    {
                        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
                        
                        // =========== PERUBAHAN: Sesuaikan arah untuk mode lama ===========
                        if (rotationDirection == RotationDirection.CounterClockwise)
                        {
                            tangent = -tangent;
                        }
                        
                        direction += tangent * 0.5f;
                        
                        float radiusDiff = circleRadius - toCenter.magnitude;
                        direction += toCenter.normalized * radiusDiff * 0.1f;
                    }
                }
                
                // 5. Apply movement
                if (direction != Vector3.zero)
                {
                    if (currentForward != Vector3.up && currentForward != Vector3.down)
                        direction.y = 0;
                    
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    
                    if (currentForward != Vector3.forward)
                    {
                        Quaternion offset = Quaternion.FromToRotation(Vector3.forward, currentForward);
                        targetRot = targetRot * offset;
                    }
                    
                    f.rotation = Quaternion.Slerp(f.rotation, targetRot, rotationSpeed * Time.deltaTime);
                    f.position += f.TransformDirection(currentForward) * speed * Time.deltaTime;
                }
            }
            
            yield return wait;
        }
    }
    
    Vector3 KeepInBounds(Transform f)
    {
        Vector3 force = Vector3.zero;
        Vector3 pos = f.position;
        
        if (pos.x > center.x + areaSize.x) force += Vector3.left;
        if (pos.x < center.x - areaSize.x) force += Vector3.right;
        if (pos.z > center.z + areaSize.z) force += Vector3.back;
        if (pos.z < center.z - areaSize.z) force += Vector3.forward;
        
        if (pos.y > center.y + areaSize.y || pos.y < center.y - areaSize.y)
        {
            if (currentForward == Vector3.up || currentForward == Vector3.down)
                force += (center.y - pos.y > 0 ? Vector3.up : Vector3.down) * 0.3f;
            else
                pos.y = center.y;
        }
        
        return force.normalized;
    }
    
    // =========== TAMBAHAN: Fungsi untuk mengubah arah putaran ===========
    public void SetRotationDirection(RotationDirection newDirection)
    {
        rotationDirection = newDirection;
    }
    
    public void ToggleRotationDirection()
    {
        rotationDirection = (rotationDirection == RotationDirection.Clockwise) 
            ? RotationDirection.CounterClockwise 
            : RotationDirection.Clockwise;
    }
    
    public RotationDirection GetCurrentRotationDirection()
    {
        return rotationDirection;
    }
    // ====================================================================
    
    public void ToggleAntMill(bool enabled)
    {
        useCircularMovement = enabled;
        if (enabled)
        {
            for (int i = 0; i < fish.Count; i++)
            {
                if (i < fishAngles.Count)
                {
                    fishAngles[i] = (360f / fish.Count) * i;
                }
            }
        }
    }
    
    public void SetWaveAmplitude(float amplitude)
    {
        waveAmplitude = Mathf.Clamp(amplitude, 0f, 5f);
    }
    
    public void SetWaveFrequency(float frequency)
    {
        waveFrequency = Mathf.Clamp(frequency, 0.1f, 4f);
    }
    
    // Public methods
    public void ChangeForwardAxis(ForwardAxis newAxis)
    {
        forwardAxis = newAxis;
        SetForwardDirection();
        
        foreach (var f in fish)
        {
            if (f != null)
            {
                Vector3 up = (currentForward == Vector3.up || currentForward == Vector3.down) 
                    ? Vector3.forward : Vector3.up;
                f.rotation = Quaternion.LookRotation(currentForward, up);
            }
        }
    }
    
    public Vector3 GetCurrentForward() => currentForward;
    
    public int GetActiveFishCount() => fish.Count;
    
    void OnDrawGizmosSelected()
    {
        // Draw area boundary
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, areaSize * 2);
        
        // Draw circle path
        if (useCircularMovement)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, circleRadius);
            
            // =========== TAMBAHAN: Gambar panah arah putaran ===========
            // Gambar panah kecil untuk menunjukkan arah putaran
            Gizmos.color = (rotationDirection == RotationDirection.Clockwise) 
                ? Color.red : Color.blue;
            
            // Hitung posisi panah di lingkaran
            float arrowAngle = 45f * Mathf.Deg2Rad;
            Vector3 arrowPos = transform.position + new Vector3(
                Mathf.Cos(arrowAngle) * circleRadius * 1.2f,
                0,
                Mathf.Sin(arrowAngle) * circleRadius * 1.2f
            );
            
            // Gambar panah sesuai arah
            if (rotationDirection == RotationDirection.Clockwise)
            {
                Gizmos.DrawLine(arrowPos, arrowPos + new Vector3(-0.5f, 0, 0.5f));
                Gizmos.DrawLine(arrowPos, arrowPos + new Vector3(-0.5f, 0, -0.5f));
            }
            else
            {
                Gizmos.DrawLine(arrowPos, arrowPos + new Vector3(0.5f, 0, 0.5f));
                Gizmos.DrawLine(arrowPos, arrowPos + new Vector3(0.5f, 0, -0.5f));
            }
            // ==========================================================
            
            if (useWaveMovement && waveAmplitude > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Vector3 prevPoint = Vector3.zero;
                int segments = 36;
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (360f / segments) * i * Mathf.Deg2Rad;
                    float wave = Mathf.Sin(angle * waveFrequency) * waveAmplitude;
                    
                    Vector3 point = new Vector3(
                        Mathf.Cos(angle) * circleRadius,
                        wave,
                        Mathf.Sin(angle) * circleRadius
                    ) + transform.position;
                    
                    if (i > 0)
                    {
                        Gizmos.DrawLine(prevPoint, point);
                    }
                    prevPoint = point;
                }
            }
        }
        
        // Draw forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, currentForward * 3f);
    }
}