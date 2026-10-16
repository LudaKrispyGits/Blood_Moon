using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;     // your Game Over UI panel
    public bool freezeTime = true;       // pauses the game on death

    private bool isOver;
    public bool IsOver => isOver;

    void Start()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (isOver) return;
        isOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Free the mouse so the player can click the button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (freezeTime) Time.timeScale = 0f;
    }

    // Hook this up to your Restart button's OnClick
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}