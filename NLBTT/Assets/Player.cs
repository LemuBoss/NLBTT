using UnityEngine;

/// <summary>
/// Handles player state and position on the game board
/// </summary>
public class Player : MonoBehaviour
{
    private Vector2Int currentPosition;
    private BoardManager boardManager;
    
    [Header("Player Model")]
    [SerializeField] private GameObject playerChipPrefab;
    [SerializeField] private Vector3 chipOffset = new Vector3(0, 0.1f, 0); // Offset above the card to prevent clipping
    
    private GameObject playerChipInstance; // The instantiated chip
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Resources")] 
    [SerializeField] private int totalHunger = 20;
    [SerializeField] private int hungerCap = 20;

    [SerializeField] private int totalStamina = 5;
    [SerializeField] private int staminaCap = 5;
    [SerializeField] private int totalHealth = 5;
    [SerializeField] private int totalBloodpoints = 0;
    
    [Header("Altar Requirements")] 
    [SerializeField] private int AltarRequirements = 0;
    private int bloodpointsStoredInAltar = 0;

    private int staminaPenaltyApplied = 0;
    private bool starvationApplied = false;

    private int bloodpointCardsVisited = 0;
    
    private BloodPointEventCard lastBloodPointCardVisited;

    private void Awake()
    {
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        
        if (boardManager == null)
        {
            Debug.LogError("Player: BoardManager not found in scene!");
        }
        
        if (playerChipPrefab == null)
        {
            Debug.LogWarning("Player: No player chip prefab assigned! Player will be invisible.");
        }
        else
        {
            // Instantiate the player chip
            playerChipInstance = Instantiate(playerChipPrefab);
            playerChipInstance.name = "PlayerChip";
            Debug.Log($"[Player] Player chip instantiated: {playerChipInstance.name}");
        }
    }

    private void Start()
    {
        // Wait one frame for BoardManager to initialize
        Invoke(nameof(InitializePosition), 0.1f);
    }
    
    private void Update()
    {
        // Check for resource exchange inputs (only works if using new Input System)
        HandleResourceExchangeInput();
    
        // Check for Altar interaction
        HandleAltarInteraction();
    }


    private void InitializePosition()
    {
        // Initialize player at the starting position (center bottom)
        if (boardManager != null)
        {
            currentPosition = boardManager.GetPlayerPosition();
            LogDebug($"Player initialized at position ({currentPosition.x}, {currentPosition.y})");
            
            // Update visual position
            UpdatePlayerChipPosition();
        }
    }

    /// <summary>
    /// Attempts to move the player to a new position
    /// Returns true if movement was successful, false otherwise
    /// </summary>
    public bool TryMoveTo(Vector2Int newPosition)
    {
        if (boardManager == null)
        {
            Debug.LogError("Player: Cannot move - BoardManager reference is null");
            return false;
        }

        LogDebug($"Attempting to move from ({currentPosition.x}, {currentPosition.y}) to ({newPosition.x}, {newPosition.y})");

        // Check if the new position is valid (within grid bounds)
        Card targetCard = boardManager.GetCardAt(newPosition.x, newPosition.y);
        if (targetCard == null)
        {
            LogDebug($"Movement failed: Position ({newPosition.x}, {newPosition.y}) is out of bounds");
            return false;
        }

        // Check if the new position is adjacent to current position
        if (!boardManager.IsCardAdjacent(currentPosition, newPosition))
        {
            LogDebug($"Movement failed: Position ({newPosition.x}, {newPosition.y}) is not adjacent to current position ({currentPosition.x}, {currentPosition.y})");
            return false;
        }

        // Check if the card can be moved onto
        Debug.Log($"[Player] Checking walkability: Card type = {targetCard.GetType().Name}, CanMoveOnto = {targetCard.CanMoveOnto}, TurnedAround = {targetCard.TurnedAround}");
        if (!targetCard.CanMoveOnto)
        {
            LogDebug($"Movement failed: Card at ({newPosition.x}, {newPosition.y}) [{targetCard.GetType().Name}] cannot be moved onto");
            
            // Even though we can't move, we still trigger the card's OnPlayerEnter for effects
            targetCard.OnPlayerEnter();
            return false;
        }

        // Movement is valid - update position
        Vector2Int oldPosition = currentPosition;
        currentPosition = newPosition;
        
        LogDebug($"Player moved from ({oldPosition.x}, {oldPosition.y}) to ({currentPosition.x}, {currentPosition.y})");
        
        // Update BoardManager with new player position
        boardManager.SetPlayerPosition(currentPosition);
        
        // Update visual position of player chip
        UpdatePlayerChipPosition();
        
        // Trigger the card's OnPlayerEnter logic
        targetCard.OnPlayerEnter();
        
        // Apply hunger cost for successful movement
        modifyHunger(-1);
        LogDebug($"Hunger reduced by 1. Current hunger: {totalHunger}");
        
        // Check for starvation after movement
        if (isStarving())
        {
            LogDebug("Player is starving!");
            applyStarvation();
        }
        
        // Check for stamina depletion after movement
        if (isStaminaEmpty())
        {
            LogDebug("Player has run out of stamina!");
            applyStaminaPenalty();
        }

        if (isSatiated())
        {
            LogDebug("Player is fully satiated!");
            applySatiationBonus();
        }

        if (isDead())
        {
            OnPlayerDeath();
        }
        
        return true;
    }

    /// <summary>
    /// Gets the player's current grid position
    /// </summary>
    public Vector2Int GetPosition()
    {
        return currentPosition;
    }

    /// <summary>
    /// Sets the player's position directly (use carefully, bypasses movement checks)
    /// Useful for teleportation or initialization
    /// </summary>
    public void SetPosition(Vector2Int newPosition)
    {
        currentPosition = newPosition;
        LogDebug($"Player position set to ({currentPosition.x}, {currentPosition.y})");
        
        if (boardManager != null)
        {
            boardManager.SetPlayerPosition(currentPosition);
        }
        
        // Update visual position
        UpdatePlayerChipPosition();
    }
    
    /// <summary>
    /// Updates the player chip model's world position based on current grid position
    /// Teleports the chip to the card's world position with an offset
    /// </summary>
    private void UpdatePlayerChipPosition()
    {
        Debug.Log($"[Player] UpdatePlayerChipPosition called. PlayerChipInstance null? {playerChipInstance == null}");
        
        if (playerChipInstance == null)
        {
            Debug.LogWarning("Player: Cannot update chip position - playerChipInstance is null! Assign a prefab in the Inspector.");
            return; // No model to update
        }
        
        if (boardManager == null)
        {
            Debug.LogError("Player: Cannot update chip position - BoardManager is null");
            return;
        }
        
        Debug.Log($"[Player] Current position: ({currentPosition.x}, {currentPosition.y})");
        
        // Get the card visual at the current player position
        CardVisual cardVisual = boardManager.GetCardVisualAt(currentPosition.x, currentPosition.y);
        
        if (cardVisual == null)
        {
            Debug.LogError($"Player: Cannot find card visual at position ({currentPosition.x}, {currentPosition.y})");
            return;
        }
        
        // Get the world position of the card
        Vector3 cardWorldPosition = cardVisual.transform.position;
        
        Debug.Log($"[Player] Card world position: {cardWorldPosition}");
        Debug.Log($"[Player] Chip offset: {chipOffset}");
        
        // Apply offset and set player chip position
        Vector3 newChipPosition = cardWorldPosition + chipOffset;
        playerChipInstance.transform.position = newChipPosition;
        
        // Make sure the chip is active
        if (!playerChipInstance.activeSelf)
        {
            Debug.LogWarning("Player chip model was inactive - activating it now");
            playerChipInstance.SetActive(true);
        }
        
        Debug.Log($"[Player] Player chip moved to world position {newChipPosition}, chip active: {playerChipInstance.activeSelf}");
        LogDebug($"Player chip moved to world position {newChipPosition}");
    }
    
    
    // Resource Management //

    public void modifyHunger(int amount)
    {
        totalHunger = Mathf.Clamp(totalHunger + amount, 0, hungerCap);
        LogDebug($"Hunger modified by {amount}. Current hunger: {totalHunger}/{hungerCap}");
    }
    
    public void modifyHealth(int amount)
    {
        totalHealth = Mathf.Max(0, totalHealth + amount);
        LogDebug($"Health modified by {amount}. Current health: {totalHealth}");
        
        // Check for death
        if (totalHealth <= 0)
        {
            LogDebug("Player has died!");
            OnPlayerDeath();
        }
    }
    
    public void modifyStamina(int amount)
    {
        totalStamina = Mathf.Clamp(totalStamina + amount, 0, staminaCap);
        LogDebug($"Stamina modified by {amount}. Current stamina: {totalStamina}/{staminaCap}");
    }
    
    public void modifyBloodpoints(int amount)
    {
        totalBloodpoints = Mathf.Max(0, totalBloodpoints + amount);
        LogDebug($"Bloodpoints modified by {amount}. Current bloodpoints: {totalBloodpoints}");
    }
    
    public void modifyBloodpointCardVisited(int amount)
    {
        bloodpointCardsVisited += amount;
        LogDebug($"{amount} bloodpoint card visited. Bloodpoint cards visited: {bloodpointCardsVisited}");
    }

    public bool isStaminaEmpty()
    {
        return totalStamina <= 0;
    }

    public bool isStarving()
    {
        return totalHunger <= 0;
    }

    public bool isSatiated()
    {
        if (totalHunger == hungerCap)
        {
            starvationApplied = false;
            return true;
        }
        return false;
    }

    public void applyStaminaPenalty()
    {
        if (staminaPenaltyApplied < totalHealth)
        {
            modifyHealth(-1);
            staminaPenaltyApplied += 1;
            LogDebug("Stamina penalty applied: Lost 1 health");
        }
        else
        {
            LogDebug("Stamina penalty already applied: Player can't die from losing all stamina.");
        }
    }

    public void applyStarvation()
    {
        if (!starvationApplied)
        {
            staminaCap = Mathf.Max(0, staminaCap - 3);
            starvationApplied = true;
            LogDebug($"Starvation applied: Stamina cap reduced by 3 to {staminaCap}");
            
            // If current stamina exceeds new cap, reduce it
            if (totalStamina > staminaCap)
            {
                totalStamina = staminaCap;
            }
        }
    }

    public void applySatiationBonus()
    {
        staminaCap = 5;
        LogDebug($"Satiation bonus applied: Stamina cap increased by 2 to {staminaCap}");
    }

    public bool isDead()
    {
        if (totalHealth <= 0)
        {
            LogDebug("Player is dead.");
            return true;
        }
        else return false;
    }
    
    private void exchangeFoodForBloodpoints()
    {
        if (totalHunger > 0)
        {
            totalHunger -= 1;
            totalBloodpoints += 1;
            LogDebug($"Exchanged 1 Food for 1 Bloodpoint");
        }
        else
        {
            LogDebug($"Tried to exchange 1 Food for 1 Bloodpoint, but Player already starving.");
        }
    }
    
    private void exchangeHealthForBloodpoints()
    {
        if (totalHealth > 0)
        {
            totalHealth -= 1;
            totalBloodpoints += 5;
            LogDebug($"Exchanged 1 Health for 5 Bloodpoints");
        }
        else
        {
            LogDebug($"Tried to exchange 1 Health for 5 Bloodpoints, but Player is dead.");
        }
    }

    private void transferBloodpointsIntoAltar()
    {
        if (totalBloodpoints > 0)
        {
            int amountToTransfer = totalBloodpoints;
            bloodpointsStoredInAltar += amountToTransfer;
            totalBloodpoints -= amountToTransfer;
            LogDebug($"Transferred {amountToTransfer} bloodpoints into the Altar");
        }
        else
        {
            LogDebug($"Tried transferring bloodpoints into the Altar, but Player has no bloodpoints.");
        }
    }
    
    
    /// <summary>
    /// Handles keyboard input for exchanging resources into bloodpoints
    /// Press 1 to exchange Food for Bloodpoints
    /// Press 2 to exchange Health for Bloodpoints
    /// </summary>
    private void HandleResourceExchangeInput()
    {
        // Using new Input System (if you have it installed)
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                ExchangeFoodForBloodpoints();
            }
        
            if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                ExchangeHealthForBloodpoints();
            }
        }
        // Fallback to old Input system if new system not available
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ExchangeFoodForBloodpoints();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ExchangeHealthForBloodpoints();
        }
    }
    
    /// <summary>
    /// Handles the Altar interaction
    /// Press ENTER when standing on an Altar card to deposit all bloodpoints
    /// </summary>
    private void HandleAltarInteraction()
    {
        if (boardManager == null)
            return;
    
        // Get the card the player is currently standing on
        Card currentCard = boardManager.GetCardAt(currentPosition.x, currentPosition.y);
    
        if (currentCard == null || !(currentCard is AltarCard))
            return; // Player is not on an Altar
    
        // Using new Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
            {
                transferBloodpointsIntoAltar();
            }
        }
        // Fallback to old Input system
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            transferBloodpointsIntoAltar();
        }
    }

    /// <summary>
    /// Public wrapper for the private exchange methods (so they can be called from UI buttons later)
    /// </summary>
    public void ExchangeFoodForBloodpoints()
    {
        exchangeFoodForBloodpoints();
    }

    public void ExchangeHealthForBloodpoints()
    {
        exchangeHealthForBloodpoints();
    }

    public void DepositBloodpointsToAltar()
    {
        transferBloodpointsIntoAltar();
    }
    
    /// <summary>
    /// Sets the last bloodpoint card visited. Called by bloodpoint cards when triggered.
    /// </summary>
    public void SetLastBloodPointCardVisited(BloodPointEventCard card)
    {
        lastBloodPointCardVisited = card;
        LogDebug($"Last bloodpoint card visited set to: {card.GetType().Name}");
    }
    
    /// <summary>
    /// Gets the last bloodpoint card visited, or null if none have been visited yet
    /// </summary>
    public BloodPointEventCard GetLastBloodPointCardVisited()
    {
        return lastBloodPointCardVisited;
    }

    /// <summary>
    /// Called when the player's health reaches 0
    /// Override or extend this method to handle game over logic
    /// </summary>
    private void OnPlayerDeath()
    {
        Debug.LogWarning("GAME OVER: Player has died!");

        // Find and trigger the GameOverUIManager
        GameOverUIManager gameOverUI = Object.FindFirstObjectByType<GameOverUIManager>();
        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver("Du bist gestorben! Deine Gesundheit hat 0 erreicht.");
        }
        else
        {
            Debug.LogError("GameOverUIManager not found in scene!");
        }
    }

    

    /// <summary>
    /// Gets the altar requirement for winning the game
    /// </summary>
    public int GetAltarRequirement() => AltarRequirements;

    /// <summary>
    /// Sets the altar requirement (useful for configuring difficulty)
    /// </summary>
    public void SetAltarRequirement(int requirement)
    {
        AltarRequirements = requirement;
        LogDebug($"Altar requirement set to: {AltarRequirements}");
    }

    /// <summary>
    /// Resets all player resources to their starting values
    /// Called when restarting the game
    /// </summary>
    public void ResetToStartingValues()
    {
        // Reset resources to starting values
        totalHunger = 20;
        hungerCap = 20;
        totalStamina = 5;
        staminaCap = 5;
        totalHealth = 5;
        totalBloodpoints = 0;
        bloodpointsStoredInAltar = 0;
        bloodpointCardsVisited = 0;

        // Reset penalty flags
        staminaPenaltyApplied = 0;
        starvationApplied = false;

        // Clear last bloodpoint card visited
        lastBloodPointCardVisited = null;

        LogDebug("Player resources reset to starting values");
    }


   

    /// <summary>
    /// Helper method for debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Player] {message}");
        }
    }
    
    // Public getters for UI or other systems
    public int GetHunger() => totalHunger;
    public int GetHungerCap() => hungerCap;
    public int GetStamina() => totalStamina;
    public int GetStaminaCap() => staminaCap;
    public int GetHealth() => totalHealth;
    public int GetBloodpoints() => totalBloodpoints;
    public int GetBloodpointCardsVisited() => bloodpointCardsVisited;
    public int GetBloodpointsInAltar() => bloodpointsStoredInAltar;
    
    
}