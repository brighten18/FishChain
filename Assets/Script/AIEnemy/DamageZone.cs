using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public int damagePerSecond = 5;
    public bool showGizmo = true;
    public Color gizmoColor = Color.red;
    
    void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            Collider collider = GetComponent<Collider>();
            
            if (collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(transform.position + box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
