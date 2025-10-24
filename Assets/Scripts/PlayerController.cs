using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("Camera")]
    public Camera playerCamera;
    public float mouseSensitivity = 100f;
    
    [Header("Health & Survival")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 5f; // Health per second near fire
    public float healthDecayRate = 2f; // Health lost per second when cold
    
    [Header("Temperature")]
    public float bodyTemperature = 37f; // Normal body temp
    public float minTemperature = 20f; // Minimum safe temperature
    public float maxTemperature = 45f; // Maximum safe temperature
    public float ambientTemperature = 15f; // Outside temperature
    
    [Header("UI")]
    public GameObject healthBar;
    public GameObject temperatureIndicator;
    
    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landSound;
    
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private AudioSource audioSource;
    private FireController nearbyFire;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;
    
    void Start()
    {
        // Initialize components
        if (controller == null)
            controller = GetComponent<CharacterController>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize health and temperature
        currentHealth = maxHealth;
        bodyTemperature = 37f;
    }
    
    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleSurvival();
        HandleInteraction();
    }
    
    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Calculate movement
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
        
        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlaySound(jumpSound);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
        // Footstep sounds
        if (isGrounded && (x != 0 || z != 0))
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        yRotation += mouseX;
        
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    
    void HandleSurvival()
    {
        // Update temperature based on environment
        UpdateTemperature();
        
        // Update health based on temperature
        UpdateHealth();
        
        // Update UI
        UpdateUI();
    }
    
    void UpdateTemperature()
    {
        float targetTemperature = ambientTemperature;
        
        // Increase temperature near fire
        if (nearbyFire != null && nearbyFire.IsBurning())
        {
            float distanceToFire = Vector3.Distance(transform.position, nearbyFire.transform.position);
            float fireInfluence = Mathf.Clamp01(1f - (distanceToFire / 10f)); // 10 unit radius
            targetTemperature = Mathf.Lerp(ambientTemperature, 40f, fireInfluence);
        }
        
        // Gradually adjust body temperature
        bodyTemperature = Mathf.Lerp(bodyTemperature, targetTemperature, Time.deltaTime * 0.5f);
        bodyTemperature = Mathf.Clamp(bodyTemperature, 0f, 50f);
    }
    
    void UpdateHealth()
    {
        if (bodyTemperature < minTemperature)
        {
            // Losing health due to cold
            currentHealth -= healthDecayRate * Time.deltaTime;
        }
        else if (bodyTemperature > minTemperature && bodyTemperature < maxTemperature)
        {
            // Regenerating health near fire
            if (nearbyFire != null && nearbyFire.IsBurning())
            {
                currentHealth += healthRegenRate * Time.deltaTime;
            }
        }
        
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void UpdateUI()
    {
        // Update health bar
        if (healthBar != null)
        {
            // You can implement health bar updates here
            // healthBar.fillAmount = currentHealth / maxHealth;
        }
        
        // Update temperature indicator
        if (temperatureIndicator != null)
        {
            // You can implement temperature indicator updates here
        }
    }
    
    void HandleInteraction()
    {
        // This will be handled by PlayerInteraction script
        // But we can add some player-specific interaction here if needed
    }
    
    void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0 && audioSource != null)
        {
            AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(footstep, 0.5f);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void Die()
    {
        Debug.Log("Player died!");
        // Implement death logic here
        // You could restart the scene, show game over screen, etc.
    }
    
    public void SetNearbyFire(FireController fire)
    {
        nearbyFire = fire;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public float GetTemperaturePercentage()
    {
        return (bodyTemperature - minTemperature) / (maxTemperature - minTemperature);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            FireController fire = other.GetComponent<FireController>();
            if (fire != null)
            {
                SetNearbyFire(fire);
                Debug.Log("Player is near fire - warming up!");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            if (nearbyFire != null && other.GetComponent<FireController>() == nearbyFire)
            {
                SetNearbyFire(null);
                Debug.Log("Player left fire area - getting cold!");
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
