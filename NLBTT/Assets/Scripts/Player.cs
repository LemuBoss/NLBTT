using UnityEngine;

/// <summary>
/// Handles player state and position on the game board
/// </summary>
public class Player : MonoBehaviour
{
    private Vector2Int currentPosition;
    private BoardManager boardManager;
    private WolfAI wolfAI;
    private ItemManager itemManager;

    public AudioSource audioSource;       
    public AudioClip moveSound;           
    
    [Header("Player Model")]
    [SerializeField] private GameObject playerChipPrefab;
    [SerializeField] private Vector3 chipOffset = new Vector3(0, 0.1f, 0);
    
    private GameObject playerChipInstance;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Resources")] 
    [SerializeField] private int totalHunger = 40;
    [SerializeField] private int hungerCap = 40;
    [SerializeField] private int baseHungerConsumption = 1;

    [SerializeField] private int totalHealth = 5;
    [SerializeField] private int totalBloodpoints = 0;
    
    // NEW: Can heal self flag (controlled by items like Old Bread)
    private bool canHealSelf = true;
    
    [Header("Altar Requirements")] 
    [SerializeField] private int AltarRequirements = 0;
    private int bloodpointsStoredInAltar = 0;

    private int bloodpointCardsVisited = 0;
    
    private BloodPointEventCard lastBloodPointCardVisited;

    private void Awake()
    {
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        wolfAI = Object.FindFirstObjectByType<WolfAI>();
        itemManager = GetComponent<ItemManager>();

        if (boardManager == null)
        {
            Debug.LogError("Player: BoardManager not found in scene!");
        }

        if (wolfAI == null)
        {
            Debug.LogWarning("Player: WolfAI not found in scene!");
        }

        if (itemManager == null)
        {
            Debug.LogError("Player: ItemManager not found! Add ItemManager component to Player.");
        }

        if (playerChipPrefab == null)
        {
            Debug.LogWarning("Player: No player chip prefab assigned! Player will be invisible.");
        }
        else
        {
            playerChipInstance = Instantiate(playerChipPrefab);
            playerChipInstance.name = "PlayerChip";
            Debug.Log($"[Player] Player chip instantiated: {playerChipInstance.name}");
        }
    }

    private void Start()
    {
        Invoke(nameof(InitializePosition), 0.1f);
    }
    
    private void Update()
    {
        HandleResourceExchangeInput();
        HandleAltarInteraction();
        HandleEventTrigger();
    }

    private void InitializePosition()
    {
        if (boardManager != null)
        {
            currentPosition = boardManager.GetPlayerPosition();
            LogDebug($"Player initialized at position ({currentPosition.x}, {currentPosition.y})");

            UpdatePlayerChipPosition();
            audioSource.PlayOneShot(moveSound);

            Card startCard = boardManager.GetCardAt(currentPosition.x, currentPosition.y);
            if (startCard != null)
            {
                startCard.OnPlayerEnter();
                LogDebug($"Notified starting card of player presence");
            }
        }
    }

    private void HandleEventTrigger()
    {
        if (boardManager == null)
            return;

        Card currentCard = boardManager.GetCardAt(currentPosition.x, currentPosition.y);

        if (currentCard == null || !currentCard.HasActiveEvent)
            return;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                LogDebug($"Triggering event on {currentCard.GetType().Name}");
                currentCard.TriggerEvent();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            LogDebug($"Triggering event on {currentCard.GetType().Name}");
            currentCard.TriggerEvent();
        }
    }

    public bool TryMoveTo(Vector2Int newPosition)
    {
        if (boardManager == null)
        {
            Debug.LogError("Player: Cannot move - BoardManager reference is null");
            return false;
        }

        LogDebug($"Attempting to move from ({currentPosition.x}, {currentPosition.y}) to ({newPosition.x}, {newPosition.y})");

        Card targetCard = boardManager.GetCardAt(newPosition.x, newPosition.y);
        if (targetCard == null)
        {
            LogDebug($"Movement failed: Position ({newPosition.x}, {newPosition.y}) is out of bounds");
            return false;
        }

        if (!boardManager.IsCardAdjacent(currentPosition, newPosition))
        {
            LogDebug($"Movement failed: Position ({newPosition.x}, {newPosition.y}) is not adjacent to current position ({currentPosition.x}, {currentPosition.y})");
            return false;
        }

        Debug.Log($"[Player] Checking walkability: Card type = {targetCard.GetType().Name}, CanMoveOnto = {targetCard.CanMoveOnto}, TurnedAround = {targetCard.TurnedAround}");
        if (!targetCard.CanMoveOnto)
        {
            // Check if it's a RockCard and player has ClimbingRope
            bool canClimbRock = (targetCard is RockCard) && 
                                (itemManager != null) && 
                                itemManager.HasItem(ItemManager.ItemType.ClimbingRope);
    
            if (!canClimbRock)
            {
                LogDebug($"Movement failed: Card at ({newPosition.x}, {newPosition.y}) [{targetCard.GetType().Name}] cannot be moved onto");
                targetCard.OnPlayerEnter();
                return false;
            }
            else
            {
                LogDebug($"Climbing onto RockCard using Climbing Rope!");
            }
        }

        Card oldCard = boardManager.GetCardAt(currentPosition.x, currentPosition.y);
        if (oldCard != null)
        {
            oldCard.OnPlayerExit();
        }

        Vector2Int oldPosition = currentPosition;
        currentPosition = newPosition;

        LogDebug($"Player moved from ({oldPosition.x}, {oldPosition.y}) to ({currentPosition.x}, {currentPosition.y})");

        boardManager.SetPlayerScent(currentPosition);
        boardManager.SetPlayerPosition(currentPosition);
        boardManager.RevealCardAndAdjacent(currentPosition);

        UpdatePlayerChipPosition();
        audioSource.PlayOneShot(moveSound);

        targetCard.OnPlayerEnter();

        // Calculate hunger cost based on terrain
        int hungerCost = CalculateHungerCost(targetCard);
        modifyHunger(-hungerCost);
        LogDebug($"Hunger reduced by {hungerCost}. Current hunger: {totalHunger}");

        // Check if satiated for healing
        if (isSatiated())
        {
            LogDebug("Player is fully satiated!");
            applySatiationBonus();
        }

        if (isDead())
        {
            OnPlayerDeath();
        }

        boardManager.DecayScentGrid();

        if (wolfAI != null)
        {
            wolfAI.MoveAllWolves();
            wolfAI.UpdateAllWolfVisibility();
        }

        return true;
    }

    /// <summary>
    /// Calculates hunger cost based on the terrain card
    /// </summary>
    private int CalculateHungerCost(Card card)
    {
        if (card is TerrainCard terrainCard)
        {
            // Terrain cards have their own hunger modifier
            return terrainCard.HungerModifier;
        }
        
        // Default cost for non-terrain cards
        return baseHungerConsumption;
    }

    public Vector2Int GetPosition()
    {
        return currentPosition;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        currentPosition = newPosition;
        LogDebug($"Player position set to ({currentPosition.x}, {currentPosition.y})");
        
        if (boardManager != null)
        {
            boardManager.SetPlayerPosition(currentPosition);
        }
        
        UpdatePlayerChipPosition();
    }
    
    private void UpdatePlayerChipPosition()
    {
        Debug.Log($"[Player] UpdatePlayerChipPosition called. PlayerChipInstance null? {playerChipInstance == null}");
        
        if (playerChipInstance == null)
        {
            Debug.LogWarning("Player: Cannot update chip position - playerChipInstance is null! Assign a prefab in the Inspector.");
            return;
        }
        
        if (boardManager == null)
        {
            Debug.LogError("Player: Cannot update chip position - BoardManager is null");
            return;
        }
        
        Debug.Log($"[Player] Current position: ({currentPosition.x}, {currentPosition.y})");
        
        CardVisual cardVisual = boardManager.GetCardVisualAt(currentPosition.x, currentPosition.y);
        
        if (cardVisual == null)
        {
            Debug.LogError($"Player: Cannot find card visual at position ({currentPosition.x}, {currentPosition.y})");
            return;
        }
        
        Vector3 cardWorldPosition = cardVisual.transform.position;
        
        Debug.Log($"[Player] Card world position: {cardWorldPosition}");
        Debug.Log($"[Player] Chip offset: {chipOffset}");
        
        Vector3 newChipPosition = cardWorldPosition + chipOffset;
        playerChipInstance.transform.position = newChipPosition;
        
        if (!playerChipInstance.activeSelf)
        {
            Debug.LogWarning("Player chip model was inactive - activating it now");
            playerChipInstance.SetActive(true);
        }
        
        Debug.Log($"[Player] Player chip moved to world position {newChipPosition}, chip active: {playerChipInstance.activeSelf}");
        LogDebug($"Player chip moved to world position {newChipPosition}");
    }

    // Resource Management

    public void modifyHunger(int amount)
    {
        totalHunger = Mathf.Clamp(totalHunger + amount, 0, hungerCap);
        LogDebug($"Hunger modified by {amount}. Current hunger: {totalHunger}/{hungerCap}");
    }

    public void ModifyHunger(int amount)
    {
        totalHunger = Mathf.Clamp(totalHunger + amount, 0, hungerCap);
        LogDebug($"Hunger modified by {amount}. Current hunger: {totalHunger}/{hungerCap}");
    }

    public void modifyHealth(int amount)
    {
        totalHealth = Mathf.Max(0, totalHealth + amount);
        LogDebug($"Health modified by {amount}. Current health: {totalHealth}");

        if (totalHealth <= 0)
        {
            LogDebug("Player has died!");
            OnPlayerDeath();
        }
    }

    public void ModifyHealth(int amount)
    {
        totalHealth = Mathf.Max(0, totalHealth + amount);
        LogDebug($"Health modified by {amount}. Current health: {totalHealth}");

        if (totalHealth <= 0)
        {
            LogDebug("Player has died!");
            OnPlayerDeath();
        }
    }

    public void modifyBloodpoints(int amount)
    {
        totalBloodpoints = Mathf.Max(0, totalBloodpoints + amount);
        LogDebug($"Bloodpoints modified by {amount}. Current bloodpoints: {totalBloodpoints}");
    }

    public void ModifyBloodpoints(int amount)
    {
        totalBloodpoints = Mathf.Max(0, totalBloodpoints + amount);
        LogDebug($"Bloodpoints modified by {amount}. Current bloodpoints: {totalBloodpoints}");
    }

    public void modifyBloodpointCardVisited(int amount)
    {
        bloodpointCardsVisited += amount;
        LogDebug($"{amount} bloodpoint card visited. Bloodpoint cards visited: {bloodpointCardsVisited}");
    }

    public bool isStarving()
    {
        return totalHunger <= 0;
    }

    public bool isSatiated()
    {
        return totalHunger == hungerCap;
    }

    public void applySatiationBonus()
    {
        if (totalHealth < 5)
        {
            totalHealth += 1;
        }
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

    private void exchangeFoodForHealth()
    {
        if (!canHealSelf)
        {
            Debug.Log("[Player] Cannot heal self - blocked by item effect (Old Bread)");
            return;
        }

        if (totalHealth < 5 && totalHunger >= 5)
        {
            totalHunger -= 5;
            totalHealth += 1;
            Debug.Log("[Player] Exchanged 5 Food for 1 Health");
        }
        else
        {
            Debug.Log("[Player] Cannot exchange Food for Health - either Health is full or not enough Food");
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
    
    private void HandleResourceExchangeInput()
    {
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

            if (UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                ExchangeFoodForHealth();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ExchangeFoodForBloodpoints();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ExchangeHealthForBloodpoints();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ExchangeFoodForHealth();
        }
    }
    
    private void HandleAltarInteraction()
    {
        if (boardManager == null)
            return;
    
        Card currentCard = boardManager.GetCardAt(currentPosition.x, currentPosition.y);
    
        if (currentCard == null || !(currentCard is AltarCard))
            return;
    
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
            {
                transferBloodpointsIntoAltar();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            transferBloodpointsIntoAltar();
        }
    }

    public void ExchangeFoodForBloodpoints()
    {
        exchangeFoodForBloodpoints();
    }

    public void ExchangeHealthForBloodpoints()
    {
        exchangeHealthForBloodpoints();
    }

    public void ExchangeFoodForHealth()
    {
        exchangeFoodForHealth();
    }

    public void DepositBloodpointsToAltar()
    {
        transferBloodpointsIntoAltar();
    }
    
    public void SetLastBloodPointCardVisited(BloodPointEventCard card)
    {
        lastBloodPointCardVisited = card;
        LogDebug($"Last bloodpoint card visited set to: {card.GetType().Name}");
    }
    
    public BloodPointEventCard GetLastBloodPointCardVisited()
    {
        return lastBloodPointCardVisited;
    }

    private void OnPlayerDeath()
    {
        Debug.LogWarning("GAME OVER: Player has died!");

        if (UIQueueManager.Instance != null)
        {
            UIQueueManager.Instance.QueueGameOver("Du bist gestorben! Deine Gesundheit hat 0 erreicht.");
        }
        else
        {
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
    }

    public int GetAltarRequirement() => AltarRequirements;

    public void SetAltarRequirement(int requirement)
    {
        AltarRequirements = requirement;
        LogDebug($"Altar requirement set to: {AltarRequirements}");
    }

    public void ResetToStartingValues()
    {
        totalHunger = 30;
        hungerCap = 30;
        totalHealth = 5;
        totalBloodpoints = 0;
        bloodpointsStoredInAltar = 0;
        bloodpointCardsVisited = 0;

        canHealSelf = true;

        lastBloodPointCardVisited = null;

        if (itemManager != null)
        {
            itemManager.ResetItemStates();
        }

        LogDebug("Player resources reset to starting values");
    }

    public ItemManager GetItemManager()
    {
        return itemManager;
    }

    public bool CanHealSelf() => canHealSelf;

    public void SetCanHealSelf(bool value)
    {
        canHealSelf = value;
        LogDebug($"Can heal self set to: {value}");
    }

    public void SetHealth(int value)
    {
        totalHealth = value;
        LogDebug($"Health set to: {totalHealth}");
    }

    public void SetBloodpoints(int value)
    {
        totalBloodpoints = value;
        LogDebug($"Bloodpoints set to: {totalBloodpoints}");
    }

    public void ModifyHungerCap(int amount)
    {
        hungerCap = Mathf.Max(0, hungerCap + amount);
        totalHunger = Mathf.Clamp(totalHunger, 0, hungerCap);
        LogDebug($"Hunger cap modified by {amount}. New cap: {hungerCap}, current hunger: {totalHunger}");
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Player] {message}");
        }
    }
    
    // Getters
    public int GetHunger() => totalHunger;
    public int GetHungerCap() => hungerCap;
    public int GetHealth() => totalHealth;
    public int GetBloodpoints() => totalBloodpoints;
    public int GetBloodpointCardsVisited() => bloodpointCardsVisited;
    public int GetBloodpointsInAltar() => bloodpointsStoredInAltar;
    
    // Legacy compatibility method (returns 0 since stamina is removed)
    public int GetStamina() => 0;
    public int GetStaminaCap() => 0;
}


