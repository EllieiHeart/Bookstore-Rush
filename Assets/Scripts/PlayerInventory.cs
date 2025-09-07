using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory State")]
    public Book heldBook;
    
    [Header("Visual Settings")]
    public float bobSpeed = 6f;
    public float bobHeight = 0.05f;
    public Vector3 holdOffset = new Vector3(0, 1f, 0);
    
    private float bobTime;

    public bool IsHoldingBook 
    { 
        get 
        { 
            if (heldBook == null) return false;
            if (heldBook.gameObject == null) return false;
            if (heldBook.transform.parent != transform) return false;
            return true;
        } 
    }

    public string GetInventoryStatus()
    {
        if (!IsHoldingBook) return "Hands: Empty";
        return $"Carrying: {heldBook.GetStatusDescription()}";
    }

    public string GetDetailedBookInfo()
    {
        if (!IsHoldingBook) return "No book in inventory";
        
        var book = heldBook;
        return $"Book Details:\n" +
               $"Genre: {book.genre}\n" +
               $"Cover: {book.cover}\n" +
               $"Source: {book.shelfSource}\n" +
               $"Status: {(book.isCheckedOut ? "Stamped" : "Unstamped")}";
    }

    void Update()
    {
        ValidateInventoryState();
        UpdateBookPosition();
    }

    void ValidateInventoryState()
    {
        if (heldBook != null && (heldBook.gameObject == null || heldBook.transform.parent != transform))
        {
            Debug.Log("Cleaning up invalid book reference");
            heldBook = null;
        }
    }

    void UpdateBookPosition()
    {
        if (IsHoldingBook)
        {
            bobTime += Time.deltaTime * bobSpeed;
            Vector3 bobOffset = new Vector3(0, Mathf.Sin(bobTime) * bobHeight, 0);
            heldBook.transform.localPosition = holdOffset + bobOffset;
        }
    }

    public bool CanPickUpBook()
    {
        return !IsHoldingBook;
    }

    public void PickUpBook(Book book)
    {
        if (!CanPickUpBook()) 
        {
            Debug.Log("Cannot pick up book - hands are full!");
            return;
        }

        if (book == null)
        {
            Debug.Log("Cannot pick up null book!");
            return;
        }

        heldBook = book;
        book.transform.SetParent(transform);
        book.transform.localPosition = holdOffset;

        var spriteRenderer = book.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) 
            spriteRenderer.sortingOrder = 10;

        var rigidBody = book.GetComponent<Rigidbody2D>();
        if (rigidBody) Destroy(rigidBody);
        
        var collider = book.GetComponent<Collider2D>();
        if (collider) collider.enabled = false;

        Debug.Log($"Picked up: {book.GetStatusDescription()}");
    }

    public Book DropBook()
    {
        if (!IsHoldingBook) 
        {
            Debug.Log("No book to drop!");
            return null;
        }

        var book = heldBook;
        heldBook = null;
        
        book.transform.SetParent(null);
        
        var collider = book.GetComponent<Collider2D>();
        if (collider) collider.enabled = true;

        Debug.Log($"Dropped: {book.GetStatusDescription()}");
        return book;
    }

    public void ClearInventory()
    {
        if (heldBook != null)
        {
            if (heldBook.gameObject != null)
                Destroy(heldBook.gameObject);
            heldBook = null;
        }
    }
}
