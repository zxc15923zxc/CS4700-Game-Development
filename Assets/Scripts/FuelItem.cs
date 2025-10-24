using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuelItem : MonoBehaviour
{
    [Header("Fuel Properties")]
    public float fuelValue = 25f;
    public FuelType fuelType = FuelType.Wood;
    public bool isCollected = false;
    
    [Header("Visual Effects")]
    public GameObject collectEffect;
    public AudioClip collectSound;
    public float rotationSpeed = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    
    private Vector3 startPosition;
    private float bobTimer;
    private AudioSource audioSource;
    
    public enum FuelType
    {
        Wood,
        Stick,
        Grass,
        Coal,
        Paper
    }
    
    void Start()
    {
        startPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        
        // Add collider if not present
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        // Make sure it's a trigger
        GetComponent<Collider>().isTrigger = true;
        
        // Add rigidbody for physics if not present
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
    
    void Update()
    {
        if (!isCollected)
        {
            // Rotate the fuel item
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            
            // Bob up and down
            bobTimer += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(bobTimer) * bobHeight;
            transform.position = startPosition + Vector3.up * yOffset;
        }
    }
    
    public bool CollectFuel()
    {
        if (isCollected) return false;
        
        isCollected = true;
        
        // Play collection effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Play collection sound
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Hide the object
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        
        // Destroy after a short delay
        Destroy(gameObject, 1f);
        
        return true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            // Show collection prompt (you can implement UI here)
            Debug.Log($"Press E to collect {fuelType} (Fuel: {fuelValue})");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Hide collection prompt
            Debug.Log("Collection prompt hidden");
        }
    }
    
    public float GetFuelValue()
    {
        return fuelValue;
    }
    
    public FuelType GetFuelType()
    {
        return fuelType;
    }
}
