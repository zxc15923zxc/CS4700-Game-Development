using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FuelUI : MonoBehaviour
{
    [Tooltip("Reference to the FireController that provides fuel status. If left empty the script will find the first FireController in the scene.")]
    public FireController fire;

    [Tooltip("UI Text that shows numeric fuel (current / max).")]
    public Text fuelText;

    void Awake()
    {
        if (fire == null)
            fire = FindObjectOfType<FireController>();

        if (fire == null)
        {
            Debug.LogWarning("FuelUI: No FireController assigned or found in the scene.");
            return;
        }

        // Subscribe so UI updates automatically
        fire.onFuelChanged.AddListener(OnFuelChanged);

        // Initialize immediately
        UpdateFuelText();
    }

    void OnDestroy()
    {
        if (fire != null)
            fire.onFuelChanged.RemoveListener(OnFuelChanged);
    }

    // Called by FireController with a 0..1 fuel percentage.
    public void OnFuelChanged(float fuelPct)
    {
        UpdateFuelText();
    }

    private void UpdateFuelText()
    {
        if (fuelText == null || fire == null)
            return;

        fuelText.text = "Current fuel: " + $"{Mathf.CeilToInt(fire.currentFuel)} / {Mathf.CeilToInt(fire.maxFuel)}";
    }
}
