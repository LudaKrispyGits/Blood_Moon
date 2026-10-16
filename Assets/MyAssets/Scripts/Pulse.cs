using UnityEngine;

[RequireComponent(typeof(Light))]
public class PulsingLight : MonoBehaviour
{
    private Light myLight;

    [Header("Pulse Settings")]
    public float minIntensity = 0.1f;
    public float maxIntensity = 2.5f;
    public float pulseSpeed = 2.0f;

    void Start()
    {
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        // Calculate a wave value that oscillates smoothly between 0 and 1
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;
        
        // Interpolate the light intensity based on the wave value
        myLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);
    }
}
