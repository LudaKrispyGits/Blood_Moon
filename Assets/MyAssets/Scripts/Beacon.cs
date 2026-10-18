using UnityEngine;

public class Beacon : MonoBehaviour
{
    [Header("Win")]
    public Transform player;                // leave empty to auto-find by tag
    public string playerTag = "Player";
    public float winRadius = 2f;            // how close the player must get
    public GameOverManager gameManager;

    [Header("Trigger")]
    public TimerScript timer;               // leave empty to auto-find

    [Header("Look")]
    public Color beaconColor = new Color(1f, 0.15f, 0.1f);
    public float height = 200f;             // how tall the beam is
    public float width = 1.5f;              // how thick the beam is
    [Range(0f, 1f)] public float beamAlpha = 0.35f;
    public float fadeInTime = 3f;           // seconds to fade in

    [Header("Light")]
    public float lightIntensity = 8f;
    public float lightRange = 40f;
    public float lightHeight = 3f;          // how far above the base the light sits
    public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float pulseAmount = 0.3f;

    [Header("Placement")]
    public bool snapToGround = true;
    public LayerMask groundMask = ~0;

    [Header("Optional")]
    public GameObject customVisuals;        // your own beam/prefab to enable instead (must be inactive and NOT this object)

    private GameObject beam;
    private Renderer beamRenderer;
    private Light beaconLight;
    private bool active;
    private float fade;

    void Start()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameOverManager>();
            if (player == null)
            {
                 GameObject p = GameObject.FindGameObjectWithTag(playerTag);
                 if (p != null) player = p.transform;
            }

        if (timer == null) timer = FindFirstObjectByType<TimerScript>();

        if (snapToGround)
        {
            Vector3 origin = transform.position + Vector3.up * 500f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
                transform.position = hit.point;
        }

        if (customVisuals != null)
        {
            customVisuals.SetActive(false);
            return;
        }

        BuildBeam();
        SetVisible(false);
    }

    void Update()
    {   
        if (player != null && gameManager != null && !gameManager.IsOver)
{
    Vector3 flat = player.position - transform.position;
    flat.y = 0f;                                   // ignore height differences
    if (flat.magnitude <= winRadius)
    {
        gameManager.TriggerWin();
        return;
    }
}
        if (!active)
        {
            if (timer != null && timer.IsFinished) Activate();
            return;
        }

        if (customVisuals != null) return;

        fade = fadeInTime <= 0f ? 1f : Mathf.Clamp01(fade + Time.deltaTime / fadeInTime);
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        Color c = beaconColor;
        c.a = beamAlpha * fade * Mathf.Lerp(1f, pulse, 0.5f);
        beamRenderer.material.color = c;

        beaconLight.intensity = lightIntensity * fade * pulse;
    }

    void Activate()
    {
        active = true;
        if (customVisuals != null) customVisuals.SetActive(true);
        else SetVisible(true);
    }

    void BuildBeam()
    {
        beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "BeaconBeam";
        Destroy(beam.GetComponent<Collider>());     // no collision, player walks through it
        beam.transform.SetParent(transform, false);
        // Default cylinder is 2 units tall, so scale Y = height / 2
        beam.transform.localScale = new Vector3(width, height / 2f, width);
        beam.transform.localPosition = new Vector3(0f, height / 2f, 0f);

        beamRenderer = beam.GetComponent<Renderer>();
        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;

        Shader s = Shader.Find("Sprites/Default");  // unlit + supports transparency in any pipeline
        beamRenderer.material = new Material(s);

        GameObject lightObj = new GameObject("BeaconLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, lightHeight, 0f);
        beaconLight = lightObj.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = beaconColor;
        beaconLight.range = lightRange;
        beaconLight.intensity = 0f;
        beaconLight.shadows = LightShadows.None;
    }

    void SetVisible(bool visible)
    {
        if (beam != null) beam.SetActive(visible);
        if (beaconLight != null) beaconLight.gameObject.SetActive(visible);
    }
    void OnDrawGizmosSelected()
{
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, winRadius);
}
}