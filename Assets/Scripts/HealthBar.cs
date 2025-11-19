using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;               // assign the Slider on this GO
    [SerializeField] private Text healthCounter;          // optional, assign if you have one
    [SerializeField] private PlayerController player;     // assign your Player (has current/maxHealth)

    private void Awake()
    {
        // Fallbacks so we don't NRE if you forgot to assign in Inspector.
        if (slider == null) slider = GetComponent<Slider>();
        if (player == null) player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (player == null || slider == null) return;

        float current = Mathf.Max(0f, player.currentHealth);
        float max = Mathf.Max(0.0001f, player.maxHealth); // prevent division by zero
        slider.value = current / max;

        if (healthCounter != null)
        {
            healthCounter.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }
    }
}
