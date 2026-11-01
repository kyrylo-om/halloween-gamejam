using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the classroom test rules:
/// - 3-minute exam countdown timer (timer hits 0 = Time's Up / Lose).
/// - Essay writing progress bar (copying all words = Victory / Win).
/// - Displays HUD for Timer and Progress.
/// - Shows Win or Time's Up Game Over screens.
/// </summary>
public class ExamManager : MonoBehaviour
{
    private static ExamManager instance;
    public static ExamManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ExamManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ExamManager");
                    instance = go.AddComponent<ExamManager>();
                }
            }
            return instance;
        }
    }

    [Header("Exam Timer Settings")]
    [Tooltip("Total exam duration in seconds (180s = 3 minutes).")]
    public float examDuration = 180f;
    [Tooltip("In-game minutes shown on the clock at the start. The clock counts down to 0 over examDuration.")]
    public float clockStartMinutes = 40f;
    private const float ClockStepSeconds = 10f;

    [Header("References")]
    public TestPaper testPaper;
    public TeacherController teacher;

    [Header("Audio")]
    public AudioClip victoryClip;
    public AudioClip lossClip;

    [Header("HUD Styling")]
    public Font uiFont;
    public Vector2 hudPosition = new Vector2(25f, 25f);
    public Vector2 progressBarSize = new Vector2(220f, 16f);
    public Color progressFillColor = new Color(0.25f, 0.75f, 1f, 0.95f);
    public Color progressCompleteColor = new Color(0.3f, 0.9f, 0.4f, 0.95f);

    // Runtime state
    public float TimeRemaining { get; private set; }
    public float ElapsedTime => examDuration - TimeRemaining;
    public bool IsWon { get; private set; }
    public bool IsTimeUp { get; private set; }
    public bool IsGameOver => IsTimeUp || (teacher != null && teacher.IsGameOver);

    private float displayProgress = 0f;
    private float endScreenStartTime = -1f;
    private AudioSource endAudioSource;
    private bool endSoundPlayed = false;

    private GUIStyle timerTitleStyle;
    private GUIStyle timerDigitsStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        TimeRemaining = examDuration;

        endAudioSource = gameObject.AddComponent<AudioSource>();
        endAudioSource.playOnAwake = false;
        endAudioSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (testPaper == null)
        {
            testPaper = FindAnyObjectByType<TestPaper>();
        }

        if (teacher == null)
        {
            teacher = FindAnyObjectByType<TeacherController>();
        }
    }

    void Update()
    {
        if (IsWon || IsTimeUp || (teacher != null && teacher.IsGameOver))
        {
            PlayEndSound();
            HandleRestartInput();
            return;
        }

        // Countdown test timer
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TriggerTimeUp();
            return;
        }

        // Check for victory from TestPaper
        if (testPaper != null && testPaper.TotalWords > 0 && testPaper.WordsWritten >= testPaper.TotalWords)
        {
            TriggerWin();
        }

        // Smooth visual progress bar fill
        float targetProg = testPaper != null ? testPaper.Progress : 0f;
        displayProgress = Mathf.Lerp(displayProgress, targetProg, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    /// <summary>
    /// Triggered when all words from the answer paper have been copied.
    /// </summary>
    public void TriggerWin()
    {
        if (IsWon || IsGameOver) return;

        IsWon = true;

        // Unlock cursor for victory UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Triggered when the 3-minute exam timer runs out.
    /// </summary>
    public void TriggerTimeUp()
    {
        if (IsWon || IsTimeUp) return;

        IsTimeUp = true;

        // Unlock cursor for restart
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PlayEndSound()
    {
        if (endSoundPlayed) return;
        endSoundPlayed = true;

        AudioClip clip = IsWon ? victoryClip : lossClip;
        if (clip != null)
        {
            endAudioSource.PlayOneShot(clip);
        }
    }

    private void HandleRestartInput()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartExam();
        }
    }

    public void RestartExam()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        InitStyles();
        GuiScale.Apply();

        // 1. In-game HUD: Timer (Top-Left)
        if (!IsWon && !IsTimeUp && !(teacher != null && teacher.IsGameOver))
        {
            DrawHUD();
            return;
        }

        if (endScreenStartTime < 0f) endScreenStartTime = Time.unscaledTime;
        float alpha = Mathf.Clamp01((Time.unscaledTime - endScreenStartTime) / 0.8f);

        // 2. Victory Win Screen
        if (IsWon)
        {
            DrawWinScreen(alpha);
        }
        // 3. Time's Up Lose Screen
        else if (IsTimeUp && !(teacher != null && teacher.IsGameOver))
        {
            DrawTimeUpScreen(alpha);
        }
    }

    private void DrawHUD()
    {
        float x = hudPosition.x;
        float y = hudPosition.y;

        // Map real time onto the in-game clock, which ticks down in ClockStepSeconds steps.
        // Real seconds per step = examDuration / (clockStartMinutes * 60 / ClockStepSeconds).
        float clockSteps = clockStartMinutes * 60f / ClockStepSeconds;
        float realSecondsPerStep = examDuration / clockSteps;
        int clockSeconds = Mathf.CeilToInt(TimeRemaining / realSecondsPerStep) * (int)ClockStepSeconds;

        // Format Time MM:SS
        int minutes = clockSeconds / 60;
        int seconds = clockSeconds % 60;
        string timeStr = $"{minutes:00}:{seconds:00}";

        // Pick timer color based on remaining time
        Color timerColor = GuiTheme.Bone;
        if (TimeRemaining <= 30f)
        {
            float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            timerColor = Color.Lerp(GuiTheme.Blood, GuiTheme.BloodBright, flash);
        }
        else if (TimeRemaining <= 60f)
        {
            timerColor = GuiTheme.Candle;
        }

        // Soft dark backdrop fading to the right
        GuiTheme.Texture(new Rect(0f, 0f, x + 260f, y + 90f), GuiTheme.FadeRight, new Color(0f, 0f, 0f, 0.55f));

        GuiTheme.Label(new Rect(x, y, 200f, 18f), "time remaining", timerTitleStyle, GuiTheme.BoneDim);
        GuiTheme.Label(new Rect(x, y + 14f, 200f, 46f), timeStr, timerDigitsStyle, timerColor);

        // Thin line that burns down with the remaining time
        float lineW = 120f;
        float ratio = Mathf.Clamp01(TimeRemaining / Mathf.Max(1f, examDuration));
        GuiTheme.Rect(new Rect(x + 2f, y + 62f, lineW, 1f), GuiTheme.WithAlpha(GuiTheme.BoneDim, 0.4f));
        GuiTheme.Rect(new Rect(x + 2f, y + 61f, lineW * ratio, 2f), timerColor);
        GuiTheme.Texture(new Rect(x + 2f + lineW * ratio - 4f, y + 58f, 8f, 8f), GuiTheme.Dot, GuiTheme.WithAlpha(timerColor, 0.6f));

        // Draw Progress Bar below Timer — disabled
        /*
        float barY = y + 54f;
        int wordsWritten = testPaper != null ? testPaper.WordsWritten : 0;
        int totalWords = testPaper != null ? testPaper.TotalWords : 1;
        float ratio = Mathf.Clamp01(displayProgress);

        string progressText = $"ESSAY PROGRESS: {Mathf.RoundToInt(ratio * 100f)}% ({wordsWritten}/{totalWords} words)";
        progressLabelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, barY, progressBarSize.x + 80f, 18f), progressText, progressLabelStyle);

        float barDrawY = barY + 20f;
        // Border
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(x - 2f, barDrawY - 2f, progressBarSize.x + 4f, progressBarSize.y + 4f), whiteTexture);
        // Background
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        GUI.DrawTexture(new Rect(x, barDrawY, progressBarSize.x, progressBarSize.y), whiteTexture);
        // Fill
        Color fillColor = ratio >= 0.99f ? progressCompleteColor : progressFillColor;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, barDrawY, progressBarSize.x * ratio, progressBarSize.y), whiteTexture);
        */
    }

    private void DrawWinScreen(float alpha)
    {
        int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(ElapsedTime % 60f);
        string timeTaken = $"{minutes:00}:{seconds:00}";

        string winMsg = $"The essay is copied and no one saw a thing.\nTime taken: {timeTaken}";
        if (GuiTheme.EndScreen(alpha, new Color(0.02f, 0.03f, 0.02f, 0.9f), "Exam Passed", GuiTheme.Candle,
                               winMsg, "Play again  (R)", titleStyle, subtitleStyle, buttonStyle))
        {
            RestartExam();
        }
    }

    private void DrawTimeUpScreen(float alpha)
    {
        int wordsWritten = testPaper != null ? testPaper.WordsWritten : 0;
        int totalWords = testPaper != null ? testPaper.TotalWords : 1;
        string msg = $"Pencils down. The bell has tolled.\nYou only finished {wordsWritten} of {totalWords} words.";
        if (GuiTheme.EndScreen(alpha, new Color(0.06f, 0.02f, 0.02f, 0.9f), "Time's Up", GuiTheme.BloodBright,
                               msg, "Retry exam  (R)", titleStyle, subtitleStyle, buttonStyle))
        {
            RestartExam();
        }
    }

    private void InitStyles()
    {
        if (timerTitleStyle == null)
        {
            timerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = 15
            };
        }

        if (timerDigitsStyle == null)
        {
            timerDigitsStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = 40
            };
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 84
            };
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                wordWrap = true
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22
            };
        }
    }
}

