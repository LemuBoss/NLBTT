using UnityEngine;

/// <summary>
/// Represents a wolf entity on the board
/// Similar to Player, has a position and visual figurine
/// </summary>
public class Wolf : MonoBehaviour
{
    private Vector2Int currentPosition;
    private Vector2Int lastDirection; // Direction the wolf came from (to avoid backtracking)
    private GameObject wolfModelInstance;
    private BoardManager boardManager;
    
    [Header("Wolf Model")]
    [SerializeField] private Vector3 chipOffset = new Vector3(0, 0.02f, 0); // Slightly higher than player to distinguish
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        
        if (boardManager == null)
        {
            Debug.LogError("[Wolf] BoardManager not found in scene!");
        }
    }

    /// <summary>
    /// Initializes the wolf at a specific position with a visual model
    /// </summary>
    public void Initialize(Vector2Int startPosition, GameObject modelPrefab)
    {
        currentPosition = startPosition;
        lastDirection = Vector2Int.zero; // No previous direction yet
        
        // Instantiate the wolf model
        if (modelPrefab != null)
        {
            wolfModelInstance = Instantiate(modelPrefab);
            wolfModelInstance.name = $"Wolf_{startPosition.x}_{startPosition.y}";
            UpdateVisualPosition();
            LogDebug($"Wolf initialized at position ({currentPosition.x}, {currentPosition.y})");
        }
        else
        {
            Debug.LogWarning("[Wolf] No model prefab provided - wolf will be invisible!");
        }
    }

    /// <summary>
    /// Gets the wolf's current grid position
    /// </summary>
    public Vector2Int GetPosition()
    {
        return currentPosition;
    }

    /// <summary>
    /// Sets the wolf's position (used by WolfAI during movement)
    /// </summary>
    public void SetPosition(Vector2Int newPosition)
    {
        // Calculate direction moved
        Vector2Int direction = newPosition - currentPosition;
        lastDirection = direction;
        
        currentPosition = newPosition;
        UpdateVisualPosition();
        
        LogDebug($"Wolf moved to ({currentPosition.x}, {currentPosition.y}), direction: ({direction.x}, {direction.y})");
    }

    /// <summary>
    /// Gets the last direction the wolf moved (for anti-backtracking)
    /// </summary>
    public Vector2Int GetLastDirection()
    {
        return lastDirection;
    }

    /// <summary>
    /// Updates the wolf model's visual position based on current grid position
    /// </summary>
    private void UpdateVisualPosition()
    {
        if (wolfModelInstance == null)
        {
            LogDebug("Cannot update visual position - wolf model is null");
            return;
        }

        if (boardManager == null)
        {
            Debug.LogError("[Wolf] Cannot update position - BoardManager is null");
            return;
        }

        // Get the card visual at the current wolf position
        CardVisual cardVisual = boardManager.GetCardVisualAt(currentPosition.x, currentPosition.y);

        if (cardVisual == null)
        {
            Debug.LogError($"[Wolf] Cannot find card visual at position ({currentPosition.x}, {currentPosition.y})");
            return;
        }

        // Get the world position of the card and apply offset
        Vector3 cardWorldPosition = cardVisual.transform.position;
        Vector3 newPosition = cardWorldPosition + chipOffset;
        wolfModelInstance.transform.position = newPosition;

        // Ensure the model is active
        if (!wolfModelInstance.activeSelf)
        {
            wolfModelInstance.SetActive(true);
        }

        LogDebug($"Wolf visual updated to world position {newPosition}");
    }

    /// <summary>
    /// Checks if the wolf is at the same position as the player
    /// </summary>
    public bool IsAtPlayerPosition()
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            return currentPosition == player.GetPosition();
        }
        return false;
    }

    /// <summary>
    /// Called when wolf catches the player
    /// </summary>
    public void OnCatchPlayer()
    {
        Debug.Log($"[Wolf] Wolf at ({currentPosition.x}, {currentPosition.y}) caught the player!");
        // Placeholder for future game over logic
    }

    /// <summary>
    /// Cleanup when wolf is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (wolfModelInstance != null)
        {
            Destroy(wolfModelInstance);
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Wolf] {message}");
        }
    }
}
