using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("UI References")]
    public GameObject interactionPrompt;
    
    [Header("Audio")]
    public AudioClip interactSound;
    
    private Camera playerCamera;
    private AudioSource audioSource;
    private FuelItem currentFuelItem;
    private FireController currentFire;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    void Update()
    {
        CheckForInteractables();
        
        if (Input.GetKeyDown(interactKey))
        {
            PerformInteraction();
        }
    }
    
    void CheckForInteractables()
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check for fuel items
            FuelItem fuelItem = hitObject.GetComponent<FuelItem>();
            if (fuelItem != null && !fuelItem.isCollected)
            {
                currentFuelItem = fuelItem;
                currentFire = null;
                ShowInteractionPrompt($"Collect {fuelItem.GetFuelType()} (E)");
                return;
            }
            
            // Check for fire
            FireController fire = hitObject.GetComponent<FireController>();
            if (fire != null)
            {
                currentFire = fire;
                currentFuelItem = null;
                
                if (fire.IsBurning())
                {
                    ShowInteractionPrompt($"Fire is burning - Fuel: {fire.GetFuelPercentage():P0} (E to add fuel)");
                }
                else
                {
                    ShowInteractionPrompt($"Light fire (E) - Needs {fire.minFuelToBurn} fuel");
                }
                return;
            }
        }
        
        // No interactables found
        currentFuelItem = null;
        currentFire = null;
        HideInteractionPrompt();
    }
    
    void PerformInteraction()
    {
        if (currentFuelItem != null)
        {
            CollectFuelItem();
        }
        else if (currentFire != null)
        {
            InteractWithFire();
        }
    }
    
    void CollectFuelItem()
    {
        if (currentFuelItem != null && currentFuelItem.CollectFuel())
        {
            // Add fuel to inventory or directly to nearby fire
            Debug.Log($"Collected {currentFuelItem.GetFuelType()} with {currentFuelItem.GetFuelValue()} fuel value");
            
            // Play interaction sound
            if (interactSound != null)
            {
                audioSource.PlayOneShot(interactSound);
            }
            
            // Find nearest fire to add fuel to
            FireController nearestFire = FindNearestFire();
            if (nearestFire != null && nearestFire.CanAddFuel())
            {
                nearestFire.AddFuel(currentFuelItem.GetFuelValue());
                Debug.Log($"Added fuel to fire. Current fuel: {nearestFire.currentFuel}");
            }
            else
            {
                Debug.Log("No nearby fire found to add fuel to");
            }
        }
    }
    
    void InteractWithFire()
    {
        if (currentFire == null) return;
        
        if (currentFire.IsBurning())
        {
            // Try to add fuel from inventory or nearby fuel items
            FuelItem nearbyFuel = FindNearestFuelItem();
            if (nearbyFuel != null && !nearbyFuel.isCollected)
            {
                nearbyFuel.CollectFuel();
                currentFire.AddFuel(nearbyFuel.GetFuelValue());
                Debug.Log($"Added {nearbyFuel.GetFuelValue()} fuel to fire");
            }
            else
            {
                Debug.Log("No fuel available to add to fire");
            }
        }
        else
        {
            // Try to light the fire
            if (currentFire.LightFire())
            {
                Debug.Log("Fire lit!");
            }
            else
            {
                Debug.Log("Not enough fuel to light fire");
            }
        }
        
        // Play interaction sound
        if (interactSound != null)
        {
            audioSource.PlayOneShot(interactSound);
        }
    }
    
    FireController FindNearestFire()
    {
        FireController[] fires = FindObjectsOfType<FireController>();
        FireController nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (FireController fire in fires)
        {
            float distance = Vector3.Distance(transform.position, fire.transform.position);
            if (distance < nearestDistance && distance <= interactionRange * 2)
            {
                nearest = fire;
                nearestDistance = distance;
            }
        }
        
        return nearest;
    }
    
    FuelItem FindNearestFuelItem()
    {
        FuelItem[] fuelItems = FindObjectsOfType<FuelItem>();
        FuelItem nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (FuelItem fuel in fuelItems)
        {
            if (!fuel.isCollected)
            {
                float distance = Vector3.Distance(transform.position, fuel.transform.position);
                if (distance < nearestDistance && distance <= interactionRange * 2)
                {
                    nearest = fuel;
                    nearestDistance = distance;
                }
            }
        }
        
        return nearest;
    }
    
    void ShowInteractionPrompt(string message)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            // You can set the text here if you have a Text component
            // interactionPrompt.GetComponent<Text>().text = message;
        }
        Debug.Log(message);
    }
    
    void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
