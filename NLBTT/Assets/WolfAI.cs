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
    [SerializeField] [Range(0f, 0.3f)] private float backtrackProbability = 0.05f; // 5% chance to go back
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private List<Wolf> wolves = new List<Wolf>();
    private BoardManager boardManager;
    private bool[,] scentGrid; // false = card exists, null equivalent = no card, true = player scent
    
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

        // Initialize scent grid
        InitializeScentGrid();

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
    /// Initializes the scent grid based on the board layout
    /// false = card exists, true = player scent (none initially)
    /// Null cells in cardGrid remain as default (false) but are treated specially
    /// </summary>
    private void InitializeScentGrid()
    {
        if (boardManager == null) return;

        // Get grid dimensions from BoardManager
        int gridWidth = boardManager.GetGridWidth();
        int gridHeight = boardManager.GetGridHeight();

        scentGrid = new bool[gridWidth, gridHeight];

        // Initialize: false = no scent (default for all cells)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                scentGrid[x, y] = false; // Default: no scent
            }
        }

        LogDebug($"Scent grid initialized: {gridWidth}x{gridHeight}");
    }

    /// <summary>
    /// Moves all wolves after the player has made a move
    /// Wolves move in hierarchy order to prevent conflicts
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

        // Move each wolf in order
        for (int i = 0; i < wolves.Count; i++)
        {
            Wolf wolf = wolves[i];
            Vector2Int currentPos = wolf.GetPosition();
            
            // Get eligible movement options
            List<Vector2Int> eligiblePositions = GetEligibleMovementPositions(wolf, claimedPositions);

            if (eligiblePositions.Count == 0)
            {
                LogDebug($"Wolf {i} at ({currentPos.x}, {currentPos.y}) has no valid moves - skipping turn");
                continue;
            }

            // Choose a random position with anti-backtracking
            Vector2Int chosenPosition = ChooseMovementPosition(wolf, eligiblePositions);
            
            // Claim this position
            claimedPositions.Add(chosenPosition);
            
            // Move the wolf
            wolf.SetPosition(chosenPosition);
            
            LogDebug($"Wolf {i} moved from ({currentPos.x}, {currentPos.y}) to ({chosenPosition.x}, {chosenPosition.y})");

            // Check if wolf caught the player
            if (wolf.IsAtPlayerPosition())
            {
                wolf.OnCatchPlayer();
            }
        }
    }

    /// <summary>
    /// Gets all eligible positions a wolf can move to
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

            // Check if card is walkable (RockCard and TraderCard are not walkable)
            // Player's current position IS walkable
            if (!targetCard.CanMoveOnto)
                continue;

            eligible.Add(targetPos);
        }

        return eligible;
    }

    /// <summary>
    /// Chooses a movement position from eligible options
    /// Applies anti-backtracking logic (previous direction has lower probability)
    /// </summary>
    private Vector2Int ChooseMovementPosition(Wolf wolf, List<Vector2Int> eligiblePositions)
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
                // Add with reduced probability (5% chance means ~5% of total weight)
                // We'll add it once, and add all others multiple times
                weightedOptions.Add(pos);
            }
            else
            {
                // Add multiple times to increase weight
                // If backtrack weight is ~5%, non-backtrack should be ~31.67% each (for 3 options)
                // Ratio: 1 (backtrack) : 6.33 (each other) ≈ 5% : 31.67%
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
    /// Gets the scent grid (for player to modify)
    /// </summary>
    public bool[,] GetScentGrid()
    {
        return scentGrid;
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