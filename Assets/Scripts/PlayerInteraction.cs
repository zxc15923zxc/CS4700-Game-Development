using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Camera used for pointing (defaults to Camera.main when empty).")]
    public Camera playerCamera;
    [Tooltip("Max distance the player can point to interact.")]
    public float maxInteractDistance = 3.0f;
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Only these layers will be considered by the raycast. Use -1 for everything.")]
    public LayerMask interactLayerMask = -1;
    [Tooltip("If no fire is pointed at, the player can still add fuel to the nearest fire within this radius.")]
    public float proximityInteractRadius = 2.5f;

    [Header("Carry / Inventory")]
    public float carriedFuel = 0f;
    public float maxCarry = 100f;

    [Header("UI (optional)")]
    public InteractionPromptUI promptUI;    // assign the InteractPrompt GameObject (with InteractionPromptUI)
    public Text carriedFuelText;            // optional numeric carried fuel display

    private FuelItem pointedFuel;
    private FireController pointedFire;             // only set if the raycast hit a BoxCollider on the campfire
    private FireController nearbyFireInRange;       // search result restricted to fires with BoxCollider

    // suppress automatic prompt updates while a temporary prompt is visible
    private float promptSuppressUntil = 0f;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        UpdatePointedTarget();
        UpdateCarriedUI();
        UpdatePromptUI();

        if (Input.GetKeyDown(interactKey))
        {
            if (pointedFuel != null && !pointedFuel.isCollected)
            {
                TryCollectFuel(pointedFuel);
            }
            else
            {
                // Prefer pointedFire (must be on a BoxCollider). If none, find nearest fire's BoxCollider within proximity radius (action-only)
                FireController fireToUse = pointedFire ?? nearbyFireInRange;
                if (fireToUse != null && carriedFuel > 0f)
                {
                    float added = carriedFuel;
                    if (fireToUse.AddFuel(added))
                    {
                        // Immediately hide any current prompt so it won't linger
                        promptUI?.HideImmediate();

                        // Clear carried fuel immediately so prompt logic doesn't re-show the add prompt
                        carriedFuel = 0f;
                        UpdateCarriedUI();

                        // Suppress UpdatePromptUI while showing the temporary success message
                        float duration = 1.5f;
                        promptSuppressUntil = Time.time + duration;
                        promptUI?.ShowTemporary($"+{Mathf.CeilToInt(added)} fuel added", duration);

                        // Clear pointed references so next frames don't accidentally re-show the prompt
                        pointedFire = null;
                        nearbyFireInRange = null;
                    }
                    else
                    {
                        float duration = 1.25f;
                        promptSuppressUntil = Time.time + duration;
                        promptUI?.ShowTemporary("Campfire cannot accept more fuel", duration);
                    }
                }
            }
        }
    }

    // Raycast from camera center to determine what the player is pointing at.
    private void UpdatePointedTarget()
    {
        pointedFuel = null;
        pointedFire = null;
        nearbyFireInRange = null;

        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxInteractDistance, interactLayerMask, QueryTriggerInteraction.Collide))
        {
            // fuel items: accept any hit (they typically have trigger colliders)
            pointedFuel = hit.collider.GetComponent<FuelItem>() ?? hit.collider.GetComponentInParent<FuelItem>() ?? hit.collider.GetComponentInChildren<FuelItem>();

            // fire: only treat as "pointed" if the ray actually hit a BoxCollider that belongs to the FireController
            FireController fc = hit.collider.GetComponent<FireController>() ?? hit.collider.GetComponentInParent<FireController>() ?? hit.collider.GetComponentInChildren<FireController>();
            if (fc != null)
            {
                // check whether the EXACT collider hit (or a parent/child) is a BoxCollider on the fire
                BoxCollider hitBox = hit.collider as BoxCollider;
                if (hitBox == null)
                {
                    // try to find a BoxCollider on the fire root/children
                    hitBox = fc.GetComponent<BoxCollider>() ?? fc.GetComponentInChildren<BoxCollider>();
                    // if a box collider exists but the ray didn't hit it, we don't set pointedFire
                    // this ensures sphere collider (warmth area) won't enable "add fuel" prompt
                    if (hitBox != null)
                    {
                        // confirm the raycast actually intersected the box collider bounds (best-effort)
                        // use ClosestPoint to detect whether the hit point lies on/near the box bounds
                        Vector3 closest = hitBox.ClosestPoint(hit.point);
                        if (Vector3.Distance(closest, hit.point) <= 0.01f)
                        {
                            pointedFire = fc;
                        }
                    }
                }
                else
                {
                    // raycast directly hit a BoxCollider
                    pointedFire = fc;
                }
            }
        }

        // Find nearest fire within proximity radius (action fallback only).
        // Only consider fires that expose a BoxCollider (we only allow adding fuel to the box area).
        nearbyFireInRange = FindNearestFireBoxWithinRadius(proximityInteractRadius);
    }

    // Find nearest FireController within radius but only if the fire has a BoxCollider.
    private FireController FindNearestFireBoxWithinRadius(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, interactLayerMask, QueryTriggerInteraction.Collide);
        FireController best = null;
        float bestDist = float.MaxValue;
        foreach (var c in hits)
        {
            if (c == null) continue;
            FireController fc = c.GetComponent<FireController>() ?? c.GetComponentInParent<FireController>() ?? c.GetComponentInChildren<FireController>();
            if (fc == null) continue;

            // get all box colliders on the fire (root + children). pick closest box.
            BoxCollider[] boxes = fc.GetComponentsInChildren<BoxCollider>(includeInactive: false);
            if (boxes == null || boxes.Length == 0) continue;

            // compute distance from player to the closest point on any box collider
            float nearestBoxDist = float.MaxValue;
            foreach (var box in boxes)
            {
                Vector3 closest = box.ClosestPoint(transform.position);
                float d = Vector3.Distance(transform.position, closest);
                if (d < nearestBoxDist) nearestBoxDist = d;
            }

            if (nearestBoxDist < bestDist)
            {
                bestDist = nearestBoxDist;
                best = fc;
            }
        }
        return best;
    }

    private void TryCollectFuel(FuelItem item)
    {
        if (item == null) return;

        float value = item.GetFuelValue();
        float space = maxCarry - carriedFuel;
        if (space <= 0f)
        {
            float duration = 1.25f;
            promptSuppressUntil = Time.time + duration;
            promptUI?.ShowTemporary("Cannot carry more fuel", duration);
            return;
        }

        float collectedAmount = Mathf.Min(value, space);

        bool success = item.CollectFuel();
        if (success)
        {
            carriedFuel += collectedAmount;
            carriedFuel = Mathf.Clamp(carriedFuel, 0f, maxCarry);
            float duration = 1.25f;
            promptSuppressUntil = Time.time + duration;
            promptUI?.HideImmediate();
            promptUI?.ShowTemporary($"+{Mathf.CeilToInt(collectedAmount)} fuel", duration);
            UpdateCarriedUI();
        }
    }

    private void UpdatePromptUI()
    {
        if (promptUI == null) return;

        // Suppress automatic prompt updates while a temporary prompt is being displayed
        if (Time.time < promptSuppressUntil)
            return;

        // SHOW PROMPT ONLY WHEN POINTING at an interactable (fuel or campfire BoxCollider).
        // Nearby fires are allowed as an action fallback when pressing E, but do NOT display the prompt.
        if (pointedFuel != null)
        {
            promptUI.Show($"Press {interactKey} to collect {pointedFuel.fuelType} ({Mathf.CeilToInt(pointedFuel.fuelValue)})");
        }
        else if (pointedFire != null && carriedFuel > 0f)
        {
            promptUI.Show($"Press {interactKey} to add fuel to campfire ({Mathf.CeilToInt(carriedFuel)})");
        }
        else
        {
            promptUI.Hide();
        }
    }

    private void UpdateCarriedUI()
    {
        if (carriedFuelText == null) return;
        carriedFuelText.text = $"Carried Fuel: {Mathf.CeilToInt(carriedFuel)} / {Mathf.CeilToInt(maxCarry)}";
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null)
                playerCamera = Camera.main;
            else
                return;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + dir * maxInteractDistance);
        Gizmos.DrawWireSphere(origin + dir * maxInteractDistance, 0.05f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, proximityInteractRadius);
    }
}