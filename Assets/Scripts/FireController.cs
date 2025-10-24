using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("Fire Properties")]
    public float maxFuel = 100f;
    public float currentFuel = 0f;
    public float fuelBurnRate = 5f; // Fuel consumed per second
    public float minFuelToBurn = 10f; // Minimum fuel needed to start burning

    [Header("Fire States")]
    public bool isBurning = false;
    public bool canBeLit = true;
    
    [Header("Campfire Asset References")]
    [Tooltip("Reference to the Fire prefab from your campfire asset")]
    public GameObject firePrefab;
    [Tooltip("Reference to the AudioSource with campfire sound")]
    public AudioSource fireAudio;
    [Tooltip("Reference to the Light component")]
    public Light fireLight;
    [Tooltip("Reference to the LightFlicker script on the light")]
    public Abiogenesis3d.LightFlicker lightFlicker;
    
    [Header("Light Properties")]
    public float maxLightIntensity = 2f;
    public float minLightIntensity = 0.1f;
    public Color fireColor = Color.red;
    
    private float baseLightIntensity;
    
    void Start()
    {
        // Initialize fire state
        baseLightIntensity = maxLightIntensity;
        
        // Start with no fuel
        currentFuel = 0f;
        isBurning = false;
        
        // Set up audio
        if (fireAudio != null)
        {
            fireAudio.loop = true;
            fireAudio.volume = 0.5f;
        }
        
        // Initialize fire state
        UpdateFireState();
    }
    
    void Update()
    {
        if (isBurning && currentFuel > 0)
        {
            // Consume fuel over time
            currentFuel -= fuelBurnRate * Time.deltaTime;
            currentFuel = Mathf.Max(0, currentFuel);
            
            // Update fire intensity based on fuel level
            UpdateFireIntensity();
            
            // Check if fire should extinguish
            if (currentFuel <= 0)
            {
                ExtinguishFire();
            }
        }
        else if (isBurning && currentFuel <= 0)
        {
            ExtinguishFire();
        }
    }
    
    public bool AddFuel(float amount)
    {
        if (amount <= 0) return false;
        
        currentFuel += amount;
        currentFuel = Mathf.Min(currentFuel, maxFuel);
        
        // Auto-light fire if it has enough fuel and isn't burning
        if (!isBurning && currentFuel >= minFuelToBurn && canBeLit)
        {
            LightFire();
        }
        
        return true;
    }
    
    public bool LightFire()
    {
        if (currentFuel >= minFuelToBurn && canBeLit)
        {
            isBurning = true;
            UpdateFireState();
            return true;
        }
        return false;
    }
    
    public void ExtinguishFire()
    {
        isBurning = false;
        UpdateFireState();
    }
    
    private void UpdateFireState()
    {
        // Update fire prefab (enable/disable the visual fire)
        if (firePrefab != null)
        {
            firePrefab.SetActive(isBurning);
        }
        
        // Update light
        if (fireLight != null)
        {
            fireLight.enabled = isBurning;
            fireLight.color = fireColor;
        }
        
        // Update light flicker script
        if (lightFlicker != null)
        {
            lightFlicker.enabled = isBurning;
        }
        
        // Update audio
        if (fireAudio != null)
        {
            if (isBurning)
            {
                fireAudio.Play();
            }
            else
            {
                fireAudio.Stop();
            }
        }
    }
    
    private void UpdateFireIntensity()
    {
        float fuelRatio = currentFuel / maxFuel;
        
        // Update light intensity based on fuel level
        if (fireLight != null)
        {
            float targetIntensity = minLightIntensity + (maxLightIntensity - minLightIntensity) * fuelRatio;
            fireLight.intensity = Mathf.Lerp(fireLight.intensity, targetIntensity, Time.deltaTime * 2f);
        }
        
        // Update audio volume based on fuel level
        if (fireAudio != null)
        {
            float targetVolume = 0.2f + (0.3f * fuelRatio);
            fireAudio.volume = Mathf.Lerp(fireAudio.volume, targetVolume, Time.deltaTime * 2f);
        }
    }
    
    
    public float GetFuelPercentage()
    {
        return currentFuel / maxFuel;
    }
    
    public bool CanAddFuel()
    {
        return currentFuel < maxFuel;
    }
    
    public bool IsBurning()
    {
        return isBurning;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player is near fire for warmth/healing effects
        if (other.CompareTag("Player"))
        {
            // Could add warmth/health regeneration here
            Debug.Log("Player is near the fire!");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left the fire area");
        }
    }
}
