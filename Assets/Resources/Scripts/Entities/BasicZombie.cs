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

    [Header("Zombie Parameters")]
    [SerializeField] float loseRadius = 15.0f;
    [SerializeField] float detectionRadius = 10.0f;
    [SerializeField] float meleeRadius = 2.0f;
    [SerializeField] float movementSpeed = 0.25f;
    public int attackDamage = 5;
    const int maxHealth = 30;

    [Header("Zombie Animator")]
    [SerializeField] Animator zombieAnimator;

    [Header("Zombie Audios")]
    [SerializeField] AudioClip zombieAttackSfx;
    [SerializeField] AudioClip zombieScreechSfx;
    [SerializeField] AudioClip zombieDeathSfx;

    [Header("Zombie Network variables")]
    // Variables that need to be updated in both Clients and Server
    public NetworkVariable<ZombieState> currentState = new NetworkVariable<ZombieState>(ZombieState.Idle);
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);

    [Header("Zombie Melee properties")]
    [SerializeField] GameObject meleeHitboxPrefab;
    [SerializeField] Transform meleeSpawnpoint;

    // Flags for logic handling
    bool zombieSpawned = false;
    bool isAttacking = false;
    bool isDead = false;

    // Helpers and Components
    NavMeshAgent agent;
    GameObject targetPlayer;
    AudioSource audioSource;

    public override void OnNetworkSpawn()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("ZombieSpawnpoint");

        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnTransform = spawnPoints[randomIndex].transform;

            // Set the Zombie spawn point to the Spawnpoint position
            transform.position = spawnTransform.position;
            transform.rotation = spawnTransform.rotation;
        }
        else
        {
            Debug.LogWarning("No zombie spawn points found. Spawning at default position.");
            this.transform.position = new Vector3(0, 0, 0); // Fallback position
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = movementSpeed;

        audioSource = GetComponent<AudioSource>();

        StartCoroutine(WaitForSpawnAnimation());
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsHost || !zombieSpawned || isDead) return;

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
                smallestDistance = distance;
                closestPlayer = player;
            }
        }

        if (closestPlayer == null) return;

        targetPlayer = closestPlayer;

        if (smallestDistance < meleeRadius)
        {
            currentState.Value = ZombieState.Melee;
        }
        else if (currentState.Value != ZombieState.Chase && smallestDistance < detectionRadius)
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
            if (!CheckAnimationState("Chase")) zombieAnimator.SetTrigger("Chase");

            agent.SetDestination(targetPlayer.transform.position);
            //Debug.Log("Basic Zombie Chase");
        }
    }

    void DoIdle()
    {
        if (!CheckAnimationState("Idle")) zombieAnimator.SetTrigger("Idle");

        //Debug.Log("Basic Zombie Idle");
    }

    void DoMelee()
    {
        agent.ResetPath();
        if (!CheckAnimationState("Attack1") && !isAttacking)
        {
            if (IsServer) StartCoroutine(SpawnMeleeHitbox());

            zombieAnimator.SetTrigger("Attack1");
        }
        //Debug.Log("Basic Zombie Attack");
    }

    IEnumerator SpawnMeleeHitbox()
    {
        isAttacking = true;

        GameObject currentHitbox = Instantiate(meleeHitboxPrefab, meleeSpawnpoint.position, meleeSpawnpoint.rotation);
        currentHitbox.GetComponent<NetworkObject>().Spawn();

        currentHitbox.GetComponent<ZombieDamageHitbox>().attacker = this;

        AnimatorStateInfo animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);

        while (!animatorStateInfo.IsName("Attack1"))
        {
            yield return null;
            animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        }

        yield return new WaitForSeconds(animatorStateInfo.length);

        if (currentHitbox != null)
        {
            if (currentHitbox.TryGetComponent(out NetworkObject networkObj))
            {
                networkObj.Despawn();
            }
            else
            {
                Destroy(currentHitbox);
            }
        }

        isAttacking = false;
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int amount)
    {
        if (currentHealth.Value <= 0 || isDead) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            isDead = true;
            if (!CheckAnimationState("Death")) zombieAnimator.SetTrigger("Death");

            //Debug.Log("Basic Zombie Death");

            agent.ResetPath();
            audioSource.PlayOneShot(zombieDeathSfx);

            StartCoroutine(WaitForDeathAnimation());
        }
    }

    IEnumerator WaitForDeathAnimation()
    {
        AnimatorStateInfo animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);

        while (!animatorStateInfo.IsName("Death"))
        {
            yield return null;
            animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        }

        yield return new WaitForSeconds(animatorStateInfo.length);

        NetworkObject.Despawn();
    }

    IEnumerator WaitForSpawnAnimation()
    {
        AnimatorStateInfo animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);

        while (!animatorStateInfo.IsName("Spawn"))
        {
            yield return null;
            animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        }

        yield return new WaitForSeconds(animatorStateInfo.length);

        zombieSpawned = true;
    }

    bool CheckAnimationState(string animName)
    {
        AnimatorStateInfo animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);

        return animatorStateInfo.IsName(animName);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.DrawWireSphere(transform.position, loseRadius);
        Gizmos.DrawWireSphere(transform.position, meleeRadius);
    }
}
