using UnityEngine;

/// <summary>
/// Makes a Light flicker: constant noise-driven jitter plus occasional short blackouts.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("Minimum intensity as a fraction of the base intensity.")]
    [Range(0f, 1f)] public float minIntensity = 0.6f;

    [Tooltip("How fast the intensity jitters.")]
    public float flickerSpeed = 8f;

    [Header("Blackout Settings")]
    [Tooltip("Average blackouts per second.")]
    public float blackoutChance = 0.15f;

    [Tooltip("Random blackout duration range in seconds.")]
    public Vector2 blackoutDuration = new Vector2(0.05f, 0.25f);

    private Light lightSource;
    private float baseIntensity;
    private float noiseOffset;
    private float blackoutTimer;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        baseIntensity = lightSource.intensity;
        noiseOffset = Random.value * 100f;
    }

    private void Update()
    {
        if (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.deltaTime;
            lightSource.intensity = 0f;
            return;
        }

        if (Random.value < blackoutChance * Time.deltaTime)
        {
            blackoutTimer = Random.Range(blackoutDuration.x, blackoutDuration.y);
        }

        float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerSpeed);
        lightSource.intensity = baseIntensity * Mathf.Lerp(minIntensity, 1f, noise);
    }
}
