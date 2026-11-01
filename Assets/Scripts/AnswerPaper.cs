using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The cheat sheet hidden under the desk. Right mouse button slides it out or back.
/// Includes audio placeholders for paper sliding in and out.
/// </summary>
public class AnswerPaper : MonoBehaviour
{
    public TextAsset answers;
    public TMP_Text text;
    public Vector3 pullOutOffset = new Vector3(0f, 0f, -0.4f);

    [Header("Animation")]
    public float pullOutDuration = 0.45f;
    public float tuckInDuration = 0.3f;
    [Tooltip("Tilt (x), turn (y) and roll (z) in degrees reached mid-slide.")]
    public Vector3 slideTilt = new Vector3(-6f, 8f, 3f);
    [Tooltip("Angle the paper rests at once pulled out.")]
    public Vector3 restTilt = new Vector3(0f, -5f, 0f);

    [Header("Tremble")]
    [Tooltip("Nervous tremble while the paper is held out. 0 = none, 1 = strong.")]
    [Range(0f, 1f)]
    public float trembleIntensity = 0.5f;
    public float trembleSpeed = 14f;

    [Header("Audio Settings (Placeholders)")]
    [Tooltip("AudioSource component. Auto-created if null.")]
    public AudioSource audioSource;
    [Tooltip("Audio clip played when sliding the paper out (showing cheat sheet).")]
    public AudioClip paperSlideOutClip;
    [Tooltip("Audio clip played when sliding the paper in (hiding cheat sheet).")]
    public AudioClip paperSlideInClip;
    [Tooltip("Generic paper rustle clip used if in/out clips are not assigned.")]
    public AudioClip paperRustleClip;
    [Range(0f, 1f)]
    public float soundVolume = 0.85f;

    public bool IsOut { get; private set; }

    private Vector3 hiddenPosition;
    private Quaternion hiddenRotation;
    private float progress;

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
        audioSource.spatialBlend = 0.3f; // Near-field 3D audio
    }

    void Start()
    {
        hiddenPosition = transform.localPosition;
        hiddenRotation = transform.localRotation;
        if (answers != null && text != null)
        {
            text.text = answers.text;
        }
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            IsOut = !IsOut;
            PlaySlideSound(IsOut);
        }

        float duration = IsOut ? pullOutDuration : tuckInDuration;
        progress = Mathf.MoveTowards(progress, IsOut ? 1f : 0f, Time.deltaTime / duration);

        // Ease in and out of the slide; the tilt peaks mid-way.
        float eased = progress * progress * (3f - 2f * progress);
        float arc = Mathf.Sin(progress * Mathf.PI);

        float t = Time.time * trembleSpeed;
        float tremble = trembleIntensity * eased;
        Vector3 trembleOffset = new Vector3(Noise(t, 0f), 0f, Noise(t, 3f)) * (0.004f * tremble);
        Vector3 trembleAngle = new Vector3(Noise(t, 6f), Noise(t, 9f), Noise(t, 12f)) * (3f * tremble);

        transform.localPosition = hiddenPosition + pullOutOffset * eased + trembleOffset;
        transform.localRotation = hiddenRotation * Quaternion.Euler(restTilt * eased + slideTilt * arc + trembleAngle);
    }

    private static float Noise(float t, float seed)
    {
        return (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f;
    }

    private void PlaySlideSound(bool slidingOut)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = slidingOut ? paperSlideOutClip : paperSlideInClip;
        if (clipToPlay == null)
        {
            clipToPlay = paperRustleClip;
        }

        if (clipToPlay != null)
        {
            audioSource.pitch = Random.Range(0.92f, 1.08f);
            audioSource.PlayOneShot(clipToPlay, soundVolume);
        }
    }
}
