using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{

    private Slider slider;
    public Text healthCounter;

    public GameObject playerState;

    private float currentHealth, maxhealth;


    // Start is called before the first frame update
    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        currentHealth = playerState.GetComponent<PlayerState>().currentHealth;
        maxhealth = playerState.GetComponent<PlayerState>().maxHealth;

        float fillValue = currentHealth / maxhealth;
        slider.value = fillValue;

        healthCounter.text = currentHealth + " / "  + maxhealth;
    }
}
