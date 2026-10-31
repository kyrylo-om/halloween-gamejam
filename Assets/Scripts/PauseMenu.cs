using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuUI;

    [Header("Player Control Reference")]
    [Tooltip("Drag your player movement script component here (e.g., SimpleFPSController)")]
    [SerializeField] private MonoBehaviour playerController;

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsGamePaused { get; private set; } = false;

    private void Update()
    {
        // Check for Escape or R key press
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
           (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ))
#else
        if (Input.GetKeyDown(KeyCode.Escape) 
#endif
        {
            if (IsGamePaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;

        // Re-enable player movement & lock cursor
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;

        // Disable player movement & unlock cursor
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}