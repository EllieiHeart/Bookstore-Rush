using UnityEngine;

[System.Serializable]
public class BookType
{
    public string genre;        // "Romance", "Fantasy", "Mystery", etc.
    public string cover;        // "Flowers", "Dragon", "Skull", etc.
    public Color bookColor;     // Visual color of the book
    public string shelfSource;  // Which shelf this book came from
    
    public BookType(string genre, string cover, Color color, string shelfSource = "")
    {
        this.genre = genre;
        this.cover = cover;
        this.bookColor = color;
        this.shelfSource = shelfSource;
    }

    public override string ToString()
    {
        return $"{genre} book with {cover} cover";
    }

    public string GetDetailedDescription()
    {
        string source = !string.IsNullOrEmpty(shelfSource) ? $" (from {shelfSource})" : "";
        return $"{genre} book with {cover} cover{source}";
    }
}

public class Book : MonoBehaviour
{
    [Header("Book Information")]
    public BookType bookType;

    [Header("Visual Components")]
    private SpriteRenderer sr;
    private Transform stampIcon;

    [Header("State")]
    public bool isCheckedOut { get; private set; }

    // Quick access properties for backward compatibility
    public string genre => bookType?.genre ?? "";
    public string cover => bookType?.cover ?? "";
    public string shelfSource => bookType?.shelfSource ?? "";

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        CreateStampIcon();
    }

    void CreateStampIcon()
    {
        // Create tiny visual "stamp" so it looks processed
        stampIcon = new GameObject("Stamp").transform;
        stampIcon.SetParent(transform);
        stampIcon.localPosition = new Vector3(0.15f, 0.25f, 0);

        var stampRenderer = stampIcon.gameObject.AddComponent<SpriteRenderer>();
        stampRenderer.sprite = sr.sprite; // reuse square sprite
        stampRenderer.color = new Color(0f, 0f, 0f, 0f); // invisible until stamped
        stampRenderer.sortingOrder = sr.sortingOrder + 1;
        stampIcon.localScale = new Vector3(0.25f, 0.25f, 1);
    }

    public void Setup(string genre, string cover, Color color, string shelfSource = "")
    {
        bookType = new BookType(genre, cover, color, shelfSource);

        if (sr != null)
            sr.color = color;

        // Update the GameObject name for easier debugging
        gameObject.name = $"Book_{genre}_{cover}";
    }

    public void Setup(BookType newBookType)
    {
        bookType = newBookType;

        if (sr != null)
            sr.color = bookType.bookColor;

        gameObject.name = $"Book_{bookType.genre}_{bookType.cover}";
    }

    public void StampAtCheckout()
    {
        isCheckedOut = true;
        var stampRenderer = stampIcon.GetComponent<SpriteRenderer>();
        stampRenderer.color = new Color(0f, 0f, 0f, 0.45f); // faint black "stamp"

        Debug.Log($"Stamped {bookType.GetDetailedDescription()}");
    }

    public bool MatchesOrder(Order order)
    {
        return isCheckedOut &&
               genre == order.genre &&
               cover == order.cover;
    }

    public string GetStatusDescription()
    {
        string stampStatus = isCheckedOut ? "✓ Stamped" : "○ Unstamped";
        return $"{bookType.GetDetailedDescription()} - {stampStatus}";
    }
    public void ChangeCover(string newCover, Color newColor)
    {
        // Update the book type with new cover and color
        bookType.cover = newCover;
        bookType.bookColor = newColor;

        // Update visual appearance
        if (sr != null)
            sr.color = newColor;

        // Update GameObject name for debugging
        gameObject.name = $"Book_{bookType.genre}_{bookType.cover}";
    }

    public void ChangeGenreAndCover(string newGenre, string newCover, Color newColor)
    {
        // Update the book type with new genre, cover and color
        bookType.genre = newGenre;
        bookType.cover = newCover;
        bookType.bookColor = newColor;

        // Update visual appearance
        if (sr != null)
            sr.color = newColor;

        // Update GameObject name for debugging
        gameObject.name = $"Book_{bookType.genre}_{bookType.cover}";

        Debug.Log($"Book transformed to: {GetStatusDescription()}");
    }


}
