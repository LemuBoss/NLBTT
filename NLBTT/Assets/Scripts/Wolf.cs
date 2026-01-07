using UnityEngine;
using System.Collections.Generic;

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
    private bool isVisible = true; // Whether the wolf model should be shown
    
    // Scent tracking state
    private bool isTrackingScent = false;
    private Dictionary<Vector2Int, float> scentMemory = new Dictionary<Vector2Int, float>(); // Stores scent values of visited positions
    
    // Encounter cooldown
    private bool isOnCooldown = false; // Wolf waits one turn after encounter

    [Header("Wolf Model")]
    [SerializeField] private Vector3 chipOffset = new Vector3(0, 0.02f, 0); // Slightly higher than player to distinguish

    [Header("Scent Tracking")]
    [SerializeField] private float minScentThreshold = 0.15f; // Minimum scent to start tracking

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

            // Notify the card that a wolf is on it
            NotifyCardOfPresence(startPosition);
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
        // Notify old card that wolf is leaving
        NotifyCardOfDeparture(currentPosition);

        // Calculate direction moved
        Vector2Int direction = newPosition - currentPosition;
        lastDirection = direction;

        currentPosition = newPosition;
        UpdateVisualPosition();

        // Notify new card that wolf is arriving
        NotifyCardOfPresence(currentPosition);

        LogDebug($"Wolf moved to ({currentPosition.x}, {currentPosition.y}), direction: ({direction.x}, {direction.y})");
    }


    /// <summary>
    /// Checks if the wolf should track scent at current position
    /// Returns true if tracking should begin/continue
    /// </summary>
    public bool ShouldTrackScent()
    {
        // Don't track if on cooldown
        if (isOnCooldown)
        {
            LogDebug("Wolf is on cooldown, skipping scent check");
            return false;
        }
        
        if (boardManager == null) return false;

        float currentScent = boardManager.GetScentAt(currentPosition);
        
        // Check if scent is strong enough to track
        if (currentScent < minScentThreshold)
        {
            if (isTrackingScent)
            {
                LogDebug($"🚫 GAVE UP CHASE: Scent too weak at ({currentPosition.x}, {currentPosition.y}). Current scent: {currentScent:F3}, Threshold: {minScentThreshold:F3}. Resuming random behavior.");
                isTrackingScent = false;
            }
            else
            {
                LogDebug($"Scent too weak to start tracking at ({currentPosition.x}, {currentPosition.y}). Current: {currentScent:F3}, Need: {minScentThreshold:F3}");
            }
            return false;
        }

        // Check if we've been here before
        if (scentMemory.ContainsKey(currentPosition))
        {
            float rememberedScent = scentMemory[currentPosition];
            
            // If scent has decreased or stayed the same, it's old scent - ignore it
            if (currentScent <= rememberedScent)
            {
                LogDebug($"Scent at ({currentPosition.x}, {currentPosition.y}) is old (current: {currentScent:F2}, remembered: {rememberedScent:F2}). Ignoring.");
                isTrackingScent = false;
                return false;
            }
            else
            {
                LogDebug($"Scent at ({currentPosition.x}, {currentPosition.y}) is fresh (current: {currentScent:F2}, remembered: {rememberedScent:F2}). Tracking!");
            }
        }

        // Update memory with current scent
        scentMemory[currentPosition] = currentScent;
        isTrackingScent = true;
        
        return true;
    }

    /// <summary>
    /// Attempts to find the best adjacent position to track scent
    /// Returns the position with highest scent value, or null if no valid tracking position exists
    /// </summary>
    public Vector2Int? GetScentTrackingTarget(HashSet<Vector2Int> claimedPositions)
    {
        if (boardManager == null) return null;

        float currentScent = boardManager.GetScentAt(currentPosition);
        Vector2Int? bestPosition = null;
        float bestScent = currentScent; // Must be higher than current position

        // Check all four adjacent directions
        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentPos = currentPosition + offset;

            // Check if position is claimed by another wolf this turn
            if (claimedPositions.Contains(adjacentPos))
            {
                LogDebug($"Position ({adjacentPos.x}, {adjacentPos.y}) is claimed by another wolf. Stopping scent tracking.");
                isTrackingScent = false;
                return null; // Stop tracking if desired position is blocked
            }

            // Check if position is valid and has a card
            Card targetCard = boardManager.GetCardAt(adjacentPos.x, adjacentPos.y);
            if (targetCard == null || !targetCard.CanMoveOnto)
                continue;

            // Get scent at this position
            float adjacentScent = boardManager.GetScentAt(adjacentPos);

            // Check if this scent is stronger than current position and best so far
            if (adjacentScent > bestScent)
            {
                bestScent = adjacentScent;
                bestPosition = adjacentPos;
            }
        }

        if (bestPosition.HasValue)
        {
            LogDebug($"Found stronger scent at ({bestPosition.Value.x}, {bestPosition.Value.y}) with value {bestScent:F2}");
            // Update memory for the new position
            scentMemory[bestPosition.Value] = bestScent;
        }
        else
        {
            LogDebug($"No stronger scent found. Trail ends here.");
            isTrackingScent = false;
        }

        return bestPosition;
    }

    /// <summary>
    /// Gets whether the wolf is currently tracking scent
    /// </summary>
    public bool IsTrackingScent()
    {
        return isTrackingScent;
    }

    /// <summary>
    /// Clears the wolf's scent memory (useful for board regeneration)
    /// </summary>
    public void ClearScentMemory()
    {
        scentMemory.Clear();
        isTrackingScent = false;
        LogDebug("Scent memory cleared");
    }
    
    /// <summary>
    /// Activates cooldown - wolf will skip its next turn
    /// Called after encountering the player
    /// </summary>
    public void ActivateCooldown()
    {
        isOnCooldown = true;
        isTrackingScent = false; // Stop tracking during cooldown
        LogDebug("Cooldown activated - wolf will wait one turn");
    }
    
    /// <summary>
    /// Deactivates cooldown - called by WolfAI after the cooldown turn
    /// </summary>
    public void DeactivateCooldown()
    {
        isOnCooldown = false;
        LogDebug("Cooldown deactivated - wolf can move again");
    }
    
    /// <summary>
    /// Checks if wolf is on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    /// <summary>
    /// Sets whether the wolf model should be visible
    /// Called by WolfAI based on whether the card is revealed
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        
        if (wolfModelInstance != null)
        {
            wolfModelInstance.SetActive(visible);
            LogDebug($"Wolf at ({currentPosition.x}, {currentPosition.y}) visibility set to: {visible}");
        }
    }

    /// <summary>
    /// Gets whether the wolf is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }

    /// <summary>
    /// Notifies the card at the given position that this wolf is now on it
    /// </summary>
    private void NotifyCardOfPresence(Vector2Int position)
    {
        if (boardManager == null) return;

        Card card = boardManager.GetCardAt(position.x, position.y);
        if (card != null)
        {
            card.OnWolfEnter(this);
            LogDebug($"Notified card at ({position.x}, {position.y}) of wolf presence");
        }
    }

    /// <summary>
    /// Notifies the card at the given position that this wolf is leaving
    /// </summary>
    private void NotifyCardOfDeparture(Vector2Int position)
    {
        if (boardManager == null) return;

        Card card = boardManager.GetCardAt(position.x, position.y);
        if (card != null)
        {
            card.OnWolfExit(this);
            LogDebug($"Notified card at ({position.x}, {position.y}) of wolf departure");
        }
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

        // Apply visibility setting
        wolfModelInstance.SetActive(isVisible);

        LogDebug($"Wolf visual updated to world position {newPosition}, visible: {isVisible}");
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
    /// Activates cooldown so wolf waits one turn before moving again
    /// Note: The actual encounter is triggered by the Card system
    /// </summary>
    public void OnCatchPlayer()
    {
        Debug.Log($"[Wolf] Wolf at ({currentPosition.x}, {currentPosition.y}) caught the player!");
        ActivateCooldown();
        // The actual encounter is triggered by the Card system now
    }

    /// <summary>
    /// Cleanup when wolf is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Notify current card that wolf is leaving
        NotifyCardOfDeparture(currentPosition);

        if (wolfModelInstance != null)
        {
            Destroy(wolfModelInstance);
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Wolf at ({currentPosition.x},{currentPosition.y})] {message}");
        }
    }
}
