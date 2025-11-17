using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    public float terminalVelocity = -50f; // Prevent infinite falling speed

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

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

    // New: control how quickly body temperature moves toward the target.
    // Tweak these in the Inspector:
    public float temperatureLerpSpeedNearFire = 2.0f;      // faster when near fire
    public float temperatureLerpSpeedAwayFromFire = 0.2f;  // slower when away from fire
    public float fireInfluenceRadius = 10f;                // same radius used for influence calculation

    [Header("UI")]
    public GameObject healthBar;
    public GameObject temperatureIndicator;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landSound;

    [Header("Combat")]
    public float attackRate = 1.0f;            // Attacks per second
    public float attackAnimationDelay = 0.1f;  // Delay before animation starts (helps sync)
    public float attackCooldown = 0.4f;        // Delay before you can move/attack again
    public AudioClip attackSound;

    private Animator animator;


    private Vector3 velocity;
    private bool isGrounded;
    private AudioSource audioSource;
    private FireController nearbyFire;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;

    void Start()
    {
        // Initialize components
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Initialize velocity to prevent flying
        velocity = Vector3.zero;

        // Check if ground check is set up
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check transform is not assigned! Please assign it in the inspector.");
            // Create a temporary ground check if none exists
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
            Debug.Log("Created temporary ground check. Please assign a proper one in the inspector.");
        }

        if (groundMask == 0)
        {
            Debug.LogError("Ground Mask is not set! Please set it to the ground layer in the inspector.");
            // Set to default layer if none assigned
            groundMask = 1; // Default layer
            Debug.Log("Set ground mask to default layer. Please assign the correct ground layer in the inspector.");
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize health and temperature
        currentHealth = maxHealth;
        bodyTemperature = 37f;

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovement();
        HandleSurvival();
        HandleInteraction();
        HandleCombat();
    }

    void HandleCombat()
    {
	// Basic left-click
	if (Input.GetMouseButtonDown(0))
	{
            Debug.Log("Left click detect");
            animator.SetTrigger("PunchRight");
	}
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Debug ground check
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log($"Grounded: {isGrounded}, Velocity Y: {velocity.y}");
        }

        // Reset velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Debug input
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log($"Input: x={x}, z={z}, Player Rotation: {transform.rotation.eulerAngles}");
        }

        // Calculate movement (only horizontal movement)
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlaySound(jumpSound);
        }

        // Apply gravity with terminal velocity limit
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, terminalVelocity);

        // Apply vertical movement
        controller.Move(new Vector3(0, velocity.y * Time.deltaTime, 0));

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
            float fireInfluence = Mathf.Clamp01(1f - (distanceToFire / fireInfluenceRadius)); // Use influence radius
            targetTemperature = Mathf.Lerp(ambientTemperature, 40f, fireInfluence);
        }

        // Determine lerp speed based on distance to fire
        float distanceToNearestFire = (nearbyFire != null) ? Vector3.Distance(transform.position, nearbyFire.transform.position) : fireInfluenceRadius;
        float lerpSpeed = Mathf.Lerp(temperatureLerpSpeedAwayFromFire, temperatureLerpSpeedNearFire, Mathf.Clamp01(1f - (distanceToNearestFire / fireInfluenceRadius)));

        // Gradually adjust body temperature
        bodyTemperature = Mathf.Lerp(bodyTemperature, targetTemperature, Time.deltaTime * lerpSpeed);
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
        // Update health bar using the project's HealthBar component when available.
        if (healthBar != null)
        {
            HealthBar hb = healthBar.GetComponent<HealthBar>();
            // If HealthBar component exists, keep it in sync:
            if (hb != null)
            {
                // If HealthBar expects a PlayerState, assign the project's singleton if available.
                if (hb.playerState == null && PlayerState.Instance != null)
                {
                    hb.playerState = PlayerState.Instance.gameObject;
                }

                // Try to update the Slider on the same GameObject (instant visual sync).
                Slider slider = healthBar.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.value = (maxHealth > 0f) ? (currentHealth / maxHealth) : 0f;
                }

                // Update numeric counter if present.
                if (hb.healthCounter != null)
                {
                    hb.healthCounter.text = Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
                }
            }
            else
            {
                // Fallbacks if healthBar is a raw Image or other simple UI element.
                Image img = healthBar.GetComponent<Image>();
                if (img != null)
                {
                    img.fillAmount = (maxHealth > 0f) ? (currentHealth / maxHealth) : 0f;
                }
            }
        }

        // Update temperature indicator: support Slider, Image (color), or Text as common cases.
        if (temperatureIndicator != null)
        {
            float tempPct = Mathf.Clamp01(GetTemperaturePercentage());

            Text tempText = temperatureIndicator.GetComponent<Text>();
            if (tempText != null)
            {
                tempText.text = "Body Temp: " + Mathf.Round(bodyTemperature) + "°C";
            }
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
        // Look for a FireController on the collider or any of its parents
        FireController fire = other.GetComponent<FireController>();
        if (fire == null)
        {
            fire = other.GetComponentInParent<FireController>();
        }
        if (fire != null)
        {
            SetNearbyFire(fire);
            Debug.Log("Player is near fire - warming up!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Look for a FireController on the collider or any of its parents
        FireController fire = other.GetComponent<FireController>();
        if (fire == null)
        {
            fire = other.GetComponentInParent<FireController>();
        }
        if (nearbyFire != null && fire == nearbyFire)
        {
            SetNearbyFire(null);
            Debug.Log("Player left fire area - getting cold!");
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