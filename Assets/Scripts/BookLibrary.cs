using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BookOption
{
    public string genre;
    public string cover;
    public Color color;
    
    public BookOption(string genre, string cover, Color color)
    {
        this.genre = genre;
        this.cover = cover;
        this.color = color;
    }
    
    public override string ToString()
    {
        return $"{genre} - {cover}";
    }
}

public class BookLibrary : MonoBehaviour
{
    public static BookLibrary Instance;
    
    [Header("Shelf References")]
    [Tooltip("Drag your Shelf_Fantasy and Shelf_Romance GameObjects here")]
    public ShelfStation[] shelfStations;
    
    [Header("Generated Book Options")]
    [SerializeField] private List<BookOption> availableBooks = new List<BookOption>();
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        RefreshBookLibrary();
    }
    
    public void RefreshBookLibrary()
    {
        availableBooks.Clear();
        
        if (shelfStations == null || shelfStations.Length == 0)
        {
            Debug.LogWarning("BookLibrary: No shelf stations assigned! Please drag your Shelf_Fantasy and Shelf_Romance GameObjects to the Shelf Stations array.");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"BookLibrary: Reading data from {shelfStations.Length} manually assigned shelf station(s)");
        }
        
        foreach (var shelf in shelfStations)
        {
            if (shelf == null)
            {
                Debug.LogWarning("BookLibrary: One of the shelf stations is null (empty slot in array)");
                continue;
            }
            
            if (shelf.shelfData == null)
            {
                Debug.LogWarning($"BookLibrary: {shelf.gameObject.name} has no shelfData configured");
                continue;
            }
            
            string genre = shelf.shelfData.genre;
            string[] covers = shelf.shelfData.availableCovers;
            Color[] colors = shelf.shelfData.bookColors;
            
            // Skip "General" genre books
            if (genre == "General" || string.IsNullOrEmpty(genre))
            {
                if (showDebugInfo)
                    Debug.Log($"Skipping {shelf.gameObject.name} - General genre not used for customer orders");
                continue;
            }
            
            if (covers == null || covers.Length == 0)
            {
                Debug.LogWarning($"BookLibrary: {shelf.gameObject.name} has no covers configured");
                continue;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"Processing {shelf.gameObject.name}: {genre} with covers [{string.Join(", ", covers)}]");
            }
            
            for (int i = 0; i < covers.Length; i++)
            {
                if (string.IsNullOrEmpty(covers[i])) continue;
                
                Color bookColor = (colors != null && i < colors.Length) ? colors[i] : Color.white;
                BookOption bookOption = new BookOption(genre, covers[i], bookColor);
                
                availableBooks.Add(bookOption);
                
                if (showDebugInfo)
                {
                    Debug.Log($"  Added: {bookOption}");
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"BookLibrary initialized with {availableBooks.Count} total book options");
        }
        
        if (availableBooks.Count == 0)
        {
            Debug.LogError("BookLibrary: No valid books found! Check your shelf station configurations.");
        }
    }
    
    public BookOption GetRandomBookOption()
    {
        if (availableBooks.Count == 0)
        {
            Debug.LogWarning("No books available in library! Using fallback.");
            return new BookOption("Fantasy", "Dragon", Color.blue);
        }
        
        return availableBooks[Random.Range(0, availableBooks.Count)];
    }
    
    public List<BookOption> GetAvailableBooks()
    {
        return new List<BookOption>(availableBooks);
    }
    
    [ContextMenu("Refresh Library Now")]
    public void ForceRefresh()
    {
        RefreshBookLibrary();
    }
    
    [ContextMenu("Log All Books")]
    public void LogAllBooks()
    {
        Debug.Log($"=== Available Books ({availableBooks.Count} total) ===");
        foreach (var book in availableBooks)
        {
            Debug.Log($"  {book}");
        }
    }
}
