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
    public float slideSpeed = 20f;

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

        Vector3 target = IsOut ? hiddenPosition + pullOutOffset : hiddenPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 1f - Mathf.Exp(-slideSpeed * Time.deltaTime));
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
