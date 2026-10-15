using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                 // leave empty to auto-find by tag
    public string playerTag = "Player";
    public TimerScript timer;                // leave empty to auto-find

    [Header("Detection")]
    public float detectionRange = 15f;       // starts chasing inside this distance
    public float loseRange = 25f;            // gives up beyond this (0 = never gives up)

    [Header("Movement")]
    public float moveSpeed = 2f;             // keep this slower than the player
    public float turnSpeed = 5f;
    public bool useNavMesh = false;          // needs a baked NavMesh + NavMeshAgent

    [Header("Attack")]
    public float hitRange = 1.5f;            // how close counts as "connecting"
    public float timePenalty = 10f;          // seconds removed from the timer
    public float hitCooldown = 2f;           // delay between hits
    public float pauseAfterHit = 0.75f;      // enemy freezes briefly after a hit

    private NavMeshAgent agent;
    private bool chasing;
    private float nextHitTime;
    private float resumeTime;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        if (timer == null)
            timer = FindFirstObjectByType<TimerScript>();

        if (useNavMesh)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
        }
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Start / stop chasing
        if (!chasing && dist <= detectionRange)
            chasing = true;
        else if (chasing && loseRange > 0f && dist > loseRange)
            chasing = false;

        if (!chasing)
        {
            if (agent != null) agent.ResetPath();
            return;
        }

        // Brief freeze after hitting the player
        bool paused = Time.time < resumeTime;

        if (!paused)
            MoveTowardPlayer();
        else if (agent != null)
            agent.ResetPath();

        // Contact check
        if (dist <= hitRange && Time.time >= nextHitTime)
        {
            nextHitTime = Time.time + hitCooldown;
            resumeTime = Time.time + pauseAfterHit;

            if (timer != null) timer.RemoveTime(timePenalty);
        }
    }

    void MoveTowardPlayer()
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
            return;
        }

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }

    // Shows the ranges in the Scene view when the enemy is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (loseRange > 0f)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, loseRange);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}