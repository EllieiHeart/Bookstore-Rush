using UnityEngine;
using TMPro;
using System;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float dayDuration = 120f; // 2 minutes per day
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private bool countDown = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalTimeColor = Color.white;
    [SerializeField] private Color warningTimeColor = Color.yellow;
    [SerializeField] private Color dangerTimeColor = Color.red;
    [SerializeField] private float warningThreshold = 30f; // Last 30 seconds
    [SerializeField] private float dangerThreshold = 10f;  // Last 10 seconds
    
    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false;
    
    public static event Action OnTimerFinished;
    public static event Action<float> OnTimerUpdate;
    
    public float TimeRemaining => currentTime;
    public float TimeElapsed => dayDuration - currentTime;
    public bool IsRunning => isRunning;
    public bool HasTimeLeft => currentTime > 0;

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (isRunning && !isPaused && HasTimeLeft)
        {
            currentTime -= Time.deltaTime;
            currentTime = Mathf.Max(0, currentTime);
            
            UpdateTimerUI();
            OnTimerUpdate?.Invoke(currentTime);
            
            if (currentTime <= 0)
            {
                EndTimer();
            }
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        isPaused = false;
        Debug.Log($"Day timer started! Duration: {dayDuration} seconds");
    }

    public void PauseTimer()
    {
        isPaused = true;
    }

    public void ResumeTimer()
    {
        isPaused = false;
    }

    public void ResetTimer()
    {
        currentTime = dayDuration;
        isRunning = false;
        isPaused = false;
        UpdateTimerUI();
    }

    public void SetDayDuration(float newDuration)
    {
        dayDuration = newDuration;
        ResetTimer();
    }

    void EndTimer()
    {
        isRunning = false;
        Debug.Log("Day timer finished!");
        OnTimerFinished?.Invoke();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        
        // Format time as MM:SS
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
        
        // Change color based on time remaining
        if (currentTime <= dangerThreshold)
        {
            timerText.color = dangerTimeColor;
        }
        else if (currentTime <= warningThreshold)
        {
            timerText.color = warningTimeColor;
        }
        else
        {
            timerText.color = normalTimeColor;
        }
    }

    // Public method to get formatted time string
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}
