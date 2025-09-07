using UnityEngine;
using System.Collections;

[System.Serializable]
public class ShelfBookData
{
    [Header("Basic Info")]
    public string genre = "Fantasy";
    public string shelfDisplayName = "Fantasy Shelf";
    
    [Header("Available Book Covers")]
    [Tooltip("Edit these cover names in the inspector. Customers will order these specific covers.")]
    public string[] availableCovers = { "Dragon", "Sword", "Crystal", "Castle" };
    
    [Header("Cover Colors")]
    [Tooltip("Colors for each cover. Should match the number of covers.")]
    public Color[] bookColors = { 
        new Color(0.2f, 0.4f, 0.8f),     // Dragon - Blue
        new Color(0.6f, 0.6f, 0.6f),     // Sword - Silver  
        new Color(0.5f, 0.2f, 0.8f),     // Crystal - Purple
        new Color(0.4f, 0.3f, 0.2f)      // Castle - Brown
    };
}

public class ShelfStation : MonoBehaviour
{
    [Header("Shelf Configuration")]
    [SerializeField] 
    [Tooltip("Configure your book covers and colors here. This data will be used for customer orders.")]
    public ShelfBookData shelfData = new ShelfBookData();
    
    [Header("Book Spawning")]
    public GameObject bookPrefab;
    public Transform spawnPoint;
    
    [Header("Pickup Animation")]
    [SerializeField] private bool enablePickupAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float bounceHeight = 0.5f;
    [SerializeField] private Color flashColor = Color.yellow;
    
    [Header("Inspector Tools")]
    [SerializeField] private bool autoConfigureOnStart = true;
    [Tooltip("Click to reset shelf data based on GameObject name")]
    [SerializeField] private bool resetToDefaults = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        // Only auto-configure if enabled and data is empty/default
        if (autoConfigureOnStart && IsDataEmpty())
        {
            ConfigureShelfFromName();
        }
        
        // Validate data
        ValidateShelfData();
        
        if (!gameObject.CompareTag("StationZone"))
        {
            gameObject.tag = "StationZone";
            Debug.Log($"Updated {gameObject.name} tag to 'StationZone'");
        }
    }

    private void OnValidate()
    {
        // Reset to defaults if requested
        if (resetToDefaults)
        {
            resetToDefaults = false;
            ConfigureShelfFromName();
        }
        
        ValidateShelfData();
    }

    bool IsDataEmpty()
    {
        return shelfData == null || 
               string.IsNullOrEmpty(shelfData.genre) || 
               shelfData.availableCovers == null || 
               shelfData.availableCovers.Length == 0;
    }

    void ValidateShelfData()
    {
        if (shelfData == null) 
        {
            shelfData = new ShelfBookData();
            return;
        }

        // Ensure we have at least one cover
        if (shelfData.availableCovers == null || shelfData.availableCovers.Length == 0)
        {
            shelfData.availableCovers = new string[] { "Default" };
        }

        // Ensure colors match covers count
        if (shelfData.bookColors == null || shelfData.bookColors.Length < shelfData.availableCovers.Length)
        {
            Color[] newColors = new Color[shelfData.availableCovers.Length];
            for (int i = 0; i < newColors.Length; i++)
            {
                if (shelfData.bookColors != null && i < shelfData.bookColors.Length)
                {
                    newColors[i] = shelfData.bookColors[i];
                }
                else
                {
                    newColors[i] = Color.white;
                }
            }
            shelfData.bookColors = newColors;
        }
    }

    void ConfigureShelfFromName()
    {
        string objectName = gameObject.name.ToLower();
        
        if (objectName.Contains("fantasy"))
        {
            shelfData.genre = "Fantasy";
            shelfData.shelfDisplayName = "Fantasy Shelf";
            shelfData.availableCovers = new string[] { "Dragon", "Sword", "Crystal", "Castle" };
            shelfData.bookColors = new Color[] { 
                new Color(0.2f, 0.4f, 0.8f),     // Dragon - Blue
                new Color(0.6f, 0.6f, 0.6f),     // Sword - Silver
                new Color(0.5f, 0.2f, 0.8f),     // Crystal - Purple
                new Color(0.4f, 0.3f, 0.2f)      // Castle - Brown
            };
        }
        else if (objectName.Contains("romance"))
        {
            shelfData.genre = "Romance";
            shelfData.shelfDisplayName = "Romance Shelf";
            shelfData.availableCovers = new string[] { "Rose", "Heart", "Ring", "Sunset" };
            shelfData.bookColors = new Color[] { 
                new Color(0.9f, 0.1f, 0.4f),     // Rose - Deep Pink
                new Color(0.8f, 0.2f, 0.2f),     // Heart - Red
                new Color(0.9f, 0.8f, 0.1f),     // Ring - Gold
                new Color(0.9f, 0.5f, 0.2f)      // Sunset - Orange
            };
        }
        else if (objectName.Contains("mystery"))
        {
            shelfData.genre = "Mystery";
            shelfData.shelfDisplayName = "Mystery Shelf";
            shelfData.availableCovers = new string[] { "Skull", "Key", "Magnifier", "Shadow" };
            shelfData.bookColors = new Color[] { 
                new Color(0.2f, 0.2f, 0.2f),     // Skull - Dark Gray
                new Color(0.6f, 0.4f, 0.1f),     // Key - Bronze
                new Color(0.7f, 0.7f, 0.7f),     // Magnifier - Light Gray
                new Color(0.1f, 0.1f, 0.3f)      // Shadow - Dark Blue
            };
        }
        else
        {
            shelfData.genre = "General";
            shelfData.shelfDisplayName = "General Shelf";
            shelfData.availableCovers = new string[] { "Plain" };
            shelfData.bookColors = new Color[] { Color.white };
        }
        
        Debug.Log($"Auto-configured {gameObject.name} as {shelfData.genre} shelf with {shelfData.availableCovers.Length} covers");
    }

    public Book CreateBook(Vector3 position)
    {
        if (bookPrefab == null)
        {
            Debug.LogError($"No book prefab assigned to {shelfData.shelfDisplayName}!");
            return null;
        }

        if (shelfData.availableCovers == null || shelfData.availableCovers.Length == 0)
        {
            Debug.LogError($"No covers available for {shelfData.shelfDisplayName}!");
            return null;
        }

        // Trigger pickup animation
        if (enablePickupAnimation)
        {
            StartCoroutine(PlayPickupAnimation());
        }

        // Randomly choose from available covers
        int randomIndex = Random.Range(0, shelfData.availableCovers.Length);
        string selectedCover = shelfData.availableCovers[randomIndex];
        Color selectedColor = shelfData.bookColors[Mathf.Min(randomIndex, shelfData.bookColors.Length - 1)];

        Vector3 spawnPosition = spawnPoint ? spawnPoint.position : position;
        GameObject bookObject = Instantiate(bookPrefab, spawnPosition, Quaternion.identity);
        Book book = bookObject.GetComponent<Book>();

        if (book != null)
        {
            book.Setup(shelfData.genre, selectedCover, selectedColor, shelfData.shelfDisplayName);
            
            // Add book spawn animation
            if (enablePickupAnimation)
            {
                StartCoroutine(AnimateBookSpawn(book.transform));
            }
            
            Debug.Log($"Picked up {book.GetStatusDescription()} from {shelfData.shelfDisplayName}");
        }
        else
        {
            Debug.LogError("Book prefab doesn't have a Book component!");
        }

        return book;
    }

    IEnumerator PlayPickupAnimation()
    {
        if (spriteRenderer == null) yield break;

        // Flash the shelf color
        spriteRenderer.color = flashColor;
        
        // Bounce animation
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration / 2);
            
            // Scale up
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            
            // Color lerp back to original
            spriteRenderer.color = Color.Lerp(flashColor, originalColor, progress);
            
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration / 2);
            
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            
            yield return null;
        }
        
        // Ensure we're back to original state
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;
    }

    IEnumerator AnimateBookSpawn(Transform bookTransform)
    {
        if (bookTransform == null) yield break;

        Vector3 startPos = bookTransform.position;
        Vector3 peakPos = startPos + Vector3.up * bounceHeight;
        Vector3 endPos = startPos;

        // Bounce up
        float elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration / 2);
            
            bookTransform.position = Vector3.Lerp(startPos, peakPos, progress);
            
            yield return null;
        }

        // Bounce down
        elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration / 2);
            
            bookTransform.position = Vector3.Lerp(peakPos, endPos, progress);
            
            yield return null;
        }

        bookTransform.position = endPos;
    }

    public string GetShelfDescription()
    {
        return $"{shelfData.shelfDisplayName} - Contains {shelfData.genre} books: {string.Join(", ", shelfData.availableCovers)}";
    }

    public string[] GetAllCovers()
    {
        return shelfData.availableCovers;
    }

    public Color[] GetAllColors()
    {
        return shelfData.bookColors;
    }

    // Inspector helper methods
    [ContextMenu("Log Available Covers")]
    public void LogAvailableCovers()
    {
        Debug.Log($"{shelfData.shelfDisplayName} covers: {string.Join(", ", shelfData.availableCovers)}");
    }

    [ContextMenu("Test Pickup Animation")]
    public void TestPickupAnimation()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(PlayPickupAnimation());
        }
    }
}
