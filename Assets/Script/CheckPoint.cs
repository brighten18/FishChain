using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public Material activatedMaterial;
    public Material deactivatedMaterial;
    public Light checkpointLight;
    public AudioClip activationSound;
    
    private bool isActivated = false;
    private Renderer checkpointRenderer;
    
    void Start()
    {
        checkpointRenderer = GetComponent<Renderer>();
        if (checkpointRenderer != null && deactivatedMaterial != null)
        {
            checkpointRenderer.material = deactivatedMaterial;
        }
        
        if (checkpointLight != null)
        {
            checkpointLight.enabled = false;
        }
    }
    
    public void Activate()
    {
        if (isActivated) return;
        
        isActivated = true;
        
        // Visual feedback
        if (checkpointRenderer != null && activatedMaterial != null)
        {
            checkpointRenderer.material = activatedMaterial;
        }
        
        if (checkpointLight != null)
        {
            checkpointLight.enabled = true;
        }
        
        // Sound feedback
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }
        
        Debug.Log("Checkpoint activated!");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            Activate();
        }
    }
}
