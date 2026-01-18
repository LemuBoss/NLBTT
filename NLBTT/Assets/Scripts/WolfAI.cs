using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all wolves on the board
/// Handles wolf spawning, movement coordination, and AI pathfinding
/// NEW: Central authority for wolf despawning/respawning
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
    
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    private void Awake()
    {
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] BoardManager not found in scene!");
        }
    }

    public void SpawnWolves()
    {
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] Cannot spawn wolves - BoardManager is null");
            return;
        }

        ClearWolves();

        int gridWidth = boardManager.GetGridWidth();
        int gridHeight = boardManager.GetGridHeight();
        
        LogDebug($"Searching for WolfdenCards in {gridWidth}x{gridHeight} grid...");
        
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
        
        UpdateAllWolfVisibility();
    }

    private void SpawnWolfAt(Vector2Int position, WolfdenCard den)
    {
        if (wolfPrefab == null)
        {
            Debug.LogError("[WolfAI] Cannot spawn wolf - wolfPrefab is not assigned!");
            return;
        }

        GameObject wolfObj = new GameObject($"Wolf_{position.x}_{position.y}");
        wolfObj.transform.parent = transform;
        
        Wolf wolf = wolfObj.AddComponent<Wolf>();
        wolf.Initialize(position, wolfPrefab);
        
        wolves.Add(wolf);
        
        den.AssignWolf(wolf);
        
        LogDebug($"Wolf spawned at ({position.x}, {position.y})");
    }

    /// <summary>
    /// NEW METHOD: Despawns the wolf at the specified position
    /// Called by WolfCard when player defeats a wolf
    /// Returns true if a wolf was found and despawned
    /// </summary>
    public bool DespawnWolfAtPosition(Vector2Int position)
    {
        LogDebug($"🔍 Searching for wolf at position ({position.x}, {position.y})...");
        
        // Search through all wolves
        foreach (Wolf wolf in wolves)
        {
            if (wolf == null || wolf.IsDespawned())
                continue;
            
            Vector2Int wolfPos = wolf.GetPosition();
            
            LogDebug($"  - Checking wolf at ({wolfPos.x}, {wolfPos.y})");
            
            // Found the wolf at player's position
            if (wolfPos == position)
            {
                LogDebug($"✓ Found wolf at position ({position.x}, {position.y}) - despawning!");
                wolf.Despawn();
                return true;
            }
        }
        
        LogDebug($"✗ No active wolf found at position ({position.x}, {position.y})");
        return false;
    }

    /// <summary>
    /// Moves all wolves after the player has made a move
    /// Also handles despawned wolves' respawn timers
    /// </summary>
    public void MoveAllWolves()
    {
        if (wolves.Count == 0)
        {
            LogDebug("No wolves to move");
            return;
        }

        LogDebug($"Processing {wolves.Count} wolves...");

        // First, handle respawn timers for despawned wolves
        List<Wolf> wolvesToRespawn = new List<Wolf>();
        foreach (Wolf wolf in wolves)
        {
            if (wolf.IsDespawned())
            {
                if (wolf.DecrementRespawnTimer())
                {
                    wolvesToRespawn.Add(wolf);
                }
            }
        }
        
        // Respawn wolves that are ready
        foreach (Wolf wolf in wolvesToRespawn)
        {
            wolf.Respawn();
            LogDebug($"Wolf respawned at ({wolf.GetPosition().x}, {wolf.GetPosition().y})");
        }

        // Track which positions have been claimed this turn
        HashSet<Vector2Int> claimedPositions = new HashSet<Vector2Int>();

        // Move each active (non-despawned) wolf
        for (int i = 0; i < wolves.Count; i++)
        {
            Wolf wolf = wolves[i];
            
            // Skip despawned wolves
            if (wolf.IsDespawned())
            {
                LogDebug($"Wolf {i} is despawned - skipping movement");
                continue;
            }
            
            Vector2Int currentPos = wolf.GetPosition();
            
            LogDebug($"--- Processing Wolf {i} at ({currentPos.x}, {currentPos.y}) ---");

            // Check if wolf is on cooldown
            if (wolf.IsOnCooldown())
            {
                LogDebug($"Wolf {i} is on cooldown - skipping turn");
                wolf.DeactivateCooldown();
                continue;
            }

            bool shouldTrack = wolf.ShouldTrackScent();
            
            Vector2Int? chosenPosition = null;

            if (shouldTrack)
            {
                LogDebug($"Wolf {i} is tracking scent");
                chosenPosition = wolf.GetScentTrackingTarget(claimedPositions);
                
                if (chosenPosition.HasValue)
                {
                    LogDebug($"Wolf {i} following scent to ({chosenPosition.Value.x}, {chosenPosition.Value.y})");
                }
                else
                {
                    LogDebug($"Wolf {i} scent tracking failed (trail ended or blocked). Falling back to random movement.");
                }
            }

            if (!chosenPosition.HasValue)
            {
                LogDebug($"Wolf {i} using random movement");
                List<Vector2Int> eligiblePositions = GetEligibleMovementPositions(wolf, claimedPositions);

                if (eligiblePositions.Count == 0)
                {
                    LogDebug($"Wolf {i} has no valid moves - skipping turn");
                    continue;
                }

                chosenPosition = ChooseRandomMovementPosition(wolf, eligiblePositions);
                LogDebug($"Wolf {i} chose random position ({chosenPosition.Value.x}, {chosenPosition.Value.y})");
            }

            claimedPositions.Add(chosenPosition.Value);
            wolf.SetPosition(chosenPosition.Value);
            
            LogDebug($"Wolf {i} moved to ({chosenPosition.Value.x}, {chosenPosition.Value.y})");

            // Check if wolf caught player - but don't trigger event here
            // Event triggering is handled elsewhere (e.g. by a collision detection system)
            if (wolf.IsAtPlayerPosition())
            {
                wolf.OnCatchPlayer();
                LogDebug($"Wolf {i} is at player position - cooldown activated");
            }
        }
    }

    private List<Vector2Int> GetEligibleMovementPositions(Wolf wolf, HashSet<Vector2Int> claimedPositions)
    {
        List<Vector2Int> eligible = new List<Vector2Int>();
        Vector2Int currentPos = wolf.GetPosition();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int targetPos = currentPos + dir;

            if (claimedPositions.Contains(targetPos))
                continue;

            Card targetCard = boardManager.GetCardAt(targetPos.x, targetPos.y);
            if (targetCard == null)
                continue;

            if (!targetCard.CanMoveOnto)
                continue;

            eligible.Add(targetPos);
        }

        return eligible;
    }

    private Vector2Int ChooseRandomMovementPosition(Wolf wolf, List<Vector2Int> eligiblePositions)
    {
        if (eligiblePositions.Count == 1)
            return eligiblePositions[0];

        Vector2Int currentPos = wolf.GetPosition();
        Vector2Int lastDirection = wolf.GetLastDirection();
        Vector2Int backtrackPosition = currentPos - lastDirection;

        List<Vector2Int> weightedOptions = new List<Vector2Int>();

        foreach (Vector2Int pos in eligiblePositions)
        {
            bool isBacktrack = (pos == backtrackPosition && lastDirection != Vector2Int.zero);

            if (isBacktrack)
            {
                weightedOptions.Add(pos);
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    weightedOptions.Add(pos);
                }
            }
        }

        int randomIndex = Random.Range(0, weightedOptions.Count);
        return weightedOptions[randomIndex];
    }

    public void UpdateAllWolfVisibility()
    {
        if (boardManager == null)
        {
            Debug.LogError("[WolfAI] Cannot update wolf visibility - BoardManager is null");
            return;
        }

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
            foreach (Wolf wolf in wolves)
            {
                if (wolf == null) continue;
                // Despawned wolves stay invisible even with flashlight
                if (!wolf.IsDespawned())
                {
                    wolf.SetVisible(true);
                }
            }
            LogDebug("Flashlight active: All active wolves are visible");
        }
        else
        {
            foreach (Wolf wolf in wolves)
            {
                if (wolf == null) continue;
                
                // Despawned wolves are never visible
                if (wolf.IsDespawned())
                {
                    wolf.SetVisible(false);
                    continue;
                }

                Vector2Int wolfPos = wolf.GetPosition();
                Card card = boardManager.GetCardAt(wolfPos.x, wolfPos.y);

                if (card == null)
                {
                    wolf.SetVisible(false);
                    continue;
                }

                bool isCardRevealed = !card.TurnedAround;
                wolf.SetVisible(isCardRevealed);

                LogDebug($"Wolf at ({wolfPos.x}, {wolfPos.y}): Card revealed = {isCardRevealed}, Wolf visible = {isCardRevealed}");
            }
        }
    }

    public float GetScentAt(Vector2Int position)
    {
        if (boardManager != null)
        {
            return boardManager.GetScentAt(position);
        }
        return 0f;
    }

    public float[,] GetScentGrid()
    {
        if (boardManager != null)
        {
            return boardManager.GetScentGrid();
        }
        return null;
    }

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

    public List<Wolf> GetWolves()
    {
        return wolves;
    }
    
    /// <summary>
    /// Gets all currently active (non-despawned) wolves
    /// </summary>
    public List<Wolf> GetActiveWolves()
    {
        List<Wolf> activeWolves = new List<Wolf>();
        foreach (Wolf wolf in wolves)
        {
            if (wolf != null && !wolf.IsDespawned())
            {
                activeWolves.Add(wolf);
            }
        }
        return activeWolves;
    }
    
    /// <summary>
    /// Gets all currently despawned wolves
    /// </summary>
    public List<Wolf> GetDespawnedWolves()
    {
        List<Wolf> despawnedWolves = new List<Wolf>();
        foreach (Wolf wolf in wolves)
        {
            if (wolf != null && wolf.IsDespawned())
            {
                despawnedWolves.Add(wolf);
            }
        }
        return despawnedWolves;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WolfAI] {message}");
        }
    }
}

