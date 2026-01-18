using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all wolves on the board
/// Handles wolf spawning, movement coordination, and AI pathfinding
/// </summary>
public class WolfAI : MonoBehaviour
{
    [Header("Wolf Setup")]
    [SerializeField] private GameObject wolfPrefab;
    
    [Header("Movement Settings")]
    [SerializeField] [Range(0f, 0.3f)] private float backtrackProbability = 0.05f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private List<Wolf> wolves = new List<Wolf>();
    private BoardManager boardManager;
    
    // Direction vectors for movement (up, down, left, right)
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(-1, 0), // Left
        new Vector2Int(1, 0)   // Right
    };

    private void Awake()
    {
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] BoardManager not found in scene!");
        }
    }

    /// <summary>
    /// Spawns wolves at all WolfdenCards on the board
    /// Should be called after board generation
    /// </summary>
    public void SpawnWolves()
    {
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] Cannot spawn wolves - BoardManager is null");
            return;
        }

        // Clear existing wolves
        ClearWolves();

        // Get grid dimensions from BoardManager
        int gridWidth = boardManager.GetGridWidth();
        int gridHeight = boardManager.GetGridHeight();
        
        LogDebug($"Searching for WolfdenCards in {gridWidth}x{gridHeight} grid...");
        
        // Find all WolfdenCards and spawn wolves
        int wolfdenCount = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Card card = boardManager.GetCardAt(x, y);
                
                if (card != null && card is WolfdenCard)
                {
                    wolfdenCount++;
                    LogDebug($"Found WolfdenCard at ({x}, {y})");
                    SpawnWolfAt(new Vector2Int(x, y), card as WolfdenCard);
                }
            }
        }

        LogDebug($"Found {wolfdenCount} WolfdenCards, spawned {wolves.Count} wolves on the board");
        
        // Update wolf visibility after spawning
        UpdateAllWolfVisibility();
    }

    /// <summary>
    /// Spawns a single wolf at a wolfden position
    /// </summary>
    private void SpawnWolfAt(Vector2Int position, WolfdenCard den)
    {
        if (wolfPrefab == null)
        {
            Debug.LogError("[WolfAI] Cannot spawn wolf - wolfPrefab is not assigned!");
            return;
        }

        // Create wolf GameObject
        GameObject wolfObj = new GameObject($"Wolf_{position.x}_{position.y}");
        wolfObj.transform.parent = transform;
        
        Wolf wolf = wolfObj.AddComponent<Wolf>();
        wolf.Initialize(position, wolfPrefab);
        
        // Add to wolves list
        wolves.Add(wolf);
        
        // Link wolf to den
        den.AssignWolf(wolf);
        
        LogDebug($"Wolf spawned at ({position.x}, {position.y})");
    }

    /// <summary>
    /// Moves all wolves after the player has made a move
    /// Wolves either follow scent trails or move randomly
    /// Movement follows hierarchy order to prevent conflicts
    /// Wolves on cooldown skip their turn
    /// </summary>
    public void MoveAllWolves()
    {
        if (wolves.Count == 0)
        {
            LogDebug("No wolves to move");
            return;
        }

        LogDebug($"Moving {wolves.Count} wolves...");

        // Track which positions have been claimed this turn
        HashSet<Vector2Int> claimedPositions = new HashSet<Vector2Int>();

        // Move each wolf in order (hierarchy)
        for (int i = 0; i < wolves.Count; i++)
        {
            Wolf wolf = wolves[i];
            Vector2Int currentPos = wolf.GetPosition();
            
            LogDebug($"--- Processing Wolf {i} at ({currentPos.x}, {currentPos.y}) ---");

            // Check if wolf is on cooldown
            if (wolf.IsOnCooldown())
            {
                LogDebug($"Wolf {i} is on cooldown - skipping turn");
                wolf.DeactivateCooldown(); // Clear cooldown for next turn
                continue;
            }

            // Check if wolf should track scent
            bool shouldTrack = wolf.ShouldTrackScent();
            
            Vector2Int? chosenPosition = null;

            if (shouldTrack)
            {
                // Try to follow scent trail
                LogDebug($"Wolf {i} is tracking scent");
                chosenPosition = wolf.GetScentTrackingTarget(claimedPositions);
                
                if (chosenPosition.HasValue)
                {
                    LogDebug($"Wolf {i} following scent to ({chosenPosition.Value.x}, {chosenPosition.Value.y})");
                }
                else
                {
                    LogDebug($"Wolf {i} scent tracking failed (trail ended or blocked). Falling back to random movement.");
                    // Fall through to random movement
                }
            }

            // If not tracking or tracking failed, use random movement
            if (!chosenPosition.HasValue)
            {
                LogDebug($"Wolf {i} using random movement");
                List<Vector2Int> eligiblePositions = GetEligibleMovementPositions(wolf, claimedPositions);

                if (eligiblePositions.Count == 0)
                {
                    LogDebug($"Wolf {i} has no valid moves - skipping turn");
                    continue;
                }

                // Choose random position with anti-backtracking
                chosenPosition = ChooseRandomMovementPosition(wolf, eligiblePositions);
                LogDebug($"Wolf {i} chose random position ({chosenPosition.Value.x}, {chosenPosition.Value.y})");
            }

            // Claim this position and move the wolf
            claimedPositions.Add(chosenPosition.Value);
            wolf.SetPosition(chosenPosition.Value);
            
            LogDebug($"Wolf {i} moved to ({chosenPosition.Value.x}, {chosenPosition.Value.y})");

            // Check if wolf caught the player
            if (wolf.IsAtPlayerPosition())
            {
                wolf.OnCatchPlayer();
            }
        }
    }

    /// <summary>
    /// Gets all eligible positions a wolf can move to (for random movement)
    /// Excludes: out of bounds, null cards, unwalkable cards, already claimed positions
    /// Includes: player's current position
    /// </summary>
    private List<Vector2Int> GetEligibleMovementPositions(Wolf wolf, HashSet<Vector2Int> claimedPositions)
    {
        List<Vector2Int> eligible = new List<Vector2Int>();
        Vector2Int currentPos = wolf.GetPosition();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int targetPos = currentPos + dir;

            // Check if already claimed by another wolf this turn
            if (claimedPositions.Contains(targetPos))
                continue;

            // Check if position is valid and has a card
            Card targetCard = boardManager.GetCardAt(targetPos.x, targetPos.y);
            if (targetCard == null)
                continue; // Out of bounds or empty cell

            // Check if card is walkable
            if (!targetCard.CanMoveOnto)
                continue;

            eligible.Add(targetPos);
        }

        return eligible;
    }

    /// <summary>
    /// Chooses a random movement position from eligible options
    /// Applies anti-backtracking logic (previous direction has lower probability)
    /// </summary>
    private Vector2Int ChooseRandomMovementPosition(Wolf wolf, List<Vector2Int> eligiblePositions)
    {
        if (eligiblePositions.Count == 1)
            return eligiblePositions[0];

        Vector2Int currentPos = wolf.GetPosition();
        Vector2Int lastDirection = wolf.GetLastDirection();
        Vector2Int backtrackPosition = currentPos - lastDirection; // Position we came from

        // Build weighted list
        List<Vector2Int> weightedOptions = new List<Vector2Int>();

        foreach (Vector2Int pos in eligiblePositions)
        {
            // Check if this is the backtrack position
            bool isBacktrack = (pos == backtrackPosition && lastDirection != Vector2Int.zero);

            if (isBacktrack)
            {
                // Add with reduced probability
                weightedOptions.Add(pos);
            }
            else
            {
                // Add multiple times to increase weight
                for (int i = 0; i < 6; i++)
                {
                    weightedOptions.Add(pos);
                }
            }
        }

        // Choose randomly from weighted list
        int randomIndex = Random.Range(0, weightedOptions.Count);
        return weightedOptions[randomIndex];
    }

    /// <summary>
    /// Updates visibility for all wolves based on whether their card is revealed
    /// Wolves on unrevealed cards are hidden, wolves on revealed cards are shown
    /// OVERRIDE: Flashlight item makes all wolves visible regardless of card state
    /// </summary>
    public void UpdateAllWolfVisibility()
    {
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] Cannot update wolf visibility - BoardManager is null");
            return;
        }

        // Check if player has flashlight
        Player player = Object.FindFirstObjectByType<Player>();
        bool flashlightActive = false;
    
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                flashlightActive = itemManager.ShouldWolvesBeAlwaysVisible();
            }
        }

        if (flashlightActive)
        {
            // Flashlight makes ALL wolves visible
            foreach (Wolf wolf in wolves)
            {
                if (wolf == null) continue;
                wolf.SetVisible(true);
            }
            LogDebug("Flashlight active: All wolves are visible");
        }
        else
        {
            // Normal visibility rules
            foreach (Wolf wolf in wolves)
            {
                if (wolf == null) continue;

                Vector2Int wolfPos = wolf.GetPosition();
                Card card = boardManager.GetCardAt(wolfPos.x, wolfPos.y);

                if (card == null)
                {
                    // No card at position (shouldn't happen) - hide wolf
                    wolf.SetVisible(false);
                    continue;
                }

                // Wolf is visible only if the card is revealed (not turned around)
                bool isCardRevealed = !card.TurnedAround;
                wolf.SetVisible(isCardRevealed);

                LogDebug($"Wolf at ({wolfPos.x}, {wolfPos.y}): Card revealed = {isCardRevealed}, Wolf visible = {isCardRevealed}");
            }
        }
    }

    /// <summary>
    /// Gets the scent value at a specific position from BoardManager
    /// </summary>
    public float GetScentAt(Vector2Int position)
    {
        if (boardManager != null)
        {
            return boardManager.GetScentAt(position);
        }
        return 0f;
    }

    /// <summary>
    /// Gets the entire scent grid from BoardManager
    /// </summary>
    public float[,] GetScentGrid()
    {
        if (boardManager != null)
        {
            return boardManager.GetScentGrid();
        }
        return null;
    }

    /// <summary>
    /// Clears all wolves (used when regenerating board)
    /// </summary>
    public void ClearWolves()
    {
        foreach (Wolf wolf in wolves)
        {
            if (wolf != null)
            {
                Destroy(wolf.gameObject);
            }
        }
        wolves.Clear();
        LogDebug("All wolves cleared");
    }

    /// <summary>
    /// Gets all wolves currently active
    /// </summary>
    public List<Wolf> GetWolves()
    {
        return wolves;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WolfAI] {message}");
        }
    }
}
