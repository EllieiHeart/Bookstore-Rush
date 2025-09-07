using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum GameState
{
    Preparation,
    Playing,
    DayComplete,
    DayFailed,
    Paused
}

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Refs")]
    public CustomerSpawner spawner;
    public TextMeshProUGUI scoreText;
    public BookLibrary bookLibrary;
    public GameTimer gameTimer;
    public DayManager dayManager;

    [Header("UI Panels")]
    public GameObject dayCompletePanel;
    public GameObject dayFailedPanel;
    public GameObject preparationPanel;
    public TextMeshProUGUI dayInfoText;
    public TextMeshProUGUI objectiveText;

    [Header("DEBUG")]
    public bool debugMode = true;

    [Header("Customer Exit Positions")]
    public Transform satisfiedExitPoint;
    public Transform disappointedExitPoint;
    public Vector3 defaultSatisfiedExit = new Vector3(15f, -2f, 0f);
    public Vector3 defaultDisappointedExit = new Vector3(-15f, -2f, 0f);

    [Header("Queue Settings")]
    [SerializeField] private int maxQueueSize = 5;
    [SerializeField] private float queueSpacing = 2f;
    [SerializeField] private Vector3 servicePosition = new Vector3(0f, -2f, 0f);
    [SerializeField] private Vector3 queueStartPosition = new Vector3(3f, -2f, 0f);
    [SerializeField] private float spawnInterval = 4f;

    [Header("Gameplay Settings")]
    public int scorePenalty = 25;
    public float respawnDelay = 1f;

    [Header("Runtime - DO NOT EDIT")]
    public Customer currentCustomer;
    public List<Customer> customerQueue = new List<Customer>();
    public int score;
    public int totalCustomersServed;
    public int wrongDeliveries;
    [SerializeField] private GameState _currentState = GameState.Preparation;
    public GameState currentState 
    { 
        get => _currentState; 
        private set 
        { 
            _currentState = value;
            if (debugMode) Debug.Log($"Game state changed to: {value}");
        } 
    }

    private DaySettings currentDaySettings;
    private Coroutine customerSpawningCoroutine;

    void Awake()
    {
        if (I == null) I = this; else Destroy(gameObject);
        
        if (debugMode)
            Debug.Log("GameManager: Awake() called");
    }

    void Start()
    {
        if (debugMode)
        {
            Debug.Log("GameManager: Start() called");
            DebugCheckReferences();
        }
        
        StartCoroutine(InitializeAfterLibrary());
        SetupDefaultExitPoints();
        SetupEventListeners();
        
        // Start the fallback system
        StartCoroutine(FallbackStartSystem());
    }

    void OnDestroy()
    {
        RemoveEventListeners();
    }

    void DebugCheckReferences()
    {
        Debug.Log("=== REFERENCE CHECK ===");
        Debug.Log($"GameTimer: {(gameTimer != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log($"DayManager: {(dayManager != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log($"Preparation Panel: {(preparationPanel != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log($"Day Complete Panel: {(dayCompletePanel != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log($"Day Failed Panel: {(dayFailedPanel != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log($"Score Text: {(scoreText != null ? "ASSIGNED" : "NULL - MISSING!")}");
        Debug.Log("========================");
    }

    void SetupEventListeners()
    {
        if (gameTimer != null)
            GameTimer.OnTimerFinished += OnDayTimerFinished;
        
        if (dayManager != null)
        {
            DayManager.OnDayStarted += OnDayStarted;
            DayManager.OnDayCompleted += OnDayCompleted;
        }
    }

    void RemoveEventListeners()
    {
        GameTimer.OnTimerFinished -= OnDayTimerFinished;
        DayManager.OnDayStarted -= OnDayStarted;
        DayManager.OnDayCompleted -= OnDayCompleted;
    }

    IEnumerator FallbackStartSystem()
    {
        yield return new WaitForSeconds(1f);
        
        if (currentState == GameState.Preparation)
        {
            Debug.Log("FALLBACK: Starting day system manually");
            
            // Create default day settings
            currentDaySettings = new DaySettings
            {
                dayNumber = 1,
                dayName = "Day 1",
                requiredCustomers = 5,
                dayDuration = 120f,
                customerSpawnInterval = 4f,
                maxQueueSize = 5,
                pointsPerCustomer = 100,
                wrongOrderPenalty = 25
            };
            
            StartPreparationPhase();
        }
    }

    void OnDayStarted(DaySettings daySettings)
    {
        currentDaySettings = daySettings;
        maxQueueSize = daySettings.maxQueueSize;
        spawnInterval = daySettings.customerSpawnInterval;
        scorePenalty = daySettings.wrongOrderPenalty;
        
        if (dayInfoText != null)
            dayInfoText.text = daySettings.dayName;
        
        UpdateObjectiveUI();
        StartPreparationPhase();
    }

    void StartPreparationPhase()
    {
        if (debugMode)
            Debug.Log("GameManager: StartPreparationPhase() called");
        
        currentState = GameState.Preparation;
        
        // Show preparation panel
        SetActivePanel(preparationPanel);
        
        if (gameTimer != null)
        {
            gameTimer.SetDayDuration(currentDaySettings?.dayDuration ?? 120f);
            Debug.Log($"GameTimer duration set to: {currentDaySettings?.dayDuration ?? 120f}");
        }
        else
        {
            Debug.LogError("GameTimer is NULL!");
        }
        
        // Start countdown
        StartCoroutine(WaitForGameStart());
    }

    IEnumerator WaitForGameStart()
    {
        if (debugMode)
            Debug.Log("GameManager: Starting 3 second countdown...");
        
        for (int i = 3; i > 0; i--)
        {
            Debug.Log($"Game starts in: {i}");
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("Starting day now!");
        StartDay();
    }

    public void StartDay()
    {
        if (debugMode)
            Debug.Log($"GameManager: StartDay() called. Current state: {currentState}");
        
        if (currentState != GameState.Preparation) 
        {
            Debug.LogWarning($"Cannot start day from state: {currentState}");
            return;
        }
        
        currentState = GameState.Playing;
        
        // Hide all panels
        SetActivePanel(null);
        
        // Reset stats
        score = 0;
        totalCustomersServed = 0;
        wrongDeliveries = 0;
        
        // Start timer
        if (gameTimer != null)
        {
            gameTimer.StartTimer();
            Debug.Log("GameTimer started");
        }
        else
        {
            Debug.LogError("Cannot start GameTimer - it's NULL!");
        }
        
        StartCustomerSpawning();
        UpdateScoreUI();
        
        Debug.Log($"Day started! Need {currentDaySettings?.requiredCustomers ?? 5} customers.");
    }

    void StartCustomerSpawning()
    {
        if (customerSpawningCoroutine != null)
            StopCoroutine(customerSpawningCoroutine);
        
        customerSpawningCoroutine = StartCoroutine(CustomerQueueManager());
        Debug.Log("Customer spawning started");
    }

    void OnDayTimerFinished()
    {
        if (currentState != GameState.Playing) return;
        
        Debug.Log("Day timer finished! Checking results...");
        StopCustomerSpawning();
        CheckDayCompletion();
    }

    void StopCustomerSpawning()
    {
        if (customerSpawningCoroutine != null)
        {
            StopCoroutine(customerSpawningCoroutine);
            customerSpawningCoroutine = null;
        }
    }

    void CheckDayCompletion()
    {
        bool success = totalCustomersServed >= (currentDaySettings?.requiredCustomers ?? 5);
        float timeRemaining = gameTimer != null ? gameTimer.TimeRemaining : 0f;
        
        DayResult result = new DayResult(success, totalCustomersServed, wrongDeliveries, score, timeRemaining);
        
        if (success)
        {
            OnDaySuccess(result);
        }
        else
        {
            OnDayFailed(result);
        }
        
        if (dayManager != null)
            dayManager.CompleteDayWithResult(result);
    }

    void OnDaySuccess(DayResult result)
    {
        currentState = GameState.DayComplete;
        
        Debug.Log($"Day {currentDaySettings?.dayNumber ?? 1} completed successfully!");
        
        // Show success panel
        SetActivePanel(dayCompletePanel);
        
        // Update success UI with results
        if (dayCompletePanel != null)
        {
            var resultText = dayCompletePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (resultText != null)
            {
                resultText.text = $"Day {currentDaySettings?.dayNumber ?? 1} Complete!\n\n" +
                                 $"Customers Served: {result.customersServed}/{currentDaySettings?.requiredCustomers ?? 5}\n" +
                                 $"Wrong Orders: {result.wrongOrders}\n" +
                                 $"Final Score: {result.finalScore}\n" +
                                 $"Time Bonus: {Mathf.FloorToInt(result.timeRemaining)} seconds";
            }
        }
    }

    void OnDayFailed(DayResult result)
    {
        currentState = GameState.DayFailed;
        
        Debug.Log($"Day {currentDaySettings?.dayNumber ?? 1} failed!");
        
        // Show failure panel
        SetActivePanel(dayFailedPanel);
        
        // Update failure UI with results
        if (dayFailedPanel != null)
        {
            var resultText = dayFailedPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (resultText != null)
            {
                int required = currentDaySettings?.requiredCustomers ?? 5;
                resultText.text = $"Day {currentDaySettings?.dayNumber ?? 1} Failed!\n\n" +
                                 $"Customers Served: {result.customersServed}/{required}\n" +
                                 $"You needed {required - result.customersServed} more customers!\n" +
                                 $"Wrong Orders: {result.wrongOrders}\n" +
                                 $"Final Score: {result.finalScore}";
            }
        }
    }

    void OnDayCompleted(DayResult result)
    {
        ClearAllCustomers();
    }

    // Helper method to manage panels
    void SetActivePanel(GameObject panelToShow)
    {
        if (preparationPanel != null) preparationPanel.SetActive(panelToShow == preparationPanel);
        if (dayCompletePanel != null) dayCompletePanel.SetActive(panelToShow == dayCompletePanel);
        if (dayFailedPanel != null) dayFailedPanel.SetActive(panelToShow == dayFailedPanel);
    }

    // UI Button Methods
    public void NextDay()
    {
        if (currentState != GameState.DayComplete) return;
        
        SetActivePanel(null);
        
        if (dayManager != null)
            dayManager.GoToNextDay();
        else
            RestartDay(); // Fallback
    }

    public void RestartDay()
    {
        SetActivePanel(null);
        ClearAllCustomers();
        
        if (dayManager != null)
            dayManager.RestartCurrentDay();
        else
            StartPreparationPhase(); // Fallback
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // All the rest of your existing methods...
    void SetupDefaultExitPoints()
    {
        if (satisfiedExitPoint == null)
        {
            GameObject satisfiedExit = new GameObject("Satisfied Customer Exit");
            satisfiedExit.transform.SetParent(transform);
            satisfiedExit.transform.position = defaultSatisfiedExit;
            satisfiedExitPoint = satisfiedExit.transform;
        }

        if (disappointedExitPoint == null)
        {
            GameObject disappointedExit = new GameObject("Disappointed Customer Exit");
            disappointedExit.transform.SetParent(transform);
            disappointedExit.transform.position = defaultDisappointedExit;
            disappointedExitPoint = disappointedExit.transform;
        }
    }
    
    IEnumerator InitializeAfterLibrary()
    {
        yield return null;
        
        if (BookLibrary.Instance != null)
        {
            BookLibrary.Instance.RefreshBookLibrary();
            Debug.Log("GameManager: Book library refreshed");
        }
        
        UpdateScoreUI();
    }

    IEnumerator CustomerQueueManager()
    {
        while (currentState == GameState.Playing)
        {
            if (GetTotalCustomerCount() < maxQueueSize)
            {
                SpawnNewCustomer();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    int GetTotalCustomerCount()
    {
        int count = customerQueue.Count;
        if (currentCustomer != null) count++;
        return count;
    }

    void SpawnNewCustomer()
    {
        if (spawner == null)
        {
            Debug.LogError("CustomerSpawner is NULL!");
            return;
        }

        Customer newCustomer = spawner.SpawnCustomer();
        if (newCustomer != null)
        {
            customerQueue.Add(newCustomer);
            PositionCustomerInQueue(newCustomer, customerQueue.Count - 1);
            
            if (currentCustomer == null)
            {
                AdvanceQueue();
            }
            
            Debug.Log($"Customer spawned. Queue size: {customerQueue.Count}");
        }
    }

    void PositionCustomerInQueue(Customer customer, int queuePosition)
    {
        Vector3 targetPosition = queueStartPosition + Vector3.right * (queuePosition * queueSpacing);
        customer.SetAsWaitingCustomer(queuePosition);
        StartCoroutine(customer.MoveToPosition(targetPosition));
    }

    public void AdvanceQueue()
    {
        if (customerQueue.Count == 0)
        {
            currentCustomer = null;
            return;
        }

        currentCustomer = customerQueue[0];
        customerQueue.RemoveAt(0);
        currentCustomer.SetAsCurrentCustomer();
        StartCoroutine(currentCustomer.MoveToPosition(servicePosition));

        for (int i = 0; i < customerQueue.Count; i++)
        {
            PositionCustomerInQueue(customerQueue[i], i);
        }
    }

    public void CompleteCustomerAndQueueNext()
    {
        if (currentCustomer != null)
        {
            currentCustomer = null;
        }

        totalCustomersServed++;
        AddScore(currentDaySettings?.pointsPerCustomer ?? 100);
        
        // Check for early completion
        if (currentDaySettings != null && totalCustomersServed >= currentDaySettings.requiredCustomers)
        {
            if (gameTimer != null && gameTimer.HasTimeLeft)
            {
                Debug.Log("Day completed early!");
                // Could end immediately or let timer run for bonus points
            }
        }
        
        AdvanceQueue();
        UpdateScoreUI();
        
        Debug.Log($"Customer served! Total: {totalCustomersServed}");
    }

    public void HandleFailedDelivery(Customer disappointedCustomer)
    {
        wrongDeliveries++;
        AddScore(-scorePenalty);
        
        if (customerQueue.Contains(disappointedCustomer))
        {
            customerQueue.Remove(disappointedCustomer);
            for (int i = 0; i < customerQueue.Count; i++)
            {
                PositionCustomerInQueue(customerQueue[i], i);
            }
        }
        
        if (currentCustomer == disappointedCustomer)
        {
            currentCustomer = null;
            StartCoroutine(AdvanceQueueAfterDelay());
        }
        
        UpdateScoreUI();
    }

    IEnumerator AdvanceQueueAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        AdvanceQueue();
    }

    public Vector3 GetSatisfiedExitPosition()
    {
        return satisfiedExitPoint != null ? satisfiedExitPoint.position : defaultSatisfiedExit;
    }

    public Vector3 GetDisappointedExitPosition()
    {
        return disappointedExitPoint != null ? disappointedExitPoint.position : defaultDisappointedExit;
    }

    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) 
        {
            string currentOrder = currentCustomer != null ? currentCustomer.order.ToString() : "None";
            scoreText.text = $"Score: {score}\nServed: {totalCustomersServed}\nWrong: {wrongDeliveries}\nQueue: {customerQueue.Count}\nCurrent: {currentOrder}";
        }
        
        UpdateObjectiveUI();
    }

    void UpdateObjectiveUI()
    {
        if (objectiveText != null && currentDaySettings != null)
        {
            int remaining = Mathf.Max(0, currentDaySettings.requiredCustomers - totalCustomersServed);
            objectiveText.text = $"Goal: {totalCustomersServed}/{currentDaySettings.requiredCustomers} customers\nRemaining: {remaining}";
        }
    }

    void ClearAllCustomers()
    {
        foreach (var customer in customerQueue)
        {
            if (customer != null)
                Destroy(customer.gameObject);
        }
        customerQueue.Clear();
        
        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }
    }

    public void CleanupWrongBook(Book wrongBook)
    {
        if (wrongBook != null)
        {
            Debug.Log($"Cleaning up wrong book: {wrongBook.genre} with {wrongBook.cover} cover");
            Destroy(wrongBook.gameObject, 0.5f);
        }
    }

    // Manual controls for testing
    [ContextMenu("Force Start Day")]
    public void ForceStartDay()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Forcing day start...");
            StartDay();
        }
    }

    [ContextMenu("Force Day Success")]
    public void DebugForceSuccess()
    {
        if (Application.isPlaying)
        {
            totalCustomersServed = currentDaySettings?.requiredCustomers ?? 5;
            CheckDayCompletion();
        }
    }

    [ContextMenu("Force Day Failure")]
    public void DebugForceFailure()
    {
        if (Application.isPlaying)
        {
            OnDayTimerFinished();
        }
    }
}
