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
    public float moveSpeed = 2f;             // angels can be faster than normal enemies since you can freeze them
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

    [Header("Weeping Angel (freezes when seen while timer > 0)")]
    public bool weepingAngel = true;         // untick to make this a normal enemy
    public float viewDistance = 80f;         // beyond this the player can't "see" it
    [Range(0f, 0.45f)]
    public float screenMargin = 0f;          // 0 = anywhere on screen counts, higher = must be nearer the center
    public LayerMask lineOfSightMask = ~0;   // what blocks the player's view (terrain, house, etc.)
    public bool freezeAnimation = true;      // pauses the enemy's animator while frozen

    private NavMeshAgent agent;
    private Animator anim;
    private Renderer[] renderers;
    private Camera cam;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    private bool chasing;
    private bool frozen;
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

        anim = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<Renderer>();
        cam = Camera.main;
    }

    void Update()
    {
        if (player == null) return;
        if (gameOver != null && gameOver.IsOver) return;   // stop everything on game over

        bool doomsday = timer != null && timer.IsFinished;
        float currentSpeed = doomsday ? moveSpeed * speedMultiplierAtZero : moveSpeed;

        float dist = Vector3.Distance(transform.position, player.position);

        // Weeping Angel: frozen while seen, but only before the timer hits 0
        frozen = weepingAngel && !doomsday && IsSeenByPlayer();
        if (freezeAnimation && anim != null) anim.speed = frozen ? 0f : 1f;

        // Start / stop chasing
        if (doomsday && alwaysChaseAtZero)
            chasing = true;
        else if (!chasing && dist <= detectionRange)
            chasing = true;
        else if (chasing && loseRange > 0f && dist > loseRange)
            chasing = false;

        if (!chasing)
        {
            StopAgent();
            return;
        }

        // Frozen when seen, or briefly after a hit (the hit pause is skipped at 0, no mercy)
        bool paused = frozen || (!doomsday && Time.time < resumeTime);

        if (!paused)
            MoveTowardPlayer(currentSpeed);
        else
            StopAgent();

        // Contact check (a frozen angel can't hurt you)
        if (dist <= hitRange)
        {
            if (doomsday)
            {
                // Timer is at 0: touching the player kills them
                if (gameOver != null) gameOver.TriggerGameOver();
                return;
            }

            if (!frozen && Time.time >= nextHitTime)
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
            if (!agent.isOnNavMesh) return;
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

    void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }

    // ---------- Weeping Angel vision ----------

    bool IsSeenByPlayer()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return false;
        }

        Bounds b = GetBounds();

        if (Vector3.Distance(cam.transform.position, b.center) > viewDistance)
            return false;

        // Check the middle, head and feet so peeking at any part of it counts
        float h = b.extents.y * 0.9f;
        return CanSeePoint(b.center)
            || CanSeePoint(b.center + Vector3.up * h)
            || CanSeePoint(b.center - Vector3.up * h);
    }

    bool CanSeePoint(Vector3 point)
    {
        // Is the point inside the camera's view?
        Vector3 vp = cam.WorldToViewportPoint(point);
        if (vp.z <= 0f) return false;                                    // behind the camera
        if (vp.x < screenMargin || vp.x > 1f - screenMargin) return false;
        if (vp.y < screenMargin || vp.y > 1f - screenMargin) return false;

        // Is anything solid in the way?
        Vector3 origin = cam.transform.position;
        Vector3 toPoint = point - origin;
        float d = toPoint.magnitude;
        if (d < 0.01f) return true;

        int count = Physics.RaycastNonAlloc(origin, toPoint / d, hits, d, lineOfSightMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform t = hits[i].transform;
            if (t.IsChildOf(transform)) continue;                        // the enemy itself
            if (player != null && t.IsChildOf(player)) continue;         // the player's own body
            return false;                                                // something else blocks the view
        }
        return true;
    }

    Bounds GetBounds()
    {
        if (renderers == null || renderers.Length == 0)
            return new Bounds(transform.position + Vector3.up, new Vector3(1f, 2f, 1f));

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
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