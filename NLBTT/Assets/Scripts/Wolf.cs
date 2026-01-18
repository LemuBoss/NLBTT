using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a wolf entity on the board
/// Similar to Player, has a position and visual figurine
/// NEW: Supports despawning and respawning after being defeated
/// </summary>
public class Wolf : MonoBehaviour
{
    private Vector2Int currentPosition;
    private Vector2Int lastDirection;
    private GameObject wolfModelInstance;
    private BoardManager boardManager;
    private bool isVisible = true;
    
    // Scent tracking state
    private bool isTrackingScent = false;
    private Dictionary<Vector2Int, float> scentMemory = new Dictionary<Vector2Int, float>();
    
    // Encounter cooldown
    private bool isOnCooldown = false;
    
    // NEW: Despawn/Respawn system
    private bool isDespawned = false;
    private int turnsUntilRespawn = 0;
    private Vector2Int spawnPosition; // Original spawn position (den location)

    [Header("Wolf Model")]
    [SerializeField] private Vector3 chipOffset = new Vector3(0, 0.02f, 0);

    [Header("Scent Tracking")]
    [SerializeField] private float minScentThreshold = 0.15f;
    
    [Header("Respawn Settings")]
    [SerializeField] private int respawnTurns = 10; // Turns until respawn after defeat

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
        spawnPosition = startPosition; // Remember original spawn position
        lastDirection = Vector2Int.zero;

        // Instantiate the wolf model
        if (modelPrefab != null)
        {
            wolfModelInstance = Instantiate(modelPrefab);
            wolfModelInstance.name = $"Wolf_{startPosition.x}_{startPosition.y}";
            UpdateVisualPosition();
            LogDebug($"Wolf initialized at position ({currentPosition.x}, {currentPosition.y})");

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
    /// Gets the wolf's original spawn position (den location)
    /// </summary>
    public Vector2Int GetSpawnPosition()
    {
        return spawnPosition;
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
        // Don't track if on cooldown or despawned
        if (isOnCooldown || isDespawned)
        {
            LogDebug("Wolf is on cooldown or despawned, skipping scent check");
            return false;
        }
        
        if (boardManager == null) return false;

        float currentScent = boardManager.GetScentAt(currentPosition);
        
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

        if (scentMemory.ContainsKey(currentPosition))
        {
            float rememberedScent = scentMemory[currentPosition];
            
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
        float bestScent = currentScent;

        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentPos = currentPosition + offset;

            if (claimedPositions.Contains(adjacentPos))
            {
                LogDebug($"Position ({adjacentPos.x}, {adjacentPos.y}) is claimed by another wolf. Stopping scent tracking.");
                isTrackingScent = false;
                return null;
            }

            Card targetCard = boardManager.GetCardAt(adjacentPos.x, adjacentPos.y);
            if (targetCard == null || !targetCard.CanMoveOnto)
                continue;

            float adjacentScent = boardManager.GetScentAt(adjacentPos);

            if (adjacentScent > bestScent)
            {
                bestScent = adjacentScent;
                bestPosition = adjacentPos;
            }
        }

        if (bestPosition.HasValue)
        {
            LogDebug($"Found stronger scent at ({bestPosition.Value.x}, {bestPosition.Value.y}) with value {bestScent:F2}");
            scentMemory[bestPosition.Value] = bestScent;
        }
        else
        {
            LogDebug($"No stronger scent found. Trail ends here.");
            isTrackingScent = false;
        }

        return bestPosition;
    }

    public bool IsTrackingScent()
    {
        return isTrackingScent;
    }

    public void ClearScentMemory()
    {
        scentMemory.Clear();
        isTrackingScent = false;
        LogDebug("Scent memory cleared");
    }
    
    public void ActivateCooldown()
    {
        isOnCooldown = true;
        isTrackingScent = false;
        LogDebug("Cooldown activated - wolf will wait one turn");
    }
    
    public void DeactivateCooldown()
    {
        isOnCooldown = false;
        LogDebug("Cooldown deactivated - wolf can move again");
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    // NEW: Despawn/Respawn methods
    
    /// <summary>
    /// Despawns the wolf after being defeated in combat
    /// Wolf will respawn at its den after the specified number of turns
    /// </summary>
    public void Despawn()
    {
        if (isDespawned)
        {
            LogDebug("Wolf is already despawned!");
            return;
        }
        
        isDespawned = true;
        turnsUntilRespawn = respawnTurns;
        isTrackingScent = false;
        isOnCooldown = false;
        
        // Notify current card that wolf is leaving
        NotifyCardOfDeparture(currentPosition);
        
        // Hide the wolf model
        if (wolfModelInstance != null)
        {
            wolfModelInstance.SetActive(false);
        }
        
        LogDebug($"🪦 Wolf despawned! Will respawn in {turnsUntilRespawn} turns at den ({spawnPosition.x}, {spawnPosition.y})");
    }
    
    /// <summary>
    /// Decrements the respawn timer by one turn
    /// Should be called each turn by WolfAI
    /// Returns true if wolf should respawn this turn
    /// </summary>
    public bool DecrementRespawnTimer()
    {
        if (!isDespawned)
            return false;
        
        turnsUntilRespawn--;
        LogDebug($"Respawn timer: {turnsUntilRespawn} turns remaining");
        
        if (turnsUntilRespawn <= 0)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Respawns the wolf at its original den position
    /// </summary>
    public void Respawn()
    {
        if (!isDespawned)
        {
            LogDebug("Wolf is not despawned, cannot respawn!");
            return;
        }
        
        isDespawned = false;
        turnsUntilRespawn = 0;
        
        // Reset wolf state
        ClearScentMemory();
        lastDirection = Vector2Int.zero;
        
        // Move wolf back to spawn position
        currentPosition = spawnPosition;
        
        // Show the wolf model
        if (wolfModelInstance != null)
        {
            wolfModelInstance.SetActive(true);
            UpdateVisualPosition();
        }
        
        // Notify den card that wolf is back
        NotifyCardOfPresence(spawnPosition);
        
        LogDebug($"🐺 Wolf respawned at den ({spawnPosition.x}, {spawnPosition.y})!");
    }
    
    /// <summary>
    /// Checks if the wolf is currently despawned
    /// </summary>
    public bool IsDespawned()
    {
        return isDespawned;
    }
    
    /// <summary>
    /// Gets the number of turns until respawn (0 if not despawned)
    /// </summary>
    public int GetTurnsUntilRespawn()
    {
        return isDespawned ? turnsUntilRespawn : 0;
    }

    public void SetVisible(bool visible)
    {
        // Don't show despawned wolves
        if (isDespawned)
        {
            isVisible = false;
            if (wolfModelInstance != null)
            {
                wolfModelInstance.SetActive(false);
            }
            return;
        }
        
        Player player = Object.FindFirstObjectByType<Player>();
        bool forceVisible = false;
    
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null && itemManager.ShouldWolvesBeAlwaysVisible())
            {
                forceVisible = true;
            }
        }
    
        isVisible = forceVisible || visible;
    
        if (wolfModelInstance != null)
        {
            wolfModelInstance.SetActive(isVisible);
        
            if (forceVisible && !visible)
            {
                LogDebug($"Wolf at ({currentPosition.x}, {currentPosition.y}) forced visible by Flashlight (card not revealed)");
            }
            else
            {
                LogDebug($"Wolf at ({currentPosition.x}, {currentPosition.y}) visibility set to: {isVisible}");
            }
        }
    }

    public bool IsVisible()
    {
        return isVisible;
    }

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

    public Vector2Int GetLastDirection()
    {
        return lastDirection;
    }

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

        CardVisual cardVisual = boardManager.GetCardVisualAt(currentPosition.x, currentPosition.y);

        if (cardVisual == null)
        {
            Debug.LogError($"[Wolf] Cannot find card visual at position ({currentPosition.x}, {currentPosition.y})");
            return;
        }

        Vector3 cardWorldPosition = cardVisual.transform.position;
        Vector3 newPosition = cardWorldPosition + chipOffset;
        wolfModelInstance.transform.position = newPosition;

        wolfModelInstance.SetActive(isVisible && !isDespawned);

        LogDebug($"Wolf visual updated to world position {newPosition}, visible: {isVisible}, despawned: {isDespawned}");
    }

    public bool IsAtPlayerPosition()
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            return currentPosition == player.GetPosition();
        }
        return false;
    }

    public void OnCatchPlayer()
    {
        // Don't trigger if despawned
        if (isDespawned)
            return;
            
        Debug.Log($"[Wolf] Wolf at ({currentPosition.x}, {currentPosition.y}) caught the player!");
        ActivateCooldown();
    }

    private void OnDestroy()
    {
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

