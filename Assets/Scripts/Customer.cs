using UnityEngine;
using TMPro;
using System.Collections;

public class Customer : MonoBehaviour
{
    [Header("Customer Data")]
    public Order order;
    public TextMeshPro orderText;

    [Header("Queue Settings")]
    [SerializeField] private bool isCurrentCustomer = false;
    [SerializeField] private int queuePosition = 0;

    [Header("Animation Settings")]
    public float moveSpeed = 3f;
    public float disappointmentTime = 1f;
    
    [Header("Success Animation")]
    [SerializeField] private bool enableSuccessAnimation = true;
    [SerializeField] private float successAnimationDuration = 1f;
    [SerializeField] private float jumpHeight = 0.8f;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private float celebrationTime = 1.5f;
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color disappointedColor = Color.red;
    public Color waitingColor = Color.gray;
    
    private SpriteRenderer spriteRenderer;
    private bool isBeingServed = false;
    private bool isDisappointed = false;
    private Vector3 originalPosition;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = waitingColor; // Start as waiting
        originalPosition = transform.position;
    }

    public void Setup(Order newOrder)
    {
        order = newOrder;
        gameObject.tag = "Customer";
        isBeingServed = false;
        isDisappointed = false;
        originalPosition = transform.position;
        
        // Don't show order initially - will be shown when they become current
        SetAsWaitingCustomer();
    }

    public void SetAsCurrentCustomer()
    {
        isCurrentCustomer = true;
        queuePosition = 0;
        
        // Show the order and change color to indicate they're active
        if (orderText != null)
            orderText.text = order.ToString();
        
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
        
        Debug.Log($"Customer is now current: {order}");
    }

    public void SetAsWaitingCustomer(int position = -1)
    {
        isCurrentCustomer = false;
        queuePosition = position;
        
        // Hide the order and show as waiting
        if (orderText != null)
            orderText.text = "Waiting...";
        
        if (spriteRenderer != null)
            spriteRenderer.color = waitingColor;
    }

    public bool TryFulfill(Book book)
    {
        if (book == null || isBeingServed || isDisappointed || !isCurrentCustomer) 
            return false;
        
        isBeingServed = true;
        
        // Check if book matches requirements
        bool isCorrect = book.isCheckedOut && 
                        book.genre == order.genre && 
                        book.cover == order.cover;
        
        if (isCorrect)
        {
            Debug.Log($"Customer satisfied! Received correct {book.genre} book with {book.cover} cover.");
            
            // Play success animation
            if (enableSuccessAnimation)
            {
                StartCoroutine(PlaySuccessAnimation());
            }
            
            return true;
        }
        else
        {
            // Handle wrong delivery
            string reason = "";
            if (!book.isCheckedOut) reason = "Book not stamped!";
            else if (book.genre != order.genre) reason = $"Wrong genre! Wanted {order.genre}, got {book.genre}";
            else if (book.cover != order.cover) reason = $"Wrong cover! Wanted {order.cover}, got {book.cover}";
            
            Debug.Log($"Customer disappointed: {reason}");
            StartCoroutine(HandleWrongDelivery());
            return false;
        }
    }

    IEnumerator PlaySuccessAnimation()
    {
        // Change color to success color
        if (spriteRenderer != null)
            spriteRenderer.color = successColor;
        
        // Update text to show happiness
        if (orderText != null)
            orderText.text = "Thank you! ★";
        
        // Jump animation
        yield return StartCoroutine(PlayJumpAnimation());
        
        // Celebration sparkle effect
        yield return StartCoroutine(PlaySparkleEffect());
        
        // Wait a bit for the player to see the success
        yield return new WaitForSeconds(celebrationTime);
        
        // Move to satisfied exit position
        yield return StartCoroutine(MoveToExitPosition(true));
    }

    IEnumerator PlayJumpAnimation()
    {
        Vector3 startPos = originalPosition;
        Vector3 peakPos = startPos + Vector3.up * jumpHeight;
        
        // Jump up
        float elapsed = 0f;
        while (elapsed < successAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (successAnimationDuration / 2);
            
            // Use easing for smooth jump
            float easedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, peakPos, easedProgress);
            
            // Scale bounce
            float scaleMultiplier = 1f + (easedProgress * 0.2f);
            transform.localScale = Vector3.one * scaleMultiplier;
            
            yield return null;
        }
        
        // Jump down
        elapsed = 0f;
        while (elapsed < successAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (successAnimationDuration / 2);
            
            float easedProgress = Mathf.Cos(progress * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(peakPos, startPos, progress);
            
            // Scale bounce back
            float scaleMultiplier = 1f + (easedProgress * 0.2f);
            transform.localScale = Vector3.one * scaleMultiplier;
            
            yield return null;
        }
        
        transform.position = startPos;
        transform.localScale = Vector3.one;
    }

    IEnumerator PlaySparkleEffect()
    {
        // Create simple sparkle effect by scaling up and down quickly
        for (int i = 0; i < 3; i++)
        {
            // Flash bright
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
            
            transform.localScale = Vector3.one * 1.3f;
            yield return new WaitForSeconds(0.1f);
            
            // Back to success color
            if (spriteRenderer != null)
                spriteRenderer.color = successColor;
            
            transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator MoveToExitPosition(bool satisfied)
    {
        Vector3 startPosition = transform.position;
        Vector3 exitPosition;
        
        if (GameManager.I != null)
        {
            exitPosition = satisfied ? GameManager.I.GetSatisfiedExitPosition() : GameManager.I.GetDisappointedExitPosition();
        }
        else
        {
            // Fallback positions
            exitPosition = satisfied ? new Vector3(15f, startPosition.y, startPosition.z) : new Vector3(-15f, startPosition.y, startPosition.z);
        }
        
        float elapsedTime = 0f;
        float totalTime = Vector3.Distance(startPosition, exitPosition) / moveSpeed;
        
        while (elapsedTime < totalTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / totalTime;
            transform.position = Vector3.Lerp(startPosition, exitPosition, progress);
            yield return null;
        }
        
        transform.position = exitPosition;
        
        // Destroy the customer once they reach the exit
        Destroy(gameObject);
    }

    IEnumerator HandleWrongDelivery()
    {
        isDisappointed = true;
        
        // Show disappointment visually
        if (spriteRenderer != null)
            spriteRenderer.color = disappointedColor;
        
        if (orderText != null)
            orderText.text = "Wrong book! ✗";
        
        // Shake animation for disappointment
        yield return StartCoroutine(PlayShakeAnimation());
        
        // Wait for disappointment reaction
        yield return new WaitForSeconds(disappointmentTime);
        
        // Move to disappointed exit position
        yield return StartCoroutine(MoveToExitPosition(false));
        
        // Notify GameManager about the failed delivery
        if (GameManager.I != null)
            GameManager.I.HandleFailedDelivery(this);
    }

    IEnumerator PlayShakeAnimation()
    {
        Vector3 startPos = transform.position;
        float shakeIntensity = 0.1f;
        float shakeDuration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0
            );
            
            transform.position = startPos + randomOffset;
            yield return null;
        }
        
        transform.position = startPos;
    }

    // Movement to queue position
    public IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }
        
        transform.position = targetPosition;
        originalPosition = targetPosition;
    }

    public bool IsDisappointed()
    {
        return isDisappointed;
    }

    public bool IsBeingServed()
    {
        return isBeingServed;
    }

    public bool IsCurrentCustomer()
    {
        return isCurrentCustomer;
    }

    public int GetQueuePosition()
    {
        return queuePosition;
    }

    // Test methods for the inspector
    [ContextMenu("Test Success Animation")]
    public void TestSuccessAnimation()
    {
        if (Application.isPlaying && enableSuccessAnimation)
        {
            StartCoroutine(PlaySuccessAnimation());
        }
    }

    [ContextMenu("Make Current Customer")]
    public void DebugMakeCurrent()
    {
        if (Application.isPlaying)
        {
            SetAsCurrentCustomer();
        }
    }
}
