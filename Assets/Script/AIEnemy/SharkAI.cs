using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SharkAI : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;
    public float detectionRadius = 20f;
    public float attackRadius = 5f;

    [Header("Movement")]
    public float speed = 12f;
    public float turnSpeed = 180f;

    [Header("Attack Pattern")]
    public float ascendHeight = 3f;
    public float recoveryTime = 1f;
    
    [Header("Scene Settings")]
    public string[] activeScenes = { "MainLevel" };

    private Rigidbody rb;
    private float attackTimer;
    private bool isRecovering;
    private bool isAscending;
    private Vector3 ascendStartPos;
    private bool isActiveInScene = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CheckIfActiveInCurrentScene();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.drag = 0.3f;
        }

        CheckIfActiveInCurrentScene();
        FindPlayer();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"SharkAI: Scene loaded - {scene.name}");
        CheckIfActiveInCurrentScene();
        FindPlayer();
    }

    private void CheckIfActiveInCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        isActiveInScene = false;

        foreach (string sceneName in activeScenes)
        {
            if (currentScene == sceneName)
            {
                isActiveInScene = true;
                break;
            }
        }

        if (!isActiveInScene)
        {
            if (rb != null)
                rb.velocity = Vector3.zero;
            
            Debug.Log($"SharkAI disabled - not in active scene. Current: {currentScene}");
        }
        else
        {
            Debug.Log($"SharkAI enabled in scene: {currentScene}");
        }
    }

    private void FindPlayer()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                Debug.Log($"SharkAI found player: {playerTarget.name}");
            }
            else
            {
                Debug.LogWarning("SharkAI: Player not found!");
            }
        }
    }

    void Update()
    {
        if (!isActiveInScene)
            return;

        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        if (isRecovering)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= detectionRadius)
        {
            if (distance <= attackRadius)
                AttackBehavior();
            else
                ChaseBehavior();
        }
        else
        {
            PatrolBehavior();
        }

        Debug.DrawRay(transform.position, transform.forward * 3f, Color.blue);
    }

    void ChaseBehavior()
    {
        if (playerTarget == null) return;

        Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
        RotateToFace(toPlayer, turnSpeed);

        if (rb != null)
            rb.velocity = transform.forward * speed;

        attackTimer = 0f;
    }

    void AttackBehavior()
    {
        if (playerTarget == null) return;

        attackTimer += Time.deltaTime;

        Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
        RotateToFace(toPlayer, turnSpeed * 2f);

        if (rb != null)
            rb.velocity = transform.forward * (speed * 1.5f);

        if (attackTimer >= 1f && !isAscending)
        {
            StartAscendRecovery();
        }
    }

    void PatrolBehavior()
    {
        Vector3 patrolDirection = new Vector3(
            Mathf.Sin(Time.time * 0.5f),
            0,
            Mathf.Cos(Time.time * 0.5f)
        );

        RotateToFace(patrolDirection, turnSpeed * 0.5f);

        if (rb != null)
            rb.velocity = transform.forward * (speed * 0.5f);

        attackTimer = 0f;
    }

    void RotateToFace(Vector3 direction, float rotationSpeed)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void StartAscendRecovery()
    {
        isAscending = true;
        isRecovering = true;
        ascendStartPos = transform.position;

        StartCoroutine(AscendRecoveryRoutine());
    }

    IEnumerator AscendRecoveryRoutine()
    {
        float ascendSpeed = 5f;

        while (transform.position.y < ascendStartPos.y + ascendHeight)
        {
            if (rb != null)
                rb.velocity = Vector3.up * ascendSpeed;
            yield return null;
        }

        if (rb != null)
            rb.velocity = Vector3.zero;

        yield return new WaitForSeconds(recoveryTime);

        isRecovering = false;
        isAscending = false;
        attackTimer = 0f;
    }

    public bool CanDealDamage()
    {
        if (!isActiveInScene || playerTarget == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        return !isRecovering && distance <= attackRadius && attackTimer < 1f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.right * 1.5f);
    }
}
