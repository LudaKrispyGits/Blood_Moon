using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;     // your Game Over UI panel
    public GameObject winPanel;          // your You Win UI panel
    public bool freezeTime = true;       // pauses the game on win/death

    private bool isOver;
    public bool IsOver => isOver;

    void Start()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    public void TriggerGameOver()
    {
        End(gameOverPanel);
    }

    public void TriggerWin()
    {
        End(winPanel);
    }

    void End(GameObject panel)
    {
        if (isOver) return;              // first outcome wins, ignore the other
        isOver = true;

        if (panel != null) panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (freezeTime) Time.timeScale = 0f;
    }

    // Hook this up to the Restart button on both panels
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}