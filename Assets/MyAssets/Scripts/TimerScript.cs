using UnityEngine;
using TMPro;


public class TimerScript : MonoBehaviour
{
    [Header("Hide at 0")]
    public GameObject prompt;    // the "blood moon rising" text object

    public bool IsFinished => finished;
    public float time = 300f;
    public TextMeshProUGUI timerText;

    [Header("Color")]
    public float whiteAtTime = 180f;   // at or above this, text is white
    public Color startColor = Color.white;
    public Color endColor = Color.red;

    [Header("Size")]
    public float minFontSize = 36f;    // size at/above whiteAtTime
    public float maxFontSize = 96f;    // size at 0

    private bool finished = false;

    void Update()
    {
        if (finished) return;

        time -= Time.deltaTime;

        if (time <= 0f)
        {
            time = 0f;
            finished = true;
            timerText.text = "RUN";
            timerText.color = endColor;
            timerText.fontSize = maxFontSize;
            
            if (prompt != null) prompt.SetActive(false);   // hides the prompt
            return;
        }

        // 0 = at 180s or more (white), 1 = at 0s (full red)
        float t = 1f - Mathf.Clamp01(time / whiteAtTime);

        timerText.text = Mathf.Ceil(time).ToString();
        timerText.color = Color.Lerp(startColor, endColor, t);
        timerText.fontSize = Mathf.Lerp(minFontSize, maxFontSize, t);
    }
    public void RemoveTime(float seconds)
{
    if (finished) return;
    time -= seconds;   
}
}