using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptUI;
    public TextMeshProUGUI statusUI;

    [Header("Prefabs")]
    public GameObject bookPrefab;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Collider2D currentZone;
    private PlayerInventory inventory;

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        if (promptUI) promptUI.gameObject.SetActive(false);
        if (statusUI) statusUI.gameObject.SetActive(true);
    }

    void Update()
    {
        UpdateInventoryStatusUI();
        UpdateInteractionPrompts();
        HandleInput();
    }

    void UpdateInventoryStatusUI()
    {
        if (statusUI)
        {
            if (inventory.IsHoldingBook && inventory.heldBook != null)
            {
                var book = inventory.heldBook;
                string stampStatus = book.isCheckedOut ? "✓ Stamped" : "○ Unstamped";
                statusUI.text = $"Carrying: {book.genre} ({book.cover}) - {stampStatus}";
                statusUI.color = book.isCheckedOut ? Color.green : Color.yellow;
            }
            else
            {
                statusUI.text = "Hands: Empty";
                statusUI.color = Color.white;
            }
        }
    }

    void UpdateInteractionPrompts()
    {
        if (currentZone == null)
        {
            if (promptUI) promptUI.gameObject.SetActive(false);
            return;
        }

        bool atShelf = currentZone.CompareTag("Shelf") || currentZone.CompareTag("StationZone");
        bool atCheckout = currentZone.CompareTag("Checkout");
        bool atCustomer = currentZone.CompareTag("Customer");
        bool atSorting = currentZone.CompareTag("Sorting");

        string promptText = "";

        if (atShelf)
        {
            if (inventory.CanPickUpBook())
            {
                string shelfType = GetShelfType(currentZone.name);
                promptText = $"Press E to pick up {shelfType} book";
            }
            else if (inventory.IsHoldingBook)
            {
                promptText = $"Hands full! Carrying {inventory.heldBook.genre} book";
            }
        }
        else if (atSorting)
        {
            if (inventory.IsHoldingBook)
            {
                promptText = $"Press E to cycle cover | Shift+E to change genre\nCurrent: {inventory.heldBook.genre} - {inventory.heldBook.cover}";
            }
            else
            {
                promptText = "Bring a book here to sort through all covers";
            }
        }
        else if (atCheckout)
        {
            if (inventory.IsHoldingBook)
            {
                var book = inventory.heldBook;
                if (!book.isCheckedOut)
                {
                    promptText = "Press E to stamp book";
                }
                else
                {
                    promptText = "Press E to deliver to customer";
                }
            }
            else
            {
                promptText = "Bring a book here to stamp it";
            }
        }
        else if (atCustomer)
        {
            var customer = currentZone.GetComponent<Customer>();
            if (customer != null && (customer.IsDisappointed() || customer.IsBeingServed()))
            {
                promptText = "Customer is busy...";
            }
            else if (inventory.IsHoldingBook)
            {
                promptText = "Press E to deliver book";
            }
            else
            {
                promptText = "You need a stamped book to deliver";
            }
        }

        if (promptUI)
        {
            promptUI.text = promptText;
            promptUI.gameObject.SetActive(!string.IsNullOrEmpty(promptText));
        }
    }



    string GetShelfType(string shelfName)
    {
        string name = shelfName.ToLower();
        if (name.Contains("fantasy")) return "Fantasy";
        if (name.Contains("romance")) return "Romance";
        if (name.Contains("mystery")) return "Mystery";
        return "Unknown"; // Changed from "Generic" to "Unknown"
    }


    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformInteraction();
        }

        if (Input.GetKeyDown(KeyCode.X) && showDebugLogs)
        {
            inventory.ClearInventory();
            Debug.Log("Debug: Cleared inventory");
        }
    }

   void PerformInteraction()
{
    if (currentZone == null) return;

    bool atShelf = currentZone.CompareTag("Shelf") || currentZone.CompareTag("StationZone");
    bool atCheckout = currentZone.CompareTag("Checkout");
    bool atCustomer = currentZone.CompareTag("Customer");
    bool atSorting = currentZone.CompareTag("Sorting");

    if (atShelf && inventory.CanPickUpBook())
    {
        SpawnAndPickUpBook();
    }
    else if (atSorting && inventory.IsHoldingBook)
    {
        var sortingTable = currentZone.GetComponent<SortingTable>();
        if (sortingTable != null)
        {
            sortingTable.CycleCover(inventory.heldBook);
        }
    }
    else if (atCheckout && inventory.IsHoldingBook)
    {
        HandleCheckoutInteraction();
    }
    else if (atCustomer && inventory.IsHoldingBook)
    {
        HandleCustomerDelivery();
    }
    else if (showDebugLogs)
    {
        Debug.Log($"Cannot interact - At: {currentZone?.tag}, Holding: {inventory.IsHoldingBook}");
    }
}



    void SpawnAndPickUpBook()
    {
        if (bookPrefab == null)
        {
            Debug.LogError("BOOK PREFAB NOT ASSIGNED! Please assign the Book prefab to PlayerInteraction.");
            return;
        }

        // Try to get the ShelfStation component first
        var shelfStation = currentZone.GetComponent<ShelfStation>();
        if (shelfStation != null)
        {
            // Use the shelf station to create the proper book
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            var book = shelfStation.CreateBook(spawnPos);

            if (book != null)
            {
                Debug.Log($"Created book: {book.GetStatusDescription()}");
                inventory.PickUpBook(book);
            }
            return;
        }

        // Fallback for shelves without ShelfStation component
        Vector3 fallbackSpawnPos = transform.position + Vector3.up * 0.5f;
        var bookObject = Instantiate(bookPrefab, fallbackSpawnPos, Quaternion.identity);
        var fallbackBook = bookObject.GetComponent<Book>();

        if (fallbackBook != null)
        {
            string shelfName = currentZone.name.ToLower();
            if (shelfName.Contains("romance"))
            {
                fallbackBook.Setup("Romance", "Flowers", Color.magenta, "Romance Shelf");
            }
            else if (shelfName.Contains("fantasy"))
            {
                fallbackBook.Setup("Fantasy", "Dragon", Color.blue, "Fantasy Shelf");
            }
            else
            {
                // Don't allow pickup from unidentified shelves
                Debug.LogWarning($"Cannot pick up from unidentified shelf: {currentZone.name}");
                Destroy(bookObject);
                return;
            }

            Debug.Log($"Created book: {fallbackBook.GetStatusDescription()}");
            inventory.PickUpBook(fallbackBook);
        }
    }


    void HandleCheckoutInteraction()
    {
        var book = inventory.heldBook;

        if (!book.isCheckedOut)
        {
            book.StampAtCheckout();
            if (showDebugLogs) Debug.Log("Book stamped!");
        }
        else
        {
            var customer = GameManager.I?.currentCustomer;
            if (customer != null && !customer.IsDisappointed() && !customer.IsBeingServed())
            {
                bool success = customer.TryFulfill(book);
                if (success)
                {
                    // Successful delivery
                    Destroy(inventory.DropBook()?.gameObject);
                    GameManager.I.CompleteCustomerAndQueueNext();
                    if (showDebugLogs) Debug.Log("Book delivered successfully!");
                }
                else
                {
                    // Failed delivery - customer will handle their own reaction
                    // Drop the wrong book and let GameManager clean it up
                    Book wrongBook = inventory.DropBook();
                    if (GameManager.I != null)
                        GameManager.I.CleanupWrongBook(wrongBook);

                    if (showDebugLogs)
                        Debug.Log($"Wrong book delivered! Customer wants {customer.order}, but delivered {book.genre} with {book.cover}");
                }
            }
            else
            {
                Debug.Log("No available customer to deliver to!");
            }
        }
    }

    void HandleCustomerDelivery()
    {
        var customer = currentZone.GetComponent<Customer>();
        if (customer == null && GameManager.I != null)
            customer = GameManager.I.currentCustomer;

        if (customer != null && !customer.IsDisappointed() && !customer.IsBeingServed())
        {
            bool success = customer.TryFulfill(inventory.heldBook);
            if (success)
            {
                Destroy(inventory.DropBook()?.gameObject);
                if (GameManager.I != null)
                    GameManager.I.CompleteCustomerAndQueueNext();
            }
            else
            {
                // Failed delivery - drop the wrong book and clean it up
                Book wrongBook = inventory.DropBook();
                if (GameManager.I != null)
                    GameManager.I.CleanupWrongBook(wrongBook);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        currentZone = col;
        if (showDebugLogs)
        {
            Debug.Log($"Entered zone: {col.name} (Tag: {col.tag})");
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col == currentZone)
        {
            currentZone = null;
        }
    }
    

}
