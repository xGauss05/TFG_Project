using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class BasicZombie : NetworkBehaviour
{
    public enum ZombieState
    {
        None,
        Idle,
        Chase,
        Melee,
    }

    const int maxHealth = 100;
    bool isDead = false;

    [SerializeField] float loseRadius = 15.0f;
    [SerializeField] float detectionRadius = 10.0f;
    [SerializeField] float movementSpeed = 0.25f;

    NavMeshAgent agent;
    GameObject targetPlayer;

    // Variables that need to be updated in both Clients and Server
    public NetworkVariable<ZombieState> currentState = new NetworkVariable<ZombieState>(ZombieState.Idle);
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = movementSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        CheckState();
        ExecuteState();
    }

    void CheckState()
    {
        GameObject[] foundPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (foundPlayers.Length == 0) return;

        float smallestDistance = float.MaxValue;
        GameObject closestPlayer = null;

        foreach (var player in foundPlayers)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < smallestDistance)
            {
                closestPlayer = player;
            }
        }

        if (closestPlayer == null) return;

        targetPlayer = closestPlayer;

        if (currentState.Value != ZombieState.Chase && smallestDistance < detectionRadius)
        {
            currentState.Value = ZombieState.Chase;
        }
        else if (currentState.Value != ZombieState.Idle && smallestDistance > loseRadius)
        {
            agent.SetDestination(transform.position);
            currentState.Value = ZombieState.Idle;
        }
    }

    void ExecuteState()
    {
        // Zombie finite state machine
        switch (currentState.Value)
        {
            case ZombieState.Idle:
                DoIdle();
                break;
            case ZombieState.Chase:
                DoChase();
                break;
            case ZombieState.Melee:
                DoMelee();
                break;
            case ZombieState.None:
            default:
                Debug.LogError("Basic Zombie unknown state.");
                break;
        }
    }

    void DoChase()
    {
        // Check if there is a target player, for safety measures
        if (targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.transform.position);
        }
    }

    void DoIdle()
    {
        // Trigger idle animation
    }

    void DoMelee()
    {
        // Trigger Punch animation & check whether the Zombie hit the Player
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int amount)
    {
        if (currentHealth.Value <= 0 || isDead) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            isDead = true;
            // Trigger here the Death animation. On animation end, should despawn the NetworkObject
            NetworkObject.Despawn();
        }
    }
}
