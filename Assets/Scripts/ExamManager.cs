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

    [Header("References")]
    public TestPaper testPaper;
    public TeacherController teacher;

    [Header("HUD Styling")]
    public Vector2 hudPosition = new Vector2(25f, 25f);
    public Vector2 progressBarSize = new Vector2(220f, 16f);
    public Color timerNormalColor = Color.white;
    public Color timerWarningColor = new Color(1f, 0.85f, 0.2f);
    public Color timerCriticalColor = new Color(1f, 0.25f, 0.2f);
    public Color progressFillColor = new Color(0.25f, 0.75f, 1f, 0.95f);
    public Color progressCompleteColor = new Color(0.3f, 0.9f, 0.4f, 0.95f);

    // Runtime state
    public float TimeRemaining { get; private set; }
    public float ElapsedTime => examDuration - TimeRemaining;
    public bool IsWon { get; private set; }
    public bool IsTimeUp { get; private set; }
    public bool IsGameOver => IsTimeUp || (teacher != null && teacher.IsGameOver);

    private float displayProgress = 0f;
    private Texture2D whiteTexture;

    private GUIStyle timerTitleStyle;
    private GUIStyle timerDigitsStyle;
    private GUIStyle progressLabelStyle;
    private GUIStyle winTitleStyle;
    private GUIStyle loseTitleStyle;
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

        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        if (IsWon || IsTimeUp || (teacher != null && teacher.IsGameOver))
        {
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
        if (whiteTexture == null) return;

        InitStyles();

        // 1. In-game HUD: Timer & Writing Progress (Top-Left)
        if (!IsWon && !IsTimeUp && !(teacher != null && teacher.IsGameOver))
        {
            DrawHUD();
        }

        // 2. Victory Win Screen
        if (IsWon)
        {
            DrawWinScreen();
        }
        // 3. Time's Up Lose Screen
        else if (IsTimeUp && !(teacher != null && teacher.IsGameOver))
        {
            DrawTimeUpScreen();
        }
    }

    private void DrawHUD()
    {
        float x = hudPosition.x;
        float y = hudPosition.y;

        // Format Time MM:SS
        int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
        string timeStr = $"{minutes:00}:{seconds:00}";

        // Pick timer text color based on remaining time
        Color timerColor = timerNormalColor;
        if (TimeRemaining <= 30f)
        {
            float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
            timerColor = Color.Lerp(timerCriticalColor, Color.white, flash * 0.4f);
        }
        else if (TimeRemaining <= 60f)
        {
            timerColor = timerWarningColor;
        }

        // Draw Timer Panel
        timerTitleStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x, y, 200f, 16f), "EXAM TIME LEFT", timerTitleStyle);

        timerDigitsStyle.normal.textColor = timerColor;
        GUI.Label(new Rect(x, y + 16f, 200f, 32f), timeStr, timerDigitsStyle);

        // Draw Progress Bar below Timer
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
    }

    private void DrawWinScreen()
    {
        // Festive green/gold backdrop
        GUI.color = new Color(0.02f, 0.25f, 0.08f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        // Title
        GUI.color = Color.white;
        GUI.Label(new Rect(cx - 300f, cy - 120f, 600f, 60f), "EXAM PASSED! 🎓", winTitleStyle);

        // Subtitle
        int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(ElapsedTime % 60f);
        string timeTaken = $"{minutes:00}:{seconds:00}";

        string winMsg = $"You finished copying the entire essay without getting caught!\nTime taken: {timeTaken}";
        GUI.Label(new Rect(cx - 350f, cy - 40f, 700f, 50f), winMsg, subtitleStyle);

        // Restart / Play Again Button
        Rect btnRect = new Rect(cx - 140f, cy + 35f, 280f, 50f);
        if (GUI.Button(btnRect, "PLAY AGAIN (Press R)", buttonStyle))
        {
            RestartExam();
        }
    }

    private void DrawTimeUpScreen()
    {
        // Dark amber/red backdrop
        GUI.color = new Color(0.35f, 0.05f, 0.05f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        // Title
        GUI.color = Color.white;
        GUI.Label(new Rect(cx - 300f, cy - 120f, 600f, 60f), "TIME'S UP! ⏰", loseTitleStyle);

        // Subtitle
        int wordsWritten = testPaper != null ? testPaper.WordsWritten : 0;
        int totalWords = testPaper != null ? testPaper.TotalWords : 1;
        string msg = $"Pencils down! The exam has ended.\nYou only finished {wordsWritten} of {totalWords} words.";
        GUI.Label(new Rect(cx - 350f, cy - 40f, 700f, 50f), msg, subtitleStyle);

        // Restart Button
        Rect btnRect = new Rect(cx - 140f, cy + 35f, 280f, 50f);
        if (GUI.Button(btnRect, "RETRY EXAM (Press R)", buttonStyle))
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
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        if (timerDigitsStyle == null)
        {
            timerDigitsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
        }

        if (progressLabelStyle == null)
        {
            progressLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        if (winTitleStyle == null)
        {
            winTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 1f, 0.4f) }
            };
        }

        if (loseTitleStyle == null)
        {
            loseTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }
    }

    void OnDestroy()
    {
        if (whiteTexture != null) Destroy(whiteTexture);
    }
}

