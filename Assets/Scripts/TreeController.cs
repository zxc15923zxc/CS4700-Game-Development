using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeController : MonoBehaviour
{
    [Header("Tree Properties")]
    public int maxWoodDrops = 3;
    public float respawnTime = 300f; // 5 minutes
    public float interactionRange = 2f;
    
    [Header("Wood Drop")]
    public GameObject woodPrefab;
    public Transform[] dropPoints;
    
    [Header("Visual Effects")]
    public ParticleSystem chopEffect;
    public AudioClip chopSound;
    public AudioClip fallSound;
    
    [Header("Animation")]
    public Animator treeAnimator;
    public string chopTrigger = "Chop";
    public string fallTrigger = "Fall";
    
    private bool isChopped = false;
    private float respawnTimer = 0f;
    private AudioSource audioSource;
    private Collider treeCollider;
    private Renderer treeRenderer;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        treeCollider = GetComponent<Collider>();
        treeRenderer = GetComponent<Renderer>();
        
        // If no drop points specified, use the tree position
        if (dropPoints.Length == 0)
        {
            dropPoints = new Transform[1];
            dropPoints[0] = transform;
        }
    }
    
    void Update()
    {
        if (isChopped)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnTime)
            {
                RespawnTree();
            }
        }
    }
    
    public bool ChopTree()
    {
        if (isChopped) return false;
        
        isChopped = true;
        respawnTimer = 0f;
        
        // Play chop effect
        if (chopEffect != null)
        {
            chopEffect.Play();
        }
        
        // Play chop sound
        if (chopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(chopSound);
        }
        
        // Play fall animation
        if (treeAnimator != null)
        {
            treeAnimator.SetTrigger(fallTrigger);
        }
        
        // Play fall sound after a delay
        if (fallSound != null && audioSource != null)
        {
            Invoke("PlayFallSound", 1f);
        }
        
        // Drop wood
        DropWood();
        
        // Hide the tree
        StartCoroutine(HideTree());
        
        return true;
    }
    
    void DropWood()
    {
        if (woodPrefab == null) return;
        
        int woodToDrop = Random.Range(1, maxWoodDrops + 1);
        
        for (int i = 0; i < woodToDrop; i++)
        {
            Transform dropPoint = dropPoints[Random.Range(0, dropPoints.Length)];
            Vector3 dropPosition = dropPoint.position + Random.insideUnitSphere * 2f;
            dropPosition.y = dropPoint.position.y;
            
            GameObject wood = Instantiate(woodPrefab, dropPosition, Quaternion.identity);
            
            // Add some random force to make it look natural
            Rigidbody woodRb = wood.GetComponent<Rigidbody>();
            if (woodRb != null)
            {
                woodRb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }
    }
    
    void PlayFallSound()
    {
        if (fallSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
    }
    
    IEnumerator HideTree()
    {
        yield return new WaitForSeconds(2f);
        
        if (treeRenderer != null)
        {
            treeRenderer.enabled = false;
        }
        
        if (treeCollider != null)
        {
            treeCollider.enabled = false;
        }
    }
    
    void RespawnTree()
    {
        isChopped = false;
        respawnTimer = 0f;
        
        // Show the tree again
        if (treeRenderer != null)
        {
            treeRenderer.enabled = true;
        }
        
        if (treeCollider != null)
        {
            treeCollider.enabled = true;
        }
        
        // Reset animation
        if (treeAnimator != null)
        {
            treeAnimator.SetTrigger("Respawn");
        }
        
        Debug.Log("Tree has respawned!");
    }
    
    public bool CanBeChopped()
    {
        return !isChopped;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isChopped)
        {
            Debug.Log("Press E to chop tree");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Tree interaction prompt hidden");
        }
    }
}
