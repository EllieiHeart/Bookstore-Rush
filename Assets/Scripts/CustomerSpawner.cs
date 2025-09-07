using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject customerPrefab;
    
    [Header("Available Shelves")]
    [Tooltip("Add all shelves that customers can order from")]
    public ShelfStation[] availableShelves;
    
    [Header("Alternative: Manual References")]
    [Tooltip("If you prefer to specify exactly which shelves to use")]
    public ShelfStation fantasyShelf;
    public ShelfStation romanceShelf;
    
    [Header("Spawning Weights")]
    [Tooltip("Adjust the chance of each genre being selected")]
    [Range(0f, 1f)] public float fantasyWeight = 0.5f;
    [Range(0f, 1f)] public float romanceWeight = 0.5f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool logDetailedSelection = false;

    void Start()
    {
        Debug.Log("=== CustomerSpawner Start Debug ===");
        Debug.Log($"Customer Prefab: {(customerPrefab != null ? customerPrefab.name : "NULL")}");
        Debug.Log($"Available Shelves Count: {(availableShelves != null ? availableShelves.Length : 0)}");
        Debug.Log($"Fantasy Shelf: {(fantasyShelf != null ? fantasyShelf.gameObject.name : "NULL")}");
        Debug.Log($"Romance Shelf: {(romanceShelf != null ? romanceShelf.gameObject.name : "NULL")}");
        
        // Auto-populate available shelves if not manually set
        if ((availableShelves == null || availableShelves.Length == 0) && 
            fantasyShelf != null && romanceShelf != null)
        {
            availableShelves = new ShelfStation[] { fantasyShelf, romanceShelf };
            if (showDebugInfo)
                Debug.Log("CustomerSpawner: Auto-populated shelves from manual references");
        }

        // Validate shelf configurations
        ValidateShelfConfigurations();
    }

    void ValidateShelfConfigurations()
    {
        Debug.Log("=== Validating Shelf Configurations ===");
        
        if (availableShelves == null || availableShelves.Length == 0)
        {
            Debug.LogError("No shelves assigned! Please assign shelves in the CustomerSpawner inspector.");
            return;
        }

        for (int i = 0; i < availableShelves.Length; i++)
        {
            var shelf = availableShelves[i];
            if (shelf == null)
            {
                Debug.LogError($"Shelf {i} is null!");
                continue;
            }

            Debug.Log($"Shelf {i}: {shelf.gameObject.name}");
            
            if (shelf.shelfData == null)
            {
                Debug.LogError($"Shelf {shelf.gameObject.name} has no shelfData!");
                continue;
            }

            Debug.Log($"  - Genre: {shelf.shelfData.genre}");
            Debug.Log($"  - Covers: [{string.Join(", ", shelf.shelfData.availableCovers ?? new string[0])}]");
            Debug.Log($"  - Book Prefab: {(shelf.bookPrefab != null ? shelf.bookPrefab.name : "NULL - THIS IS THE PROBLEM!")}");
        }
    }

    public Customer SpawnCustomer()
    {
        Debug.Log("=== Attempting to Spawn Customer ===");
        
        if (availableShelves == null || availableShelves.Length == 0)
        {
            Debug.LogError("CustomerSpawner: No shelves configured! Please assign shelves in the inspector.");
            return null;
        }

        // Step 1: Randomly select a genre (shelf)
        ShelfStation selectedShelf = SelectRandomShelf();
        
        if (selectedShelf == null)
        {
            Debug.LogError("CustomerSpawner: SelectRandomShelf returned null!");
            return null;
        }
        
        if (selectedShelf.shelfData == null)
        {
            Debug.LogError($"CustomerSpawner: {selectedShelf.gameObject.name} has null shelfData!");
            return null;
        }

        Debug.Log($"Selected shelf: {selectedShelf.gameObject.name} ({selectedShelf.shelfData.genre})");

        // Step 2: Randomly select a cover from that genre's available covers
        string selectedCover = SelectRandomCoverFromShelf(selectedShelf);
        
        if (string.IsNullOrEmpty(selectedCover))
        {
            Debug.LogError($"CustomerSpawner: No valid covers found in {selectedShelf.gameObject.name}!");
            return null;
        }

        Debug.Log($"Selected cover: {selectedCover}");

        // Create the order
        string genre = selectedShelf.shelfData.genre;
        Order newOrder = new Order(genre, selectedCover);

        Debug.Log($"Created order: {newOrder}");

        // Check if we have a customer prefab
        if (customerPrefab == null)
        {
            Debug.LogError("CustomerSpawner: No customer prefab assigned!");
            return null;
        }

        // Spawn the customer at spawner position (GameManager will handle positioning)
        GameObject customerObject = Instantiate(customerPrefab, transform.position, Quaternion.identity);
        var customer = customerObject.GetComponent<Customer>();
        
        if (customer == null)
        {
            Debug.LogError("Customer prefab doesn't have a Customer component!");
            Destroy(customerObject);
            return null;
        }

        customer.Setup(newOrder);

        Debug.Log($"✅ Customer spawned successfully: {newOrder}");
        return customer;
    }

    ShelfStation SelectRandomShelf()
    {
        if (logDetailedSelection)
            Debug.Log("Step 1: Selecting random genre...");

        // Filter out invalid shelves
        System.Collections.Generic.List<ShelfStation> validShelves = new System.Collections.Generic.List<ShelfStation>();
        foreach (var shelf in availableShelves)
        {
            if (shelf != null && shelf.shelfData != null && 
                !string.IsNullOrEmpty(shelf.shelfData.genre) && 
                shelf.shelfData.genre != "General" &&
                shelf.shelfData.availableCovers != null && 
                shelf.shelfData.availableCovers.Length > 0)
            {
                validShelves.Add(shelf);
                Debug.Log($"Valid shelf found: {shelf.gameObject.name} - {shelf.shelfData.genre}");
            }
            else
            {
                Debug.LogWarning($"Invalid shelf: {(shelf != null ? shelf.gameObject.name : "NULL")}");
            }
        }

        if (validShelves.Count == 0)
        {
            Debug.LogError("No valid shelves found for customer orders!");
            return null;
        }

        Debug.Log($"Found {validShelves.Count} valid shelves");

        // Use weighted selection if we have exactly Fantasy and Romance
        if (validShelves.Count == 2)
        {
            ShelfStation fantasyOption = null;
            ShelfStation romanceOption = null;
            
            foreach (var shelf in validShelves)
            {
                if (shelf.shelfData.genre.ToLower().Contains("fantasy"))
                    fantasyOption = shelf;
                else if (shelf.shelfData.genre.ToLower().Contains("romance"))
                    romanceOption = shelf;
            }

            if (fantasyOption != null && romanceOption != null)
            {
                float totalWeight = fantasyWeight + romanceWeight;
                float randomValue = Random.Range(0f, totalWeight);
                
                ShelfStation selected = randomValue < fantasyWeight ? fantasyOption : romanceOption;
                
                Debug.Log($"Weighted selection: {selected.shelfData.genre} (Random: {randomValue}, Fantasy weight: {fantasyWeight})");
                
                return selected;
            }
        }

        // Fallback to simple random selection
        int randomIndex = Random.Range(0, validShelves.Count);
        ShelfStation randomShelf = validShelves[randomIndex];
        
        Debug.Log($"Random selection: {randomShelf.shelfData.genre} (Index: {randomIndex}/{validShelves.Count})");
        
        return randomShelf;
    }

    string SelectRandomCoverFromShelf(ShelfStation shelf)
    {
        if (logDetailedSelection)
            Debug.Log($"Step 2: Selecting random cover from {shelf.shelfData.genre} shelf...");

        string[] availableCovers = shelf.shelfData.availableCovers;
        
        if (availableCovers == null)
        {
            Debug.LogError($"Shelf {shelf.gameObject.name} has null availableCovers!");
            return null;
        }

        Debug.Log($"Available covers: [{string.Join(", ", availableCovers)}]");

        // Filter out empty/null covers
        System.Collections.Generic.List<string> validCovers = new System.Collections.Generic.List<string>();
        foreach (string cover in availableCovers)
        {
            if (!string.IsNullOrEmpty(cover))
                validCovers.Add(cover);
        }

        if (validCovers.Count == 0)
        {
            Debug.LogError($"No valid covers found in {shelf.gameObject.name}!");
            return null;
        }

        int randomIndex = Random.Range(0, validCovers.Count);
        string selectedCover = validCovers[randomIndex];
        
        Debug.Log($"Selected cover: {selectedCover} (Index: {randomIndex}/{validCovers.Count})");
        
        return selectedCover;
    }

    // Utility methods
    [ContextMenu("Test Customer Spawn")]
    public void TestSpawn()
    {
        if (Application.isPlaying)
        {
            bool originalDetailed = logDetailedSelection;
            logDetailedSelection = true;
            
            Debug.Log("=== Manual Test Customer Spawn ===");
            SpawnCustomer();
            
            logDetailedSelection = originalDetailed;
        }
        else
        {
            Debug.Log("Can only test spawn during play mode");
        }
    }

    [ContextMenu("Validate Configuration")]
    public void ValidateConfiguration()
    {
        ValidateShelfConfigurations();
    }

    [ContextMenu("Log Available Options")]
    public void LogAvailableOptions()
    {
        Debug.Log("=== Available Customer Order Options ===");
        
        if (availableShelves == null || availableShelves.Length == 0)
        {
            Debug.LogError("No shelves assigned!");
            return;
        }

        foreach (var shelf in availableShelves)
        {
            if (shelf != null && shelf.shelfData != null)
            {
                Debug.Log($"{shelf.shelfData.genre} Shelf ({shelf.gameObject.name}): {string.Join(", ", shelf.shelfData.availableCovers)}");
            }
        }
    }
}
