using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cardSpacingX = 0.1f;
    [SerializeField] private float cardSpacingZ = 0.1f;
    [SerializeField] private Vector3 boardOffset = Vector3.zero;
    
    [Header("Layout Generation Settings")]
    [SerializeField] private int numberOfWaypoints = 5;
    [SerializeField] [Range(1, 5)] private int minBuffRadius = 1;
    [SerializeField] [Range(1, 5)] private int maxBuffRadius = 2;
    [SerializeField] [Range(0f, 1f)] private float orthogonalBuffProbability = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float diagonalBuffProbability = 0.5f;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int manualSeed = 12345;
    
    [Header("Card Prefab Mappings")]
    [SerializeField] private CardPrefabMapping[] cardPrefabMappings;
    
    [Header("Specific Card Counts")]
    [SerializeField] private int wolfCardCount;
    [SerializeField] private int wolfdenCardCount;
    [SerializeField] private int traderCardCount;
    [SerializeField] private int altarCardCount;
    [SerializeField] private int hareCardCount;
    [SerializeField] private int berryCardCount;
    [SerializeField] private int bloodpointCardCount;
    
    [Header("Player Position")]
    [SerializeField] private Vector2Int playerPosition;
    
    [Header("AI References")]
    [SerializeField] private WolfAI wolfAI;
    
    [Header("Scent Tracking")]
    [SerializeField] private float scentDecayRate = 0.15f;
    
    private Card[,] cardGrid;
    private CardVisual[,] cardVisualGrid;
    private bool[,] layoutGrid;
    private float[,] scentGrid; // NEW: Scent tracking grid
    
    void Start()
    {
        GenerateInitialBoard();
    }
    
    private void GenerateInitialBoard()
    {
        Vector2Int startPos = new Vector2Int(gridWidth / 2, 0);
        int? seed = useRandomSeed ? (int?)null : manualSeed;
        
        layoutGrid = BoardLayoutGenerator.Generate(
            gridWidth,
            gridHeight,
            startPos,
            numberOfWaypoints,
            minBuffRadius,
            maxBuffRadius,
            orthogonalBuffProbability,
            diagonalBuffProbability,
            seed
        );
        
        BoardLayoutGenerator.DebugPrintLayout(layoutGrid);
        
        cardGrid = GenerateLevel(layoutGrid);
        cardVisualGrid = new CardVisual[gridWidth, gridHeight];
        
        // Initialize scent grid
        InitializeScentGrid();
        
        PlaceCardsOnBoard(cardGrid);
        
        InitializePlayerPosition();
    }
    
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Regenerating board (R key pressed)");
            RegenerateBoard();
        }
        
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Turning over all cards (T key pressed)");
            TurnOverAllCards();
        }
    }

    /// <summary>
    /// Initializes the scent grid with all values at 0
    /// </summary>
    private void InitializeScentGrid()
    {
        scentGrid = new float[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                scentGrid[x, y] = 0f;
            }
        }
        
        Debug.Log($"[BoardManager] Scent grid initialized: {gridWidth}x{gridHeight}");
    }
    
    /// <summary>
    /// Sets the scent value at a specific position to 1.0 (player just stepped here)
    /// </summary>
    public void SetPlayerScent(Vector2Int position)
    {
        if (IsValidPosition(position))
        {
            scentGrid[position.x, position.y] = 1.0f;
            Debug.Log($"[BoardManager] Player scent set to 1.0 at ({position.x}, {position.y})");
        }
    }
    
    /// <summary>
    /// Decays all scent values by the decay rate (called after each player turn)
    /// </summary>
    public void DecayScentGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (scentGrid[x, y] > 0f)
                {
                    scentGrid[x, y] = Mathf.Max(0f, scentGrid[x, y] - scentDecayRate);
                }
            }
        }
        
        Debug.Log($"[BoardManager] Scent grid decayed by {scentDecayRate}");
    }
    
    /// <summary>
    /// Gets the scent value at a specific position
    /// </summary>
    public float GetScentAt(Vector2Int position)
    {
        if (IsValidPosition(position))
        {
            return scentGrid[position.x, position.y];
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the entire scent grid (for AI pathfinding)
    /// </summary>
    public float[,] GetScentGrid()
    {
        return scentGrid;
    }
    
    /// <summary>
    /// Checks if a position is within grid bounds
    /// </summary>
    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridWidth && 
               position.y >= 0 && position.y < gridHeight;
    }

    private Card[,] GenerateLevel(bool[,] layout)
    {
        Card[,] grid = new Card[gridWidth, gridHeight];
        List<Card> cardsToPlace = new List<Card>();

        int totalCardSlots = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (layout[x, y])
                    totalCardSlots++;
            }
        }
        
        int remainingSlots = totalCardSlots - 1;

        AddSpecificCards(cardsToPlace);
        
        int randomCardsNeeded = remainingSlots - cardsToPlace.Count;
        
        for (int i = 0; i < randomCardsNeeded; i++)
        {
            Card randomCard = CreateRandomCard();
            cardsToPlace.Add(randomCard);
        }
        
        System.Random rng = new System.Random();
        for (int i = cardsToPlace.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            Card temp = cardsToPlace[i];
            cardsToPlace[i] = cardsToPlace[j];
            cardsToPlace[j] = temp;
        }

        Vector2Int startPos = new Vector2Int(gridWidth / 2, 0);
        grid[startPos.x, startPos.y] = new StartCard();

        int cardIndex = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!layout[x, y])
                    continue;
                
                if (grid[x, y] != null)
                    continue;
                
                grid[x, y] = cardsToPlace[cardIndex];
                cardIndex++;
            }
        }

        return grid;
    }

    private void AddSpecificCards(List<Card> cardList)
    {
        for (int i = 0; i < wolfCardCount; i++)
            cardList.Add(new WolfCard());
        
        for (int i = 0; i < wolfdenCardCount; i++)
            cardList.Add(new WolfdenCard());
        
        for (int i = 0; i < traderCardCount; i++)
            cardList.Add(new TraderCard());
        
        for (int i = 0; i < altarCardCount; i++)
            cardList.Add(new AltarCard());
        
        for (int i = 0; i < hareCardCount; i++)
            cardList.Add(new HareCard());

        for (int i = 0; i < berryCardCount; i++)
            cardList.Add(new BerryCard());
        
        for (int i = 0; i < bloodpointCardCount; i++)
            cardList.Add(CreateRandomBloodpointCard());
    }

    private Card CreateRandomCard()
    {
        float roll = Random.value;

        if (roll < 0.4f)
            return new ForestCard();
        else if (roll < 0.8f)
            return new PathCard();
        else if (roll < 0.95f)
            return new SwampCard();
        else
            return new RockCard();
    }
    
    private Card CreateRandomBloodpointCard()
    {
        float roll = Random.value;

        if (roll < 0.20f)
            return new BloodpointCard_A();
        else if (roll < 0.30f)
            return new BloodpointCard_B();
        else if (roll < 0.50f)
            return new BloodpointCard_C();
        else if (roll < 0.5f)
            return new BloodpointCard_D();
        else if (roll < 0.80f)
            return new BloodpointCard_E();
        else if (roll < 0.90f)
            return new BloodpointCard_F();
        else
            return new BloodpointCard_G();
    }
    
    private void PlaceCardsOnBoard(Card[,] cardGrid)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Card cardLogic = cardGrid[x, y];
                
                if (cardLogic == null)
                    continue;
                
                GameObject cardPrefab = GetPrefabForCard(cardLogic);
                
                if (cardPrefab == null)
                {
                    Debug.LogError($"No prefab found for card at ({x}, {y}): {cardLogic.GetType().Name}");
                    continue;
                }
                
                Vector3 position = new Vector3(x * cardSpacingX, 0, y * cardSpacingZ) + boardOffset;
                
                Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
                
                GameObject cardObj = Instantiate(cardPrefab, position, rotation, transform);
                cardObj.name = $"Card_{x}_{y}_{cardLogic.GetType().Name}";
                
                CardVisual visual = cardObj.GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardLogic(cardLogic);
                    cardVisualGrid[x, y] = visual;
                }
                else
                {
                    Debug.LogError($"CardVisual component missing on prefab for {cardLogic.GetType().Name}");
                }
            }
        }
    }
    
    private GameObject GetPrefabForCard(Card card)
    {
        string typeName = card.GetType().Name;
        
        foreach (var mapping in cardPrefabMappings)
        {
            if (mapping.cardTypeName == typeName)
                return mapping.prefab;
        }
        
        Debug.LogError($"No prefab mapping found for card type: {typeName}");
        return null;
    }
    
    public Card GetCardAt(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return cardGrid[x, y];
        return null;
    }
    
    public CardVisual GetCardVisualAt(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return cardVisualGrid[x, y];
        return null;
    }
    
    public bool IsCardAdjacent(Vector2Int posA, Vector2Int posB)
    {
        int xDiff = Mathf.Abs(posA.x - posB.x);
        int yDiff = Mathf.Abs(posA.y - posB.y);
    
        return (xDiff == 1 && yDiff == 0) || (xDiff == 0 && yDiff == 1);
    }
    
    /// <summary>
    /// Reveals the card at the specified position and all adjacent cards
    /// Called by Player when moving to a new position
    /// </summary>
    public void RevealCardAndAdjacent(Vector2Int position)
    {
        List<CardVisual> cardsToReveal = new List<CardVisual>();
        
        // Add the center card
        CardVisual centerCard = GetCardVisualAt(position.x, position.y);
        if (centerCard != null && centerCard.GetCardLogic() != null && centerCard.GetCardLogic().TurnedAround)
        {
            cardsToReveal.Add(centerCard);
        }
        
        // Check all four adjacent directions (up, down, left, right)
        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };
        
        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentPos = position + offset;
            CardVisual adjacentCard = GetCardVisualAt(adjacentPos.x, adjacentPos.y);
            
            // Only add if the card exists, has logic, and is face-down
            if (adjacentCard != null && adjacentCard.GetCardLogic() != null && adjacentCard.GetCardLogic().TurnedAround)
            {
                cardsToReveal.Add(adjacentCard);
            }
        }
        
        // Reveal all cards simultaneously with animation
        if (cardsToReveal.Count > 0)
        {
            Debug.Log($"Revealing {cardsToReveal.Count} cards simultaneously");
            StartCoroutine(RevealCardsSimultaneously(cardsToReveal));
        }
    }
    
    /// <summary>
    /// Coroutine that reveals multiple cards simultaneously with flip animation
    /// </summary>
    private System.Collections.IEnumerator RevealCardsSimultaneously(List<CardVisual> cards)
    {
        // Start all card flip animations at the same time
        foreach (CardVisual card in cards)
        {
            StartCoroutine(card.FlipCardAnimation());
        }
        
        // Wait for animations to complete (adjust timing to match your animation duration)
        yield return new WaitForSeconds(0.6f);
    }
    
    public void InitializePlayerPosition()
    {
        playerPosition = new Vector2Int(gridWidth / 2, 0);
        
        // Set initial scent at starting position
        SetPlayerScent(playerPosition);
        
        // Reveal starting card and adjacent cards
        RevealCardAndAdjacent(playerPosition);
        
        UpdateAllCardOutlines();
    
        if (wolfAI != null)
        {
            wolfAI.SpawnWolves();
        }
        else
        {
            Debug.LogWarning("[BoardManager] WolfAI reference is not assigned!");
        }
    }
    
    public Vector2Int GetPlayerPosition()
    {
        return playerPosition;
    }
    
    public void SetPlayerPosition(Vector2Int newPosition)
    {
        playerPosition = newPosition;
        UpdateAllCardOutlines();
    }
    
    private void UpdateAllCardOutlines()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CardVisual visual = cardVisualGrid[x, y];
                if (visual != null)
                {
                    bool isAdjacent = IsCardAdjacent(playerPosition, new Vector2Int(x, y));
                    visual.SetAdjacentToPlayer(isAdjacent);
                }
            }
        }
    }
    
    public void TurnOverAllCards()
    {
        int faceUpCount = 0;
        int faceDownCount = 0;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CardVisual visual = cardVisualGrid[x, y];
                if (visual != null)
                {
                    Card card = visual.GetCardLogic();
                    if (card != null)
                    {
                        if (card.TurnedAround)
                            faceDownCount++;
                        else
                            faceUpCount++;
                    }
                }
            }
        }
        
        bool shouldTurnFaceUp = faceDownCount > faceUpCount;
        
        if (shouldTurnFaceUp)
        {
            int turnedCount = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CardVisual visual = cardVisualGrid[x, y];
                    if (visual != null)
                    {
                        Card card = visual.GetCardLogic();
                        if (card != null && card.TurnedAround)
                        {
                            visual.TurnCardOver();
                            turnedCount++;
                        }
                    }
                }
            }
            Debug.Log($"Turned {turnedCount} cards face-up");
        }
        else
        {
            Debug.Log("Regenerating board to reset cards to face-down state");
            RegenerateBoard();
        }
    }
    
    public void ResetAllTraders()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Card card = GetCardAt(x, y);
                if (card != null && card is TraderCard traderCard)
                {
                    traderCard.ResetTrader();
                }
            }
        }
        Debug.Log("[BoardManager] All traders reset");
    }
    
    private void ClearBoard()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    
        cardGrid = null;
        cardVisualGrid = null;
        layoutGrid = null;
        scentGrid = null;
    
        Debug.Log("Board cleared");
    }
    
    public void RegenerateBoard()
    {
        ResetAllTraders();
        ClearBoard();

        Vector2Int startPos = new Vector2Int(gridWidth / 2, 0);
        int? seed = useRandomSeed ? (int?)null : manualSeed;
    
        layoutGrid = BoardLayoutGenerator.Generate(
            gridWidth,
            gridHeight,
            startPos,
            numberOfWaypoints,
            minBuffRadius,
            maxBuffRadius,
            orthogonalBuffProbability,
            diagonalBuffProbability,
            seed
        );
    
        BoardLayoutGenerator.DebugPrintLayout(layoutGrid);
    
        cardGrid = GenerateLevel(layoutGrid);
        cardVisualGrid = new CardVisual[gridWidth, gridHeight];
        
        // Reinitialize scent grid
        InitializeScentGrid();
        
        PlaceCardsOnBoard(cardGrid);

        InitializePlayerPosition();

        FoliageDecorator foliageDecorator = FindObjectOfType<FoliageDecorator>();
        if (foliageDecorator != null)
        {
            foliageDecorator.SpawnFoliage();
        }
        
        EventIconManager eventIconManager = FindObjectOfType<EventIconManager>();
        if (eventIconManager != null)
        {
            eventIconManager.OnBoardRegenerated();
            Debug.Log("[BoardManager] Event icons refreshed");
        }
        else
        {
            Debug.LogWarning("[BoardManager] EventIconManager not found - event icons won't be refreshed");
        }

        Debug.Log("Board regenerated");
    }
    
    public Vector2Int GetAltarPosition()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Card card = GetCardAt(x, y);
                if (card != null && card is AltarCard)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
    
        Debug.LogWarning("BoardManager: No Altar card found on the board!");
        return new Vector2Int(-1, -1);
    }
    
    public int GetGridWidth()
    {
        return gridWidth;
    }

    public int GetGridHeight()
    {
        return gridHeight;
    }
}

[System.Serializable]
public class CardPrefabMapping
{
    public string cardTypeName;
    public GameObject prefab;
}