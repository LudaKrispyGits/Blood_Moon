using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class RestartButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Restart);
    }

    public void Restart()
    {
        Time.timeScale = 1f;   // un-freeze, since game over/win pauses the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}