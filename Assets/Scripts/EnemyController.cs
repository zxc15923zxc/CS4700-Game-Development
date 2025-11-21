using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 3;
    int currentHealth;

    public int damage = 5;
    public float damageInter = 1f;
    private float nextDamage = 0f;


    void Start()
    {
	currentHealth = maxHealth;
    }

    public void TakeDamage()
    {
	currentHealth -= 1;
	Debug.Log("Enemy took damage");

	if (currentHealth <= 0)
	{
	    Die();
	}
    }

    private void OnTriggerStay(Collider other)
    {
	if (other.CompareTag("Player") && Time.time >= nextDamage)
	{
	    PlayerController player = other.GetComponent<PlayerController>();
	    if (player != null)
	    {
		player.currentHealth -= damage;
		player.currentHealth = Mathf.Clamp(player.currentHealth, 0, player.maxHealth);
		nextDamage = Time.time + damageInter;
	    }
	}
    }

    void Die()
    {
	Debug.Log("Enemy died!");
	Destroy(gameObject);
    }
}

