using UnityEngine;
using UnityEngine.UI;

public class StaminaSystem : MonoBehaviour
{
    [Header("References")]
    public TimerScript timer;            // leave empty to auto-find
    public Canvas canvas;                // leave empty to auto-find

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float drainPerSecond = 25f;   // lower = longer sprints
    public float regenPerSecond = 15f;
    public float regenDelay = 1f;        // pause before regenerating
    public float resumeThreshold = 20f;  // must regain this much after hitting 0 to sprint again

    [Header("Meter")]
    public bool showMeter = true;
    public bool hideWhenUnlimited = true;    // bar disappears once the timer hits 0
    public bool hideWhenFull = false;        // true = only shows while stamina is below max
    public Vector2 meterSize = new Vector2(300f, 22f);
    public Vector2 meterOffset = new Vector2(40f, 40f);   // distance from the bottom-left corner
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color fullColor = new Color(0.3f, 1f, 0.4f);
    public Color lowColor = new Color(1f, 0.15f, 0.1f);
    public Color exhaustedFlashColor = new Color(0.6f, 0f, 0f);
    public float flashSpeed = 6f;

    private float stamina;
    private float nextRegenTime;
    private bool exhausted;

    private GameObject meterRoot;
    private RectTransform fillRect;
    private Image fillImage;

    public bool Unlimited => timer != null && timer.IsFinished;
    public float Normalized => maxStamina <= 0f ? 0f : stamina / maxStamina;

    void Start()
    {
        if (timer == null) timer = FindFirstObjectByType<TimerScript>();
        stamina = maxStamina;

        // Plug into the controller (it can't see this script, so we connect from this side)
        var controller = GetComponent<StarterAssets.ThirdPersonController>();
        if (controller != null) controller.SprintFilter = Tick;
        else Debug.LogWarning("StaminaSystem: no ThirdPersonController found on this object.");

        if (showMeter) BuildMeter();
    }

    // Called once per frame by the controller. Returns true if the player may sprint.
    public bool Tick(bool wantsSprint, bool moving)
    {
        bool sprinting;

        if (Unlimited)
        {
            stamina = maxStamina;
            exhausted = false;
            sprinting = wantsSprint && moving;
        }
        else
        {
            sprinting = wantsSprint && moving && !exhausted;

            if (sprinting)
            {
                stamina -= drainPerSecond * Time.deltaTime;
                nextRegenTime = Time.time + regenDelay;

                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhausted = true;
                    sprinting = false;
                }
            }
            else if (Time.time >= nextRegenTime)
            {
                stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * Time.deltaTime);
                if (exhausted && stamina >= resumeThreshold) exhausted = false;
            }
        }

        UpdateMeter();
        return sprinting;
    }

    void BuildMeter()
    {
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("StaminaSystem: no Canvas found, so the meter can't be shown.");
            return;
        }

        // Background bar
        meterRoot = new GameObject("StaminaMeter", typeof(RectTransform), typeof(Image));
        meterRoot.transform.SetParent(canvas.transform, false);
        meterRoot.transform.SetSiblingIndex(0);      // draw behind other UI so panels cover it

        RectTransform bg = meterRoot.GetComponent<RectTransform>();
        bg.anchorMin = bg.anchorMax = bg.pivot = new Vector2(0f, 0f);   // bottom-left
        bg.anchoredPosition = meterOffset;
        bg.sizeDelta = meterSize;

        Image bgImage = meterRoot.GetComponent<Image>();
        bgImage.color = backgroundColor;
        bgImage.raycastTarget = false;

        // Fill bar (width follows stamina by moving its right anchor)
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(meterRoot.transform, false);

        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fill.GetComponent<Image>();
        fillImage.color = fullColor;
        fillImage.raycastTarget = false;
    }

    void UpdateMeter()
    {
        if (meterRoot == null) return;

        bool visible = true;
        if (Unlimited && hideWhenUnlimited) visible = false;
        if (hideWhenFull && !exhausted && stamina >= maxStamina - 0.01f) visible = false;

        if (meterRoot.activeSelf != visible) meterRoot.SetActive(visible);
        if (!visible) return;

        float n = Normalized;
        fillRect.anchorMax = new Vector2(n, 1f);

        if (exhausted)
        {
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
            fillImage.color = Color.Lerp(lowColor, exhaustedFlashColor, flash);
        }
        else
        {
            fillImage.color = Color.Lerp(lowColor, fullColor, n);
        }
    }
}