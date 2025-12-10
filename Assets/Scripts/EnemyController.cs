using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Damage")]
    public int damage = 20;
    public float damageInter = 1f;
    private float nextDamage = 0f;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float chaseRange = 25f;
    public float stopDistance = 1.5f;

    private Transform player;

    void Start()
    {
	currentHealth = maxHealth;
	player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
	ChasePlayer();
    }

    void ChasePlayer()
    {
	if (player == null) return;

	float distance = Vector3.Distance(transform.position, player.position);

	// Only chase if within range
	if (distance <= chaseRange)
	{
	    // Face the player
	    Vector3 direction = (player.position - transform.position).normalized;
	    direction.y = 0; // keep enemy upright
	    transform.rotation = Quaternion.LookRotation(direction);

	    // Move toward the player until close enough to attack
	    if (distance > stopDistance)
	    {
		transform.position += direction * moveSpeed * Time.deltaTime;
	    }
	}
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
	    PlayerController playerController = other.GetComponent<PlayerController>();
	    if (playerController != null)
	    {
		playerController.currentHealth -= damage;
		playerController.currentHealth = Mathf.Clamp(playerController.currentHealth, 0, playerController.maxHealth);
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
