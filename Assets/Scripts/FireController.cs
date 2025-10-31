using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FireController : MonoBehaviour
{
    [Header("Fire Properties")]
    public float maxFuel = 100f;
    public float currentFuel = 0f;
    public float fuelBurnRate = 5f; // Fuel consumed per second
    public float minFuelToBurn = 10f; // Minimum fuel needed to start burning
    [Tooltip("If true, the fire starts lit on play using initialFuel.")]
    public bool startLit = true;
    [Tooltip("Fuel amount to start with if startLit is enabled.")]
    public float initialFuel = 50f;

    [Header("Fire State")]
    public bool isBurning = false;
    public bool canBeLit = true;

    [Header("Events (optional)")]
    [Tooltip("Invoked when the fire starts burning.")]
    public UnityEvent onLit;
    [Tooltip("Invoked when the fire is extinguished.")]
    public UnityEvent onExtinguished;
    [Tooltip("Invoked when fuel changes. Parameter: current fuel percentage (0..1).")]
    public UnityEvent<float> onFuelChanged;

    void Start()
    {
        // Initialize starting fuel and burning state
        if (startLit)
        {
            if (currentFuel <= 0f)
                currentFuel = Mathf.Clamp(initialFuel, 0f, maxFuel);

            isBurning = currentFuel >= minFuelToBurn && canBeLit;
        }
        else
        {
            // If not explicitly starting lit, derive burning from current values if sensible
            isBurning = currentFuel >= minFuelToBurn && canBeLit;
        }

        // Notify listeners about initial state
        if (isBurning)
            onLit?.Invoke();
        else
            onExtinguished?.Invoke();

        onFuelChanged?.Invoke(GetFuelPercentage());
    }

    void Update()
    {
        if (!isBurning)
            return;

        if (currentFuel > 0f)
        {
            currentFuel -= fuelBurnRate * Time.deltaTime;
            currentFuel = Mathf.Max(0f, currentFuel);
            onFuelChanged?.Invoke(GetFuelPercentage());

            if (currentFuel <= 0f)
            {
                ExtinguishFire();
            }
        }
        else
        {
            ExtinguishFire();
        }
    }

    public bool AddFuel(float amount)
    {
        if (amount <= 0f) return false;

        currentFuel = Mathf.Clamp(currentFuel + amount, 0f, maxFuel);
        onFuelChanged?.Invoke(GetFuelPercentage());

        // Auto-light if enough fuel and permitted
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
            onLit?.Invoke();
            return true;
        }
        return false;
    }

    public void ExtinguishFire()
    {
        if (!isBurning) return;
        isBurning = false;
        onExtinguished?.Invoke();
    }

    public float GetFuelPercentage()
    {
        if (maxFuel <= 0f) return 0f;
        return Mathf.Clamp01(currentFuel / maxFuel);
    }

    public bool CanAddFuel()
    {
        return currentFuel < maxFuel;
    }

    public bool IsBurning()
    {
        return isBurning;
    }
}
