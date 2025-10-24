using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSetupHelper : MonoBehaviour
{
    [Header("Fire Setup")]
    public bool autoSetupFire = true;
    
    [Header("Fire Components")]
    public ParticleSystem fireParticles;
    public ParticleSystem smokeParticles;
    public Light fireLight;
    public AudioSource fireAudio;
    public AudioClip fireSound;
    
    [Header("Fire Properties")]
    public float fireIntensity = 2f;
    public Color fireColor = Color.red;
    public float lightRange = 10f;
    
    void Start()
    {
        if (autoSetupFire)
        {
            SetupFire();
        }
    }
    
    public void SetupFire()
    {
        // Get or add FireController
        FireController fireController = GetComponent<FireController>();
        if (fireController == null)
        {
            fireController = gameObject.AddComponent<FireController>();
        }
        
        // Setup particle systems
        SetupParticleSystems();
        
        // Setup light
        SetupLight();
        
        // Setup audio
        SetupAudio();
        
        // Setup collider for interaction
        SetupCollider();
        
        // Assign components to FireController
        fireController.fireParticles = fireParticles;
        fireController.smokeParticles = smokeParticles;
        fireController.fireLight = fireLight;
        fireController.fireAudio = fireAudio;
        fireController.maxLightIntensity = fireIntensity;
        fireController.fireColor = fireColor;
        
        Debug.Log("Fire setup completed!");
    }
    
    void SetupParticleSystems()
    {
        // Create fire particles if not assigned
        if (fireParticles == null)
        {
            GameObject fireParticleObj = new GameObject("FireParticles");
            fireParticleObj.transform.SetParent(transform);
            fireParticleObj.transform.localPosition = Vector3.zero;
            fireParticles = fireParticleObj.AddComponent<ParticleSystem>();
        }
        
        // Configure fire particles
        var main = fireParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 3f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.5f, 0f, 1f);
        main.maxParticles = 100;
        
        var emission = fireParticles.emission;
        emission.rateOverTime = 50f;
        
        var shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        var velocityOverLifetime = fireParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f);
        
        // Create smoke particles if not assigned
        if (smokeParticles == null)
        {
            GameObject smokeParticleObj = new GameObject("SmokeParticles");
            smokeParticleObj.transform.SetParent(transform);
            smokeParticleObj.transform.localPosition = Vector3.zero;
            smokeParticles = smokeParticleObj.AddComponent<ParticleSystem>();
        }
        
        // Configure smoke particles
        var smokeMain = smokeParticles.main;
        smokeMain.startLifetime = 4f;
        smokeMain.startSpeed = 1f;
        smokeMain.startSize = 1f;
        smokeMain.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        smokeMain.maxParticles = 50;
        
        var smokeEmission = smokeParticles.emission;
        smokeEmission.rateOverTime = 20f;
        
        var smokeShape = smokeParticles.shape;
        smokeShape.shapeType = ParticleSystemShapeType.Circle;
        smokeShape.radius = 0.3f;
    }
    
    void SetupLight()
    {
        // Create light if not assigned
        if (fireLight == null)
        {
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            fireLight = lightObj.AddComponent<Light>();
        }
        
        // Configure light
        fireLight.type = LightType.Point;
        fireLight.color = fireColor;
        fireLight.intensity = fireIntensity;
        fireLight.range = lightRange;
        fireLight.shadows = LightShadows.Soft;
    }
    
    void SetupAudio()
    {
        // Create audio source if not assigned
        if (fireAudio == null)
        {
            fireAudio = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio
        fireAudio.clip = fireSound;
        fireAudio.loop = true;
        fireAudio.volume = 0.5f;
        fireAudio.spatialBlend = 1f; // 3D sound
        fireAudio.maxDistance = 20f;
    }
    
    void SetupCollider()
    {
        // Add trigger collider for interaction
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        
        col.isTrigger = true;
        
        // Set size for interaction area
        if (col is BoxCollider boxCol)
        {
            boxCol.size = new Vector3(3f, 2f, 3f);
        }
    }
    
    [ContextMenu("Setup Fire")]
    public void SetupFireFromMenu()
    {
        SetupFire();
    }
}
