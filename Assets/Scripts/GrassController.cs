using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassController : MonoBehaviour
{
    [Header("Grass Properties")]
    public float respawnTime = 60f; // 1 minute
    public float interactionRange = 1f;
    
    [Header("Grass Drop")]
    public GameObject grassPrefab;
    public int maxGrassDrops = 2;
    
    [Header("Visual Effects")]
    public ParticleSystem collectEffect;
    public AudioClip collectSound;
    
    [Header("Animation")]
    public Animator grassAnimator;
    public string collectTrigger = "Collect";
    
    private bool isCollected = false;
    private float respawnTimer = 0f;
    private AudioSource audioSource;
    private Collider grassCollider;
    private Renderer grassRenderer;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        grassCollider = GetComponent<Collider>();
        grassRenderer = GetComponent<Renderer>();
    }
    
    void Update()
    {
        if (isCollected)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnTime)
            {
                RespawnGrass();
            }
        }
    }
    
    public bool CollectGrass()
    {
        if (isCollected) return false;
        
        isCollected = true;
        respawnTimer = 0f;
        
        // Play collect effect
        if (collectEffect != null)
        {
            collectEffect.Play();
        }
        
        // Play collect sound
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Play collect animation
        if (grassAnimator != null)
        {
            grassAnimator.SetTrigger(collectTrigger);
        }
        
        // Drop grass items
        DropGrass();
        
        // Hide the grass
        StartCoroutine(HideGrass());
        
        return true;
    }
    
    void DropGrass()
    {
        if (grassPrefab == null) return;
        
        int grassToDrop = Random.Range(1, maxGrassDrops + 1);
        
        for (int i = 0; i < grassToDrop; i++)
        {
            Vector3 dropPosition = transform.position + Random.insideUnitSphere * 1f;
            dropPosition.y = transform.position.y;
            
            GameObject grass = Instantiate(grassPrefab, dropPosition, Quaternion.identity);
            
            // Add some random force
            Rigidbody grassRb = grass.GetComponent<Rigidbody>();
            if (grassRb != null)
            {
                grassRb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }
    }
    
    IEnumerator HideGrass()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (grassRenderer != null)
        {
            grassRenderer.enabled = false;
        }
        
        if (grassCollider != null)
        {
            grassCollider.enabled = false;
        }
    }
    
    void RespawnGrass()
    {
        isCollected = false;
        respawnTimer = 0f;
        
        // Show the grass again
        if (grassRenderer != null)
        {
            grassRenderer.enabled = true;
        }
        
        if (grassCollider != null)
        {
            grassCollider.enabled = true;
        }
        
        // Reset animation
        if (grassAnimator != null)
        {
            grassAnimator.SetTrigger("Respawn");
        }
        
        Debug.Log("Grass has regrown!");
    }
    
    public bool CanBeCollected()
    {
        return !isCollected;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            Debug.Log("Press E to collect grass");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Grass interaction prompt hidden");
        }
    }
}
