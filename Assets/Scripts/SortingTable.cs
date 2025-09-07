using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GenreData
{
    public string genreName;
    public string[] covers;
    public Color[] colors;
    
    public GenreData(string name, string[] coverNames, Color[] coverColors)
    {
        genreName = name;
        covers = coverNames;
        colors = coverColors;
    }
}

public class SortingTable : MonoBehaviour
{
    [Header("Sorting Configuration")]
    [Tooltip("Press E to cycle through covers, hold Shift+E to change genre")]
    public bool allowGenreChanging = true;
    
    [Header("Available Genres and Covers")]
    [SerializeField] private List<GenreData> availableGenres = new List<GenreData>();
    
    [Header("References")]
    [Tooltip("Drag your shelf stations here to auto-populate cover data")]
    public ShelfStation[] shelfReferences;
    
    [Header("Manual Cover Data (Fallback)")]
    [SerializeField] private string[] fantasyCovers = { "Dragon", "Sword", "Crystal", "Castle" };
    [SerializeField] private Color[] fantasyColors = { 
        new Color(0.2f, 0.4f, 0.8f),     // Dragon - Blue
        new Color(0.6f, 0.6f, 0.6f),     // Sword - Silver
        new Color(0.5f, 0.2f, 0.8f),     // Crystal - Purple
        new Color(0.4f, 0.3f, 0.2f)      // Castle - Brown
    };
    
    [SerializeField] private string[] romanceCovers = { "Rose", "Heart", "Ring", "Sunset" };
    [SerializeField] private Color[] romanceColors = { 
        new Color(0.9f, 0.1f, 0.4f),     // Rose - Deep Pink
        new Color(0.8f, 0.2f, 0.2f),     // Heart - Red
        new Color(0.9f, 0.8f, 0.1f),     // Ring - Gold
        new Color(0.9f, 0.5f, 0.2f)      // Sunset - Orange
    };

    void Start()
    {
        RefreshGenreData();
    }

    void RefreshGenreData()
    {
        availableGenres.Clear();
        
        // Try to get data from shelf references first
        if (shelfReferences != null && shelfReferences.Length > 0)
        {
            foreach (var shelf in shelfReferences)
            {
                if (shelf != null && shelf.shelfData != null && 
                    !string.IsNullOrEmpty(shelf.shelfData.genre) && 
                    shelf.shelfData.genre != "General")
                {
                    GenreData genreData = new GenreData(
                        shelf.shelfData.genre,
                        shelf.shelfData.availableCovers,
                        shelf.shelfData.bookColors
                    );
                    availableGenres.Add(genreData);
                    Debug.Log($"SortingTable: Added {shelf.shelfData.genre} with {shelf.shelfData.availableCovers.Length} covers from shelf reference");
                }
            }
        }
        
        // Fallback to manual data if no shelf references or they're empty
        if (availableGenres.Count == 0)
        {
            Debug.Log("SortingTable: Using manual fallback cover data");
            availableGenres.Add(new GenreData("Fantasy", fantasyCovers, fantasyColors));
            availableGenres.Add(new GenreData("Romance", romanceCovers, romanceColors));
        }
    }

    public void CycleCover(Book book)
    {
        if (book == null) return;

        // Check if player wants to change genre (Shift+E)
        if (allowGenreChanging && Input.GetKey(KeyCode.LeftShift))
        {
            ChangeGenre(book);
            return;
        }

        // Otherwise, cycle through covers of current genre
        CycleCoverInCurrentGenre(book);
    }

    void CycleCoverInCurrentGenre(Book book)
    {
        GenreData currentGenreData = GetGenreData(book.genre);
        
        if (currentGenreData == null)
        {
            Debug.LogWarning($"No cover data available for {book.genre} books!");
            return;
        }

        // Find current cover index
        int currentIndex = System.Array.IndexOf(currentGenreData.covers, book.cover);
        if (currentIndex == -1) currentIndex = 0; // Default to first if not found

        // Cycle to next cover (wrap around)
        int nextIndex = (currentIndex + 1) % currentGenreData.covers.Length;
        
        // Update the book's cover and color
        string newCover = currentGenreData.covers[nextIndex];
        Color newColor = nextIndex < currentGenreData.colors.Length ? currentGenreData.colors[nextIndex] : Color.white;
        
        book.ChangeCover(newCover, newColor);
        
        Debug.Log($"Cycled {book.genre} cover: {currentGenreData.covers[currentIndex]} → {newCover}");
    }

    void ChangeGenre(Book book)
    {
        if (availableGenres.Count < 2)
        {
            Debug.LogWarning("Not enough genres available to change!");
            return;
        }

        // Find current genre index
        int currentGenreIndex = -1;
        for (int i = 0; i < availableGenres.Count; i++)
        {
            if (availableGenres[i].genreName == book.genre)
            {
                currentGenreIndex = i;
                break;
            }
        }

        // Move to next genre (wrap around)
        int nextGenreIndex = (currentGenreIndex + 1) % availableGenres.Count;
        GenreData newGenreData = availableGenres[nextGenreIndex];

        // Change to first cover of the new genre
        string newGenre = newGenreData.genreName;
        string newCover = newGenreData.covers[0];
        Color newColor = newGenreData.colors.Length > 0 ? newGenreData.colors[0] : Color.white;

        // Update the book's genre, cover, and color
        book.ChangeGenreAndCover(newGenre, newCover, newColor);
        
        Debug.Log($"Changed book genre: {book.genre} → {newGenre} ({newCover})");
    }

    GenreData GetGenreData(string genreName)
    {
        foreach (var genreData in availableGenres)
        {
            if (genreData.genreName == genreName)
                return genreData;
        }
        return null;
    }

    public string GetAvailableInfo()
    {
        string info = "Available sorting options:\n";
        foreach (var genre in availableGenres)
        {
            info += $"{genre.genreName}: {string.Join(", ", genre.covers)}\n";
        }
        info += "\nControls: E = cycle cover, Shift+E = change genre";
        return info;
    }

    // Context menu for testing
    [ContextMenu("Log Available Covers")]
    public void LogAvailableCovers()
    {
        Debug.Log(GetAvailableInfo());
    }

    [ContextMenu("Refresh Genre Data")]
    public void ForceRefreshGenreData()
    {
        RefreshGenreData();
    }
}
