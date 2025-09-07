using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Refs")]
    public CustomerSpawner spawner;
    public TextMeshProUGUI scoreText;
    public BookLibrary bookLibrary;

    [Header("Customer Exit Positions")]
    [Tooltip("Where satisfied customers exit to (usually right side)")]
    public Transform satisfiedExitPoint;
    [Tooltip("Where disappointed customers exit to (usually left side)")]
    public Transform disappointedExitPoint;
    [Tooltip("Default exit positions if transforms not assigned")]
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

    [Header("Runtime")]
    public Customer currentCustomer;
    public List<Customer> customerQueue = new List<Customer>();
    public int score;
    public int totalCustomersServed;
    public int wrongDeliveries;

    [Header("Debug")]
    [SerializeField] private bool showQueueDebug = true;

    void Awake()
    {
        if (I == null) I = this; else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(InitializeAfterLibrary());
        SetupDefaultExitPoints();
    }

    void SetupDefaultExitPoints()
    {
        // Create default exit points if not assigned
        if (satisfiedExitPoint == null)
        {
            GameObject satisfiedExit = new GameObject("Satisfied Customer Exit");
            satisfiedExit.transform.SetParent(transform);
            satisfiedExit.transform.position = defaultSatisfiedExit;
            satisfiedExitPoint = satisfiedExit.transform;
            
            if (showQueueDebug)
                Debug.Log("Created default satisfied customer exit point");
        }

        if (disappointedExitPoint == null)
        {
            GameObject disappointedExit = new GameObject("Disappointed Customer Exit");
            disappointedExit.transform.SetParent(transform);
            disappointedExit.transform.position = defaultDisappointedExit;
            disappointedExitPoint = disappointedExit.transform;
            
            if (showQueueDebug)
                Debug.Log("Created default disappointed customer exit point");
        }
    }
    
    IEnumerator InitializeAfterLibrary()
    {
        yield return null; // Wait one frame
        
        // Ensure book library is refreshed
        if (BookLibrary.Instance != null)
        {
            BookLibrary.Instance.RefreshBookLibrary();
            Debug.Log("GameManager: Book library refreshed before spawning customers");
        }
        
        // Start the customer queue system
        StartCoroutine(CustomerQueueManager());
        UpdateScoreUI();
    }

    IEnumerator CustomerQueueManager()
    {
        while (true)
        {
            // Spawn new customers if queue isn't full
            if (GetTotalCustomerCount() < maxQueueSize)
            {
                SpawnNewCustomer();
            }
            
            // Wait before potentially spawning another
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
        Customer newCustomer = spawner.SpawnCustomer();
        if (newCustomer != null)
        {
            customerQueue.Add(newCustomer);
            
            // Position the customer at the back of the queue
            PositionCustomerInQueue(newCustomer, customerQueue.Count - 1);
            
            // If this is the first customer and no one is being served, make them current
            if (currentCustomer == null)
            {
                AdvanceQueue();
            }
            
            if (showQueueDebug)
            {
                Debug.Log($"Added customer to queue. Queue size: {customerQueue.Count}, Total customers: {GetTotalCustomerCount()}");
            }
        }
    }

    void PositionCustomerInQueue(Customer customer, int queuePosition)
    {
        Vector3 targetPosition = queueStartPosition + Vector3.right * (queuePosition * queueSpacing);
        
        // Set as waiting customer
        customer.SetAsWaitingCustomer(queuePosition);
        
        // Move customer to queue position
        StartCoroutine(customer.MoveToPosition(targetPosition));
    }

    public void AdvanceQueue()
    {
        if (customerQueue.Count == 0)
        {
            currentCustomer = null;
            if (showQueueDebug)
                Debug.Log("No customers in queue");
            return;
        }

        // Set the first customer in queue as current
        currentCustomer = customerQueue[0];
        customerQueue.RemoveAt(0);

        // Make them the current customer (this will show their order)
        currentCustomer.SetAsCurrentCustomer();

        // Move current customer to service position
        StartCoroutine(currentCustomer.MoveToPosition(servicePosition));

        // Advance all remaining customers in queue
        for (int i = 0; i < customerQueue.Count; i++)
        {
            PositionCustomerInQueue(customerQueue[i], i);
        }

        if (showQueueDebug)
        {
            Debug.Log($"Advanced queue. Current customer wants: {currentCustomer?.order}, Queue size: {customerQueue.Count}");
        }
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
    }

    public void CompleteCustomerAndQueueNext()
    {
        if (currentCustomer != null)
        {
            // Customer will handle their own success animation and removal
            currentCustomer = null;
        }

        totalCustomersServed++;
        AddScore(100);
        Debug.Log($"Customer #{totalCustomersServed} served successfully! +100 points");
        
        // Advance to next customer in queue
        AdvanceQueue();
        UpdateScoreUI();
    }

    public void HandleFailedDelivery(Customer disappointedCustomer)
    {
        wrongDeliveries++;
        AddScore(-scorePenalty);
        Debug.Log($"Wrong delivery #{wrongDeliveries}! -{scorePenalty} points");
        
        // Remove from queue if they're in it
        if (customerQueue.Contains(disappointedCustomer))
        {
            customerQueue.Remove(disappointedCustomer);
            // Reposition remaining customers
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
        // Customer will destroy themselves after animation
    }

    IEnumerator AdvanceQueueAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        AdvanceQueue();
        Debug.Log("Advanced to next customer after failed delivery!");
    }

    public void CleanupWrongBook(Book wrongBook)
    {
        if (wrongBook != null)
        {
            Debug.Log($"Cleaning up wrong book: {wrongBook.genre} with {wrongBook.cover} cover");
            Destroy(wrongBook.gameObject, 0.5f);
        }
    }

    // Visual helpers in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw satisfied exit point
        Vector3 satisfiedPos = GetSatisfiedExitPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(satisfiedPos, 0.7f);
        Gizmos.DrawLine(servicePosition, satisfiedPos);
        
        // Draw disappointed exit point
        Vector3 disappointedPos = GetDisappointedExitPosition();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(disappointedPos, 0.7f);
        Gizmos.DrawLine(servicePosition, disappointedPos);
        
        // Draw service position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(servicePosition, 0.5f);
        
        // Draw queue positions
        Gizmos.color = Color.yellow;
        for (int i = 0; i < maxQueueSize; i++)
        {
            Vector3 queuePos = queueStartPosition + Vector3.right * (i * queueSpacing);
            Gizmos.DrawWireCube(queuePos, Vector3.one * 0.3f);
        }
    }

    // Debug methods
    [ContextMenu("Add Customer to Queue")]
    public void DebugAddCustomer()
    {
        if (Application.isPlaying)
        {
            SpawnNewCustomer();
        }
    }

    [ContextMenu("Clear All Customers")]
    public void DebugClearAllCustomers()
    {
        if (Application.isPlaying)
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
            
            UpdateScoreUI();
        }
    }

    [ContextMenu("Show Exit Positions")]
    public void DebugShowExitPositions()
    {
        Debug.Log($"Satisfied Exit: {GetSatisfiedExitPosition()}");
        Debug.Log($"Disappointed Exit: {GetDisappointedExitPosition()}");
    }
}
