using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 10;
    public float attackCooldown = 1f;
    
    [Header("Visual Feedback")]
    public ParticleSystem attackEffect;
    public AudioClip attackSound;
    
    private float lastAttackTime;
    private SharkAI parentSharkAI;
    
    void Start()
    {
        parentSharkAI = GetComponentInParent<SharkAI>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Hanya damage player saat hiu dalam state Attack (bukan Ascending/Recovering)
        if (parentSharkAI != null && !parentSharkAI.CanDealDamage())
            return;
            
        if (other.CompareTag("Player") && Time.time > lastAttackTime + attackCooldown)
        {
            LevelSystem playerHealth = other.GetComponent<LevelSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                lastAttackTime = Time.time;
                
                PlayAttackEffects();
                
                Debug.Log($"Shark attacked player! -{damageAmount} HP");
            }
        }
    }
    
    void PlayAttackEffects()
    {
        if (attackEffect != null)
        {
            Instantiate(attackEffect, transform.position, Quaternion.identity);
        }
        
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }
    }
}
