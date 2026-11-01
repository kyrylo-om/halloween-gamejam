using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// States for the Teacher patrol AI.
/// </summary>
public enum TeacherState
{
    Walking,
    WaitingAtPoint,
    Suspecting
}

/// <summary>
/// Controls the Teacher capsule:
/// - Follows a developer-editable SplinePath.
/// - Stops at each waypoint for 5 seconds to scan the classroom.
/// - When she starts suspecting the player, she freezes the countdown and turns to stare directly at the player.
/// - Once the player hides the cheat sheet (stops suspecting), the countdown resumes on the point.
/// - 2 Vision Cones: Instant Spot Cone (Inner) and Suspicion Cone (Outer).
/// - Includes audio placeholders for 3D footsteps and alert stings.
/// </summary>
public class TeacherController : MonoBehaviour
{
    [Header("Patrol Path")]
    [Tooltip("The SplinePath that the teacher patrols along.")]
    public SplinePath path;
    public float walkSpeed = 1.3f;
    public float turnSpeed = 6f;

    [Header("Waypoint Pause & Look Around")]
    [Tooltip("Seconds the teacher stops at each spline point to scan the classroom.")]
    public float waypointWaitDuration = 5f;
    [Tooltip("How far left and right the teacher turns her gaze while looking around (in degrees).")]
    public float lookAroundAngle = 55f;
    [Tooltip("Speed of gaze sweep while scanning the classroom.")]
    public float lookAroundSpeed = 1.2f;
    [Tooltip("Rotation speed when turning to stare at the player during suspicion.")]
    public float alertTurnSpeed = 9f;

    [Header("Player & Eyes")]
    [Tooltip("The player transform (Camera). Auto-detected if null.")]
    public Transform playerTarget;
    [Tooltip("Eye / head position where vision cones originate. Leave empty to auto-create at Default Eye Offset.")]
    public Transform eyes;
    public Vector3 defaultEyeOffset = new Vector3(0f, 0.7f, 0.25f);

    [Header("Cheat Sheet Reference")]
    public AnswerPaper answerPaper;

    [Header("1. Instant Spot Cone (Inner Cone)")]
    [Tooltip("Within this distance and angle, cheating is instantly spotted.")]
    public float instantSpotRange = 3.5f;
    [Range(10f, 160f)]
    public float instantSpotAngle = 55f;

    [Header("2. Suspicion Cone (Longer Outer Cone)")]
    [Tooltip("Within this longer range and angle, cheating fills up the suspicion bar.")]
    public float suspicionRange = 8.5f;
    [Range(10f, 160f)]
    public float suspicionAngle = 75f;
    [Tooltip("Percentage suspicion added per second when cheating in sight.")]
    public float suspicionFillRate = 35f;
    [Tooltip("Percentage suspicion drained per second when hiding cheat sheet or out of sight.")]
    public float suspicionDrainRate = 22f;

    [Header("Line of Sight & Layers")]
    public LayerMask obstacleMask = ~0;

    [Header("In-Game Cone Visualizer")]
    public bool showInGameVisionCones = true;
    [Range(0.05f, 0.4f)]
    public float coneAlpha = 0.18f;

    [Header("Post-Processing Feedback")]
    [Tooltip("Global Volume whose Film Grain & Vignette react to the teacher. Auto-detected if null.")]
    public Volume postVolume;
    [Tooltip("Distance at which film grain reaches its strongest intensity.")]
    public float grainNearDistance = 1.5f;
    [Tooltip("Distance beyond which film grain stays at its base intensity.")]
    public float grainFarDistance = 8f;
    [Range(0f, 1f)]
    public float grainBaseIntensity = 0.35f;
    [Range(0f, 1f)]
    public float grainNearIntensity = 1f;
    [Tooltip("Vignette intensity while the teacher is suspicious.")]
    [Range(0f, 1f)]
    public float suspicionVignetteIntensity = 0.8f;
    [Tooltip("How fast the vignette intensity blends in/out (per second).")]
    public float vignetteBlendSpeed = 4f;

    [Header("Audio Settings (Placeholders)")]
    [Tooltip("AudioSource component for 3D footsteps. Auto-created if null.")]
    public AudioSource audioSource;
    [Tooltip("Primary footstep audio clip.")]
    public AudioClip footstepClip;
    [Tooltip("Optional multiple footstep clips for step variation.")]
    public AudioClip[] footstepClips;
    [Tooltip("Time in seconds between footsteps while walking.")]
    public float footstepInterval = 0.55f;
    [Range(0f, 1f)]
    public float footstepVolume = 0.85f;
    [Tooltip("Optional sting played when teacher starts suspecting.")]
    public AudioClip suspicionAlertClip;
    [Tooltip("Optional sting played when caught red-handed.")]
    public AudioClip caughtStingClip;

    [Header("HUD")]
    public Font uiFont;

    // Runtime state properties
    public bool IsPlayerCheating => answerPaper != null && answerPaper.IsOut;
    public TeacherState CurrentState { get; private set; } = TeacherState.Walking;
    public float WaypointWaitTimer => waitTimer;
    public float CurrentSuspicion { get; private set; }
    public bool IsSuspectingPlayer { get; private set; }
    public bool IsGameOver { get; private set; }
    public string GameOverReason { get; private set; } = "";

    // Internal navigation & look-around state
    private int currentSegmentIndex = 0;
    private float currentSegmentProgress = 0f;
    private float currentSegmentLength = 1f;
    private float waitTimer = 0f;
    private float lookAroundTimer = 0f;
    private float stepTimer = 0f;
    private bool wasSuspectingLastFrame = false;
    private Quaternion baseWaypointRotation;

    private Animator animator;
    private Vector3 lastPosition;

    private float displaySuspicion = 0f;
    private Texture2D whiteTexture;
    private GUIStyle headerStyle;
    private GUIStyle barStyle;
    private GUIStyle gameOverStyle;
    private GUIStyle subStyle;
    private GUIStyle restartBtnStyle;

    private GameObject instantConeObj;
    private GameObject suspicionConeObj;

    private FilmGrain filmGrain;
    private Vignette vignette;
    private float baseVignetteIntensity;
    private float vignetteBlend = 0f;

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
        audioSource.spatialBlend = 1f; // Full 3D spatial audio
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 15f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    void Start()
    {
        // Auto-find player target
        if (playerTarget == null && Camera.main != null)
        {
            playerTarget = Camera.main.transform;
        }

        // Auto-create eyes pivot at head height if not assigned
        if (eyes == null || eyes == transform)
        {
            Transform existing = transform.Find("TeacherEyes");
            if (existing != null)
            {
                eyes = existing;
            }
            else
            {
                GameObject eyeObj = new GameObject("TeacherEyes");
                eyeObj.transform.SetParent(transform);
                eyeObj.transform.localPosition = defaultEyeOffset;
                eyeObj.transform.localRotation = Quaternion.identity;
                eyes = eyeObj.transform;
            }
        }

        if (answerPaper == null)
        {
            answerPaper = FindAnyObjectByType<AnswerPaper>();
        }

        if (path != null && path.SegmentCount > 0)
        {
            currentSegmentIndex = 0;
            currentSegmentProgress = 0f;
            currentSegmentLength = path.GetSegmentLength(0);
            transform.position = path.EvaluateSegment(0, 0f);
            baseWaypointRotation = transform.rotation;
        }

        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        if (showInGameVisionCones)
        {
            BuildVisionConeMeshes();
        }

        if (postVolume == null)
        {
            postVolume = FindAnyObjectByType<Volume>();
        }
        if (postVolume != null)
        {
            // .profile returns a runtime copy, so the profile asset is not modified
            postVolume.profile.TryGet(out filmGrain);
            if (postVolume.profile.TryGet(out vignette))
            {
                baseVignetteIntensity = vignette.intensity.value;
            }
        }
    }

    void Update()
    {
        UpdateWalkAnimation();

        if (IsGameOver)
        {
            HandleGameOverInput();
            return;
        }

        DetectCheating();
        HandlePatrolAndSuspicion();
        UpdateConeVisuals();
        UpdatePostFX();

        // Check for suspicion alert audio trigger
        if (!wasSuspectingLastFrame && IsSuspectingPlayer)
        {
            PlayAlertSound();
        }
        wasSuspectingLastFrame = IsSuspectingPlayer;

        // Smooth visual suspicion bar
        displaySuspicion = Mathf.Lerp(displaySuspicion, CurrentSuspicion, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    /// <summary>
    /// Plays the walk animation only while the teacher actually moved since the last frame.
    /// </summary>
    private void UpdateWalkAnimation()
    {
        if (animator == null) return;

        bool moved = (transform.position - lastPosition).sqrMagnitude > 1e-8f;
        animator.speed = moved ? 1f : 0f;
        lastPosition = transform.position;
    }

    private void HandlePatrolAndSuspicion()
    {
        // If exam is over (Won or Time's Up), pause teacher patrol
        if (ExamManager.Instance != null && (ExamManager.Instance.IsWon || ExamManager.Instance.IsTimeUp))
        {
            return;
        }

        Vector3 playerPos = playerTarget != null ? playerTarget.position : new Vector3(0f, 1.15f, -0.55f);

        // CASE 1: Teacher is SUSPECTING the player
        if (IsSuspectingPlayer)
        {
            // Freeze movement & PAUSE countdown timer!
            // Rotate smoothly to stare directly at the player
            Vector3 dirToPlayer = playerPos - transform.position;
            dirToPlayer.y = 0f;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion stareRot = Quaternion.LookRotation(dirToPlayer, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, stareRot, alertTurnSpeed * Time.deltaTime);
            }
            return;
        }

        // CASE 2: Teacher is NOT suspecting (Safe)
        if (CurrentState == TeacherState.WaitingAtPoint)
        {
            // RESUME countdown timer on the point!
            waitTimer -= Time.deltaTime;

            // Look around the classroom (smooth oscillation)
            lookAroundTimer += Time.deltaTime * lookAroundSpeed;
            float sweepAngle = Mathf.Sin(lookAroundTimer) * lookAroundAngle;
            Quaternion sweepRot = baseWaypointRotation * Quaternion.Euler(0f, sweepAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, sweepRot, 4f * Time.deltaTime);

            // Once the 5 seconds are up, continue walking along spline
            if (waitTimer <= 0f)
            {
                if (path != null && path.SegmentCount > 0)
                {
                    currentSegmentIndex = (currentSegmentIndex + 1) % path.SegmentCount;
                    currentSegmentProgress = 0f;
                    currentSegmentLength = path.GetSegmentLength(currentSegmentIndex);
                }
                CurrentState = TeacherState.Walking;
                stepTimer = footstepInterval * 0.5f; // prime footstep sound
            }
        }
        else if (CurrentState == TeacherState.Walking)
        {
            if (path == null || path.SegmentCount == 0) return;

            // Handle footstep sounds while walking
            stepTimer += Time.deltaTime;
            if (stepTimer >= footstepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }

            float step = (walkSpeed * Time.deltaTime) / Mathf.Max(0.01f, currentSegmentLength);
            currentSegmentProgress += step;

            if (currentSegmentProgress >= 1f)
            {
                // Reached the next waypoint!
                currentSegmentProgress = 1f;
                transform.position = path.EvaluateSegment(currentSegmentIndex, 1f);

                Vector3 tangent = path.EvaluateSegmentTangent(currentSegmentIndex, 1f);
                tangent.y = 0f;
                if (tangent.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
                }

                baseWaypointRotation = transform.rotation;
                waitTimer = waypointWaitDuration; // start 5-second wait
                lookAroundTimer = 0f;
                CurrentState = TeacherState.WaitingAtPoint;
            }
            else
            {
                // Walking smoothly along the spline curve
                transform.position = path.EvaluateSegment(currentSegmentIndex, currentSegmentProgress);

                Vector3 tangent = path.EvaluateSegmentTangent(currentSegmentIndex, currentSegmentProgress);
                tangent.y = 0f;
                if (tangent.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(tangent, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void PlayFootstep()
    {
        if (audioSource == null) return;

        AudioClip clip = footstepClip;
        if (footstepClips != null && footstepClips.Length > 0)
        {
            clip = footstepClips[Random.Range(0, footstepClips.Length)];
        }

        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.92f, 1.08f);
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    private void PlayAlertSound()
    {
        if (audioSource != null && suspicionAlertClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(suspicionAlertClip, 0.85f);
        }
    }

    private void DetectCheating()
    {
        // If exam is over (Won or Time's Up), teacher stops suspecting
        if (ExamManager.Instance != null && (ExamManager.Instance.IsWon || ExamManager.Instance.IsTimeUp))
        {
            IsSuspectingPlayer = false;
            CurrentSuspicion = 0f;
            return;
        }

        if (answerPaper == null)
        {
            answerPaper = FindAnyObjectByType<AnswerPaper>();
        }

        if (playerTarget == null && Camera.main != null)
        {
            playerTarget = Camera.main.transform;
        }

        bool isCheating = IsPlayerCheating;

        Vector3 playerPos = playerTarget != null ? playerTarget.position : new Vector3(0f, 1.15f, -0.55f);
        Vector3 eyePos = (eyes != null && eyes != transform) ? eyes.position : (transform.position + defaultEyeOffset);
        Vector3 eyeForward = (eyes != null && eyes != transform) ? eyes.forward : transform.forward;

        Vector3 dirToPlayer = playerPos - eyePos;
        float distToPlayer = dirToPlayer.magnitude;

        // Line-of-sight check to the player (ignores desk top and teacher itself)
        bool hasLineOfSight = true;
        if (Physics.Raycast(eyePos, dirToPlayer.normalized, out RaycastHit hit, distToPlayer, obstacleMask))
        {
            if (hit.transform != transform && 
                !hit.transform.IsChildOf(transform) && 
                hit.distance < distToPlayer - 0.35f &&
                hit.transform.name != "Top" &&
                hit.transform.name != "Desk" &&
                hit.transform.name != "Sheet")
            {
                // A solid wall/obstacle blocked the view
                hasLineOfSight = false;
            }
        }

        float angleToPlayer = Vector3.Angle(eyeForward, dirToPlayer);

        float safeInstantAngle = Mathf.Clamp(instantSpotAngle, 10f, 160f);
        float safeSuspicionAngle = Mathf.Clamp(suspicionAngle, 10f, 160f);

        bool inInstantCone = distToPlayer <= instantSpotRange && angleToPlayer <= (safeInstantAngle * 0.5f);
        bool inSuspicionCone = distToPlayer <= suspicionRange && angleToPlayer <= (safeSuspicionAngle * 0.5f);

        // ONLY detect cheating if player is actively in CHEATING STATE (answer paper is out)
        if (isCheating && hasLineOfSight)
        {
            // 1. Instant Spot Cone
            if (inInstantCone)
            {
                TriggerGameOver("Teacher caught you red-handed! Your cheat sheet was out while the teacher looked right at you!");
                return;
            }

            // 2. Suspicion Cone
            if (inSuspicionCone)
            {
                IsSuspectingPlayer = true;
                CurrentSuspicion = Mathf.Min(100f, CurrentSuspicion + suspicionFillRate * Time.deltaTime);

                if (CurrentSuspicion >= 100f)
                {
                    TriggerGameOver("Teacher's suspicion reached 100%! Caught cheating during test!");
                    return;
                }
                return;
            }
        }

        // When answer paper is tucked in (not cheating) OR player is out of sight:
        // Teacher stops suspecting. Suspicion steadily cools down.
        IsSuspectingPlayer = false;
        CurrentSuspicion = Mathf.Max(0f, CurrentSuspicion - suspicionDrainRate * Time.deltaTime);
    }

    public void TriggerGameOver(string reason)
    {
        IsGameOver = true;
        GameOverReason = reason;
        CurrentSuspicion = 100f;

        // Play caught sting audio if assigned
        if (audioSource != null && caughtStingClip != null)
        {
            audioSource.PlayOneShot(caughtStingClip, 1f);
        }

        // Unlock cursor for restart
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Face player on game over
        Vector3 targetLook = playerTarget != null ? playerTarget.position : 
                             (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        Vector3 lookDir = targetLook - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }
    }

    private void HandleGameOverInput()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BuildVisionConeMeshes()
    {
        Transform parentTransform = eyes != null ? eyes : transform;
        instantConeObj = CreateConeMeshObject("InstantSpotCone", instantSpotRange, instantSpotAngle, new Color(1f, 0.15f, 0.15f, coneAlpha * 1.5f), parentTransform);
        suspicionConeObj = CreateConeMeshObject("SuspicionCone", suspicionRange, suspicionAngle, new Color(1f, 0.85f, 0.2f, coneAlpha), parentTransform);
    }

    private GameObject CreateConeMeshObject(string name, float length, float fovAngle, Color color, Transform parent)
    {
        GameObject cone = new GameObject(name);
        cone.transform.SetParent(parent);
        cone.transform.localPosition = Vector3.zero;
        cone.transform.localRotation = Quaternion.identity;

        MeshFilter mf = cone.AddComponent<MeshFilter>();
        MeshRenderer mr = cone.AddComponent<MeshRenderer>();

        // Generate procedural cone mesh
        Mesh mesh = GenerateConeMesh(length, fovAngle, 24);
        mf.mesh = mesh;

        // Create transparent material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null || mat.shader.name != "Universal Render Pipeline/Unlit")
        {
            mat = new Material(Shader.Find("Sprites/Default"));
        }
        mat.color = color;
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return cone;
    }

    private Mesh GenerateConeMesh(float length, float angleDeg, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "VisionConeMesh";

        // Clamp angle safely between 10 and 160 degrees
        angleDeg = Mathf.Clamp(angleDeg, 10f, 160f);
        length = Mathf.Max(0.5f, length);

        float radius = length * Mathf.Tan(angleDeg * 0.5f * Mathf.Deg2Rad);

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3 * 2];

        vertices[0] = Vector3.zero; // Apex at eyes

        for (int i = 0; i < segments; i++)
        {
            float rad = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, length);
        }
        vertices[segments + 1] = new Vector3(0f, 0f, length); // Cap center

        int t = 0;
        // Sides
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[t++] = 0;
            triangles[t++] = i + 1;
            triangles[t++] = next + 1;
        }

        // End Cap
        int centerIdx = segments + 1;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[t++] = centerIdx;
            triangles[t++] = next + 1;
            triangles[t++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void UpdateConeVisuals()
    {
        if (!showInGameVisionCones) return;

        if (suspicionConeObj != null)
        {
            MeshRenderer mr = suspicionConeObj.GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                float pulse = IsSuspectingPlayer ? (0.7f + 0.3f * Mathf.Sin(Time.time * 10f)) : 1f;
                Color c = IsSuspectingPlayer ? Color.Lerp(Color.yellow, Color.red, CurrentSuspicion / 100f) : new Color(1f, 0.85f, 0.2f, coneAlpha);
                c.a = coneAlpha * pulse;
                mr.material.color = c;
            }
        }
    }

    /// <summary>
    /// Film grain grows as the teacher gets closer; vignette intensifies while she is suspicious.
    /// </summary>
    private void UpdatePostFX()
    {
        if (filmGrain != null && playerTarget != null)
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            float proximity = Mathf.InverseLerp(grainFarDistance, grainNearDistance, dist);
            filmGrain.intensity.value = Mathf.Lerp(grainBaseIntensity, grainNearIntensity, proximity);
        }

        if (vignette != null)
        {
            vignetteBlend = Mathf.MoveTowards(vignetteBlend, IsSuspectingPlayer ? 1f : 0f, vignetteBlendSpeed * Time.deltaTime);
            vignette.intensity.value = Mathf.Lerp(baseVignetteIntensity, suspicionVignetteIntensity, vignetteBlend);
        }
    }

    void OnGUI()
    {
        if (whiteTexture == null) return;
        if (ExamManager.Instance != null && (ExamManager.Instance.IsWon || ExamManager.Instance.IsTimeUp)) return;

        InitStyles();

        // 1. Suspicion Bar HUD (shows when suspicion > 0) — disabled
        /*
        if (displaySuspicion > 0.5f && !IsGameOver)
        {
            float barW = 320f;
            float barH = 20f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = 30f;

            // Header Warning
            string headerText = IsSuspectingPlayer 
                ? "! TEACHER STARING AT YOU! HIDE YOUR CHEAT SHEET (RMB) !" 
                : "Teacher Suspicion (Cooling Down...)";
            headerStyle.normal.textColor = IsSuspectingPlayer ? Color.red : Color.yellow;
            GUI.Label(new Rect(barX - 60f, barY - 26f, barW + 120f, 24f), headerText, headerStyle);

            // Frame & BG
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(barX - 2f, barY - 2f, barW + 4f, barH + 4f), whiteTexture);
            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), whiteTexture);

            // Fill
            float ratio = Mathf.Clamp01(displaySuspicion / 100f);
            Color barColor = Color.Lerp(new Color(1f, 0.85f, 0.1f), Color.red, ratio);
            if (ratio > 0.75f)
            {
                float flash = 0.6f + 0.4f * Mathf.Sin(Time.time * 12f);
                barColor = Color.Lerp(barColor, Color.white, flash * 0.4f);
            }
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barX, barY, barW * ratio, barH), whiteTexture);

            // Percentage Text
            GUI.color = Color.white;
            GUI.Label(new Rect(barX, barY, barW, barH), $"{Mathf.CeilToInt(CurrentSuspicion)}%", barStyle);
        }
        */

        // 2. Game Over Screen
        if (IsGameOver)
        {
            // Dark red overlay
            GUI.color = new Color(0.45f, 0.02f, 0.02f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            // BUSTED Title
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 300f, cy - 110f, 600f, 60f), "BUSTED!", gameOverStyle);

            // Reason Subtitle
            GUI.Label(new Rect(cx - 350f, cy - 35f, 700f, 40f), GameOverReason, subStyle);

            // Restart Button
            Rect btnRect = new Rect(cx - 130f, cy + 30f, 260f, 50f);
            if (GUI.Button(btnRect, "RETRY (Press R)", restartBtnStyle))
            {
                RestartGame();
            }
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        if (barStyle == null)
        {
            barStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (gameOverStyle == null)
        {
            gameOverStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.2f, 0.2f) }
            };
        }

        if (subStyle == null)
        {
            subStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
        }

        if (restartBtnStyle == null)
        {
            restartBtnStyle = new GUIStyle(GUI.skin.button)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }
    }

    void OnDrawGizmos()
    {
        Vector3 eyePos = (eyes != null && eyes != transform) ? eyes.position : (transform.position + defaultEyeOffset);
        Vector3 fwd = (eyes != null && eyes != transform) ? eyes.forward : transform.forward;

        float safeInstantAngle = Mathf.Clamp(instantSpotAngle, 10f, 160f);
        float safeSuspicionAngle = Mathf.Clamp(suspicionAngle, 10f, 160f);

        // Draw Instant Spot Cone (Red Wireframe)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        DrawWireCone(eyePos, fwd, instantSpotRange, safeInstantAngle);

        // Draw Suspicion Cone (Yellow Wireframe)
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
        DrawWireCone(eyePos, fwd, suspicionRange, safeSuspicionAngle);

        // Draw Line to Player
        Vector3 playerPos = playerTarget != null ? playerTarget.position : 
                            (Camera.main != null ? Camera.main.transform.position : new Vector3(0f, 1.15f, -0.55f));

        if (IsPlayerCheating)
        {
            Gizmos.color = IsSuspectingPlayer ? Color.red : Color.yellow;
        }
        else
        {
            Gizmos.color = Color.green; // Player is safe (not cheating)
        }
        Gizmos.DrawLine(eyePos, playerPos);
    }

    private void DrawWireCone(Vector3 origin, Vector3 direction, float distance, float angleDeg)
    {
        float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
        float radius = distance * Mathf.Tan(halfAngle);
        Vector3 forwardOffset = direction * distance;

        Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);

        int segments = 16;
        Vector3 prevPoint = Vector3.zero;
        Vector3 firstPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 circlePoint = origin + forwardOffset + rot * new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, 0f);

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, circlePoint);
            }
            else
            {
                firstPoint = circlePoint;
            }

            if (i % 4 == 0)
            {
                Gizmos.DrawLine(origin, circlePoint);
            }

            prevPoint = circlePoint;
        }
    }

    void OnDestroy()
    {
        if (whiteTexture != null) Destroy(whiteTexture);
    }
}
