using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BasicZombie : NetworkBehaviour, IDamageable
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
    const int maxHealth = 50;
    [SerializeField] int score = 50;
    [SerializeField] float despawnRadius = 200.0f;

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
    [SerializeField] float meleeDelay = 0.0f;
    [SerializeField] GameObject meleeHitboxPrefab;
    [SerializeField] Transform meleeSpawnpoint;

    [Header("Zombie Particles")]
    [SerializeField] GameObject ps_bloodSplatter;

    // Flags for logic handling
    bool zombieSpawned = false;
    bool isAttacking = false;
    bool isDead = false;
    public bool aggressive = false;

    // Helpers and Components
    NavMeshAgent agent;
    GameObject targetPlayer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = movementSpeed;

        StartCoroutine(WaitForSpawnAnimation());
    }

    void Update()
    {
        if (!IsHost || !zombieSpawned || isDead) return;

        DespawnChecker();
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

        if (aggressive)
        {
            if (smallestDistance < meleeRadius)
            {
                currentState.Value = ZombieState.Melee;
            }
            else if (currentState.Value != ZombieState.Chase && currentState.Value != ZombieState.Melee)
            {
                currentState.Value = ZombieState.Chase;
            }
        }
        else
        {
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
            zombieAnimator.SetTrigger("Attack1");

            if (IsServer) StartCoroutine(SpawnMeleeHitbox());
        }
        //Debug.Log("Basic Zombie Attack");
    }

    IEnumerator SpawnMeleeHitbox()
    {
        isAttacking = true;

        yield return new WaitForSeconds(meleeDelay);

        GameObject currentHitbox = Instantiate(meleeHitboxPrefab, meleeSpawnpoint.position, meleeSpawnpoint.rotation);
        currentHitbox.GetComponent<NetworkObject>().Spawn();

        currentHitbox.GetComponent<ZombieDamageHitbox>().attacker = this;

        PlayZombieAttackSFXClientRpc();

        AnimatorStateInfo animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        while (!animatorStateInfo.IsName("Attack1"))
        {
            yield return null;
            animatorStateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        }

        yield return new WaitForSeconds(animatorStateInfo.length - meleeDelay);

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
        currentState.Value = ZombieState.Idle;
        if (!CheckAnimationState("Idle")) zombieAnimator.SetTrigger("Idle");
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

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    void ApplyDamage(int damage)
    {
        if (currentHealth.Value <= 0 || isDead) return;

        if (!aggressive) aggressive = true;

        currentHealth.Value -= damage;
        SpawnBloodEffectClientRpc();

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            if (!CheckAnimationState("Death"))
            {
                zombieAnimator.SetTrigger("Death");

                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
            }

            DisableCollidersClientRpc();
            //Debug.Log("Basic Zombie Death");

            agent.ResetPath();
            PlayZombieDeathSFXClientRpc();
            if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);

            StartCoroutine(WaitForDeathAnimation());
        }
    }

    void DespawnChecker()
    {
        if (IsServer)
        {
            GameObject[] foundPlayers = GameObject.FindGameObjectsWithTag("Player");
            if (foundPlayers.Length == 0) return;

            float smallestDistance = float.MaxValue;

            foreach (var player in foundPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                }
            }

            if (smallestDistance > despawnRadius)
            {
                this.NetworkObject.Despawn();
            }
        }
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void PlayZombieAttackSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(zombieAttackSfx);
    }

    [ClientRpc]
    void PlayZombieScreechSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(zombieScreechSfx);
    }

    [ClientRpc]
    void PlayZombieDeathSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(zombieDeathSfx);
    }

    [ClientRpc]
    void SpawnBloodEffectClientRpc()
    {
        if (ps_bloodSplatter == null) return;

        GameObject blood = Instantiate(ps_bloodSplatter, transform.position + Vector3.up * 1.0f, Quaternion.identity);
        Destroy(blood, 0.5f);
    }

    [ClientRpc]
    void DisableCollidersClientRpc()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        ApplyDamage(damage);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.DrawWireSphere(transform.position, loseRadius);
        Gizmos.DrawWireSphere(transform.position, meleeRadius);
    }

}
