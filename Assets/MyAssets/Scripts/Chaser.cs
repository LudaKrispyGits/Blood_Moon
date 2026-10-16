using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                 // leave empty to auto-find by tag
    public string playerTag = "Player";
    public TimerScript timer;                // leave empty to auto-find
    public GameOverManager gameOver;         // leave empty to auto-find

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

    [Header("When Timer Hits 0")]
    public float speedMultiplierAtZero = 2.5f;   // enemy speed gets multiplied by this
    public bool alwaysChaseAtZero = false;       // true = hunts the player from anywhere

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

        if (gameOver == null)
            gameOver = FindFirstObjectByType<GameOverManager>();

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
        if (gameOver != null && gameOver.IsOver) return;   // stop everything on game over

        bool doomsday = timer != null && timer.IsFinished;
        float currentSpeed = doomsday ? moveSpeed * speedMultiplierAtZero : moveSpeed;

        float dist = Vector3.Distance(transform.position, player.position);

        // Start / stop chasing
        if (doomsday && alwaysChaseAtZero)
            chasing = true;
        else if (!chasing && dist <= detectionRange)
            chasing = true;
        else if (chasing && loseRange > 0f && dist > loseRange)
            chasing = false;

        if (!chasing)
        {
            if (agent != null) agent.ResetPath();
            return;
        }

        // Brief freeze after hitting the player (skipped at 0, no mercy)
        bool paused = !doomsday && Time.time < resumeTime;

        if (!paused)
            MoveTowardPlayer(currentSpeed);
        else if (agent != null)
            agent.ResetPath();

        // Contact check
        if (dist <= hitRange)
        {
            if (doomsday)
            {
                // Timer is at 0: touching the player kills them
                if (gameOver != null) gameOver.TriggerGameOver();
                return;
            }

            if (Time.time >= nextHitTime)
            {
                nextHitTime = Time.time + hitCooldown;
                resumeTime = Time.time + pauseAfterHit;

                if (timer != null) timer.RemoveTime(timePenalty);
            }
        }
    }

    void MoveTowardPlayer(float speed)
    {
        if (agent != null)
        {
            agent.speed = speed;
            agent.SetDestination(player.position);
            return;
        }

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        transform.position += dir.normalized * speed * Time.deltaTime;
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