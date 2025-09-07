using UnityEngine;
using System;

[System.Serializable]
public class DaySettings
{
    [Header("Day Info")]
    public int dayNumber = 1;
    public string dayName = "Day 1";
    
    [Header("Objectives")]
    public int requiredCustomers = 5;
    public int maxWrongOrders = 3;
    
    [Header("Difficulty")]
    public float dayDuration = 120f; // 2 minutes
    public float customerSpawnInterval = 4f;
    public int maxQueueSize = 5;
    
    [Header("Scoring")]
    public int pointsPerCustomer = 100;
    public int wrongOrderPenalty = 25;
}

public class DayManager : MonoBehaviour
{
    [Header("Day Settings")]
    [SerializeField] private DaySettings currentDaySettings;
    [SerializeField] private int baseRequiredCustomers = 5;
    [SerializeField] private int customerIncreasePerDay = 3;
    [SerializeField] private float baseDayDuration = 120f;
    [SerializeField] private float durationIncreasePerDay = 10f;
    
    [Header("Progression")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int highestDayReached = 1;
    
    public static event Action<DaySettings> OnDayStarted;
    public static event Action<DayResult> OnDayCompleted;
    
    public DaySettings CurrentDay => currentDaySettings;
    public int CurrentDayNumber => currentDay;

    void Start()
    {
        LoadDayProgress();
        SetupDaySettings(currentDay);
    }

    public void SetupDaySettings(int dayNumber)
    {
        currentDay = dayNumber;
        
        currentDaySettings = new DaySettings
        {
            dayNumber = dayNumber,
            dayName = $"Day {dayNumber}",
            requiredCustomers = baseRequiredCustomers + (dayNumber - 1) * customerIncreasePerDay,
            maxWrongOrders = Mathf.Max(1, 5 - (dayNumber - 1)), // Gets harder over time
            dayDuration = baseDayDuration + (dayNumber - 1) * durationIncreasePerDay,
            customerSpawnInterval = Mathf.Max(2f, 4f - (dayNumber - 1) * 0.2f), // Faster spawning
            maxQueueSize = Mathf.Min(8, 5 + (dayNumber - 1)), // More customers over time
            pointsPerCustomer = 100 + (dayNumber - 1) * 10, // More points for harder days
            wrongOrderPenalty = 25 + (dayNumber - 1) * 5 // Higher penalty for mistakes
        };
        
        Debug.Log($"Day {dayNumber} Setup - Required: {currentDaySettings.requiredCustomers} customers, Duration: {currentDaySettings.dayDuration}s");
        OnDayStarted?.Invoke(currentDaySettings);
    }

    public void StartDay()
    {
        OnDayStarted?.Invoke(currentDaySettings);
    }

    public void CompleteDayWithResult(DayResult result)
    {
        if (result.success && currentDay >= highestDayReached)
        {
            highestDayReached = currentDay + 1;
            SaveDayProgress();
        }
        
        OnDayCompleted?.Invoke(result);
    }

    public void GoToNextDay()
    {
        SetupDaySettings(currentDay + 1);
    }

    public void RestartCurrentDay()
    {
        SetupDaySettings(currentDay);
    }

    public void GoToDay(int dayNumber)
    {
        if (dayNumber <= highestDayReached)
        {
            SetupDaySettings(dayNumber);
        }
        else
        {
            Debug.LogWarning($"Cannot access Day {dayNumber}. Highest unlocked: {highestDayReached}");
        }
    }

    void SaveDayProgress()
    {
        PlayerPrefs.SetInt("BookstoreRush_HighestDay", highestDayReached);
        PlayerPrefs.Save();
    }

    void LoadDayProgress()
    {
        highestDayReached = PlayerPrefs.GetInt("BookstoreRush_HighestDay", 1);
    }
}

[System.Serializable]
public class DayResult
{
    public bool success;
    public int customersServed;
    public int wrongOrders;
    public int finalScore;
    public float timeRemaining;
    public string resultMessage;
    
    public DayResult(bool success, int served, int wrong, int score, float timeLeft)
    {
        this.success = success;
        this.customersServed = served;
        this.wrongOrders = wrong;
        this.finalScore = score;
        this.timeRemaining = timeLeft;
        
        if (success)
        {
            resultMessage = $"Day Complete! Served {served} customers with {wrong} mistakes.";
        }
        else
        {
            resultMessage = $"Day Failed. Only served {served} customers (needed more).";
        }
    }
}
