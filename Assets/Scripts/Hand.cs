using UnityEngine;

/// <summary>
/// The writing hand. Its pivot is the pen tip. Snaps toward the target
/// position with a small hop, so every word feels like a pen stroke.
/// When stamina runs out, the hand shakes and tires from exhaustion.
/// Includes audio placeholders for pencil/pen scratching sounds.
/// </summary>
public class Hand : MonoBehaviour
{
    public float followSpeed = 30f;
    public float hopHeight = 0.015f;
    public float hopSettleSpeed = 25f;

    [Header("Exhaustion Feedback")]
    public float exhaustionTremor = 0.0025f;

    [Header("Cheating Tremble")]
    [Tooltip("Nervous tremble while the cheat sheet is out. 0 = none, 1 = strong.")]
    [Range(0f, 1f)]
    public float trembleIntensity = 0.5f;
    public float trembleSpeed = 14f;
    public AnswerPaper answerPaper;

    [Header("Audio Settings (Placeholders)")]
    [Tooltip("AudioSource component. Auto-created if null.")]
    public AudioSource audioSource;
    [Tooltip("Primary sound clip for pen writing / scratch.")]
    public AudioClip writingClip;
    [Tooltip("Optional multiple writing clips for random scratch variation.")]
    public AudioClip[] writingClips;
    [Range(0f, 1f)]
    public float soundVolume = 0.75f;

    private Vector3 target;
    private Vector3 position;
    private float hop = 0f;
    private float exhaustedKick = 0f;
    private float tremble = 0f;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.3f;
    }

    void Start()
    {
        position = transform.position;
        target = position;
        if (answerPaper == null)
        {
            answerPaper = FindAnyObjectByType<AnswerPaper>();
        }
    }

    public void MoveTo(Vector3 point)
    {
        target = point;
        hop = 1f;
        PlayWritingSound();
    }

    /// <summary>
    /// Triggers a twitch/shake feedback when clicking while exhausted.
    /// </summary>
    public void TriggerExhaustedShake()
    {
        exhaustedKick = 1f;
    }

    private void PlayWritingSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = writingClip;
        if (writingClips != null && writingClips.Length > 0)
        {
            clipToPlay = writingClips[Random.Range(0, writingClips.Length)];
        }

        if (clipToPlay != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.15f);
            audioSource.PlayOneShot(clipToPlay, soundVolume);
        }
    }

    void Update()
    {
        position = Vector3.Lerp(position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        hop = Mathf.Lerp(hop, 0f, 1f - Mathf.Exp(-hopSettleSpeed * Time.deltaTime));
        exhaustedKick = Mathf.Lerp(exhaustedKick, 0f, 1f - Mathf.Exp(-15f * Time.deltaTime));

        Vector3 fatigueOffset = Vector3.zero;
        if (StaminaSystem.Instance != null)
        {
            if (StaminaSystem.Instance.IsExhausted)
            {
                // Continuous tired trembling & droop when exhausted
                fatigueOffset = Random.insideUnitSphere * exhaustionTremor;
                fatigueOffset += Vector3.down * 0.004f;
            }
            else if (StaminaSystem.Instance.NormalizedStamina < 0.25f)
            {
                // Subtle twitching when running low on stamina
                fatigueOffset = Random.insideUnitSphere * (exhaustionTremor * 0.4f);
            }
        }

        if (exhaustedKick > 0.01f)
        {
            fatigueOffset += (Random.insideUnitSphere * 0.006f) * exhaustedKick;
        }

        // Fade the nervous tremble in and out with the cheat sheet
        bool cheating = answerPaper != null && answerPaper.IsOut;
        tremble = Mathf.MoveTowards(tremble, cheating ? trembleIntensity : 0f, Time.deltaTime * 3f);
        float t = Time.time * trembleSpeed;
        Vector3 trembleOffset = new Vector3(
            Mathf.PerlinNoise(t, 0f) - 0.5f,
            Mathf.PerlinNoise(t, 3f) - 0.5f,
            Mathf.PerlinNoise(t, 6f) - 0.5f) * (0.006f * tremble);

        transform.position = position + Vector3.up * hop * hopHeight + fatigueOffset + trembleOffset;
    }
}
