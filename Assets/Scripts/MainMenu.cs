using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Name of the scene to load when clicking Play")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;

    public void PlayGame()
    {
        // Load the gameplay scene by name
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");

#if UNITY_EDITOR
        // Stops Play Mode when running in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quits the application when running in a built game (.exe)
        Application.Quit();
#endif
    }
}