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

    // control how quickly body temperature moves toward the target.
    public float temperatureLerpSpeedNearFire = 2.0f;      // faster when near fire
    public float temperatureLerpSpeedAwayFromFire = 0.2f;  // slower when away from fire
    public float fireInfluenceRadius = 10f;                // same radius used for influence calculation

    [Header("UI")]
    public GameObject healthBar;
    public GameObject temperatureIndicator;

    // =======================
    // AUDIO
    // =======================
    [Header("Run Footsteps Only")]
    [Tooltip("Drop your Footsteps_Grass_Run_01..15 clips here")]
    public AudioClip[] footstepRunClips;
    [Range(0f, 10f)] public float footstepVolume = 0.6f;
    [Range(0f, 0.2f)] public float footstepPitchJitter = 0.05f;
    [Tooltip("Seconds between steps while running")]
    public float runStepInterval = 0.32f;
    [Tooltip("Planar speed at/over this counts as running")]
    public float runSpeedThreshold = 5f;

    [Header("Jump Audio (paired)")]
    [Tooltip("Start/takeoff clips: Footsteps_Grass_Jump_Start_01..")]
    public AudioClip[] jumpStartClips;
    [Tooltip("Land clips that pair by index with Start clips")]
    public AudioClip[] jumpLandClips;
    [Range(0f, 10f)] public float jumpStartVolume = 0.8f;
    [Range(0f, 10f)] public float jumpLandVolume = 0.9f;
    [Range(0f, 0.2f)] public float jumpPitchJitter = 0.05f;

    [Header("Combat")]
    public float attackRate = 1.0f;            // Attacks per second
    public float attackAnimationDelay = 0.1f;  // Delay before animation starts (helps sync)
    public float attackCooldown = 0.4f;        // Delay before you can move/attack again
    public AudioClip attackSound;

    private Animator animator;

    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private AudioSource audioSource;
    private FireController nearbyFire;

    // run footstep state
    private float footstepTimer = 0f;
    private int lastFootIndex = -1;

    // jump state
    private bool pendingLandSound;
    private int lastJumpIndex = -1;

    private bool isDead;

    void Start()
    {
        // Initialize components
        if (controller == null)
            controller = GetComponent<CharacterController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;   // 3D
        audioSource.dopplerLevel = 0f;

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
            if (animator != null) animator.SetTrigger("PunchRight");

            HitEnemy();
        }
    }

    void HitEnemy()
    {
        int attackRange = 1;

        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, attackRange))
        {
            EnemyController enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage();
                Debug.Log("Hit enemy!");
            }
        }
    }
    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // ----- INPUT -----
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Horizontal movement vector (local space), NOT yet multiplied by Time.deltaTime
        Vector3 horizDir = (transform.right * x + transform.forward * z);
        Vector3 horizVel = horizDir.normalized * speed;      // actual horizontal velocity (m/s)
        float planarSpeed = horizVel.magnitude;               // use this for footsteps

        // Animation

        if (planarSpeed > 0f)
        {
            animator.SetFloat("planarSpeed", 1);
        }
        else
        {
            animator.SetFloat("planarSpeed", 0);
        }
        // Jump (play start now and arm land)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayJumpStart();
            pendingLandSound = true;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, terminalVelocity);

        // ----- ONE SINGLE MOVE CALL -----
        Vector3 totalMotion = (horizVel + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime;
        controller.Move(totalMotion);

        // Land sound (edge)
        if (!wasGrounded && isGrounded && pendingLandSound)
        {
            PlayJumpLand();
            pendingLandSound = false;
        }
        wasGrounded = isGrounded;

        // ----- RUN-ONLY FOOTSTEPS -----
        bool moving = planarSpeed > 0.1f;
        bool runningBySpeed = planarSpeed >= runSpeedThreshold;

        if (isGrounded && moving && runningBySpeed)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= runStepInterval)
            {
                PlayRunFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
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
        // Temperature text example
        if (temperatureIndicator != null)
        {
            Text tempText = temperatureIndicator.GetComponent<Text>();
            if (tempText != null)
            {
                tempText.text = "Body Temp: " + Mathf.Round(bodyTemperature) + "°C";
            }
        }
    }

    void HandleInteraction()
    {
        // Reserved for later
    }

    // =======================
    // AUDIO HELPERS
    // =======================

    void PlayRunFootstep()
    {
        var clip = NextClipNoImmediateRepeat(footstepRunClips, ref lastFootIndex);
        if (!clip || audioSource == null) return;

        audioSource.pitch = 1f + Random.Range(-footstepPitchJitter, footstepPitchJitter);
        audioSource.PlayOneShot(clip, footstepVolume);
        audioSource.pitch = 1f; // reset
    }

    void PlayJumpStart()
    {
        if (jumpStartClips == null || jumpStartClips.Length == 0 || audioSource == null) return;

        int i = (jumpStartClips.Length == 1) ? 0 : RandomIndexNotEqual(jumpStartClips.Length, lastJumpIndex);
        lastJumpIndex = i;

        var clip = jumpStartClips[i];
        if (!clip) return;

        audioSource.pitch = 1f + Random.Range(-jumpPitchJitter, jumpPitchJitter);
        audioSource.PlayOneShot(clip, jumpStartVolume);
        audioSource.pitch = 1f;
    }

    void PlayJumpLand()
    {
        if (jumpLandClips == null || jumpLandClips.Length == 0 || audioSource == null) return;

        int i = Mathf.Clamp(lastJumpIndex, 0, jumpLandClips.Length - 1);
        var clip = jumpLandClips[i];
        if (!clip) return;

        audioSource.pitch = 1f + Random.Range(-jumpPitchJitter, jumpPitchJitter);
        audioSource.PlayOneShot(clip, jumpLandVolume);
        audioSource.pitch = 1f;
    }

    static AudioClip NextClipNoImmediateRepeat(AudioClip[] list, ref int lastIndex)
    {
        if (list == null || list.Length == 0) return null;
        if (list.Length == 1) { lastIndex = 0; return list[0]; }

        int i = RandomIndexNotEqual(list.Length, lastIndex);
        lastIndex = i;
        return list[i];
    }

    static int RandomIndexNotEqual(int length, int notThis)
    {
        int i;
        do { i = Random.Range(0, length); } while (i == notThis);
        return i;
    }

    // =======================
    // GAME OVER / MISC
    // =======================

    void PlayOneShotSafe(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        currentHealth = 0f; // clamp
        Debug.Log("Player died!");

        // Show Game Over if it's in the scene
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.Show();
        }
        else
        {
            Debug.LogWarning("GameOverUI.Instance is null. Make sure a GameOverUI is in the scene and its panel is wired.");
        }

        // Optional: stop controls, unlock cursor, etc.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
