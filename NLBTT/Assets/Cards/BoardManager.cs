using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cardSpacingX = 0.1f; // Horizontal spacing (between columns)
    [SerializeField] private float cardSpacingZ = 0.1f; // Vertical spacing (between rows)
    [SerializeField] private Vector3 boardOffset = Vector3.zero; // Offset for entire board position (e.g., to place on a table)
    
    [Header("Card Prefab Mappings")]
    [SerializeField] private CardPrefabMapping[] cardPrefabMappings;
    
    [Header("Specific Card Counts")]
    [SerializeField] private int wolfCardCount;
    [SerializeField] private int traderCardCount;
    [SerializeField] private int altarCardCount;
    [SerializeField] private int hareCardCount;
    [SerializeField] private int berryCardCount;
    [SerializeField] private int bloodpointCardCount;
    
    [Header("Player Position")]
    [SerializeField] private Vector2Int playerPosition;
    
    private Card[,] cardGrid;
    private CardVisual[,] cardVisualGrid;
    
    void Start()
    {
        GenerateInitialBoard();
    }
    
    /// <summary>
    /// Generates the initial board on game start
    /// </summary>
    private void GenerateInitialBoard()
    {
        cardGrid = GenerateLevel();
        cardVisualGrid = new CardVisual[gridWidth, gridHeight];
        PlaceCardsOnBoard(cardGrid);
        InitializePlayerPosition();
    }
    
    /// <summary>
    /// For testing: Press R key to regenerate the board
    /// Press T key to turn over all cards
    /// Uses new Input System
    /// </summary>
    void Update()
    {
        // Press R to regenerate board (testing shortcut)
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Regenerating board (R key pressed)");
            RegenerateBoard();
        }
        
        // Press T to turn over all cards (debugging shortcut)
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Turning over all cards (T key pressed)");
            TurnOverAllCards();
        }
    }

    private Card[,] GenerateLevel()
    {
        Card[,] grid = new Card[gridWidth, gridHeight];
        List<Card> cardsToPlace = new List<Card>();

        // Reserve spot for start card (not shuffled)
        int remainingSlots = (gridWidth * gridHeight) - 1;

        // Add specific cards first
        AddSpecificCards(cardsToPlace);
        
        // Calculate how many random cards we need to fill remaining slots
        int randomCardsNeeded = remainingSlots - cardsToPlace.Count;
        
        // Fill the rest with weighted random cards
        for (int i = 0; i < randomCardsNeeded; i++)
        {
            Card randomCard = CreateRandomCard();
            cardsToPlace.Add(randomCard);
        }
        
        // Shuffle ALL cards (specific + random)
        System.Random rng = new System.Random();
        for (int i = cardsToPlace.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            Card temp = cardsToPlace[i];
            cardsToPlace[i] = cardsToPlace[j];
            cardsToPlace[j] = temp;
        }

        // Place start card at center bottom (fixed position, not shuffled)
        grid[gridWidth / 2, 0] = new StartCard();

        // Fill remaining slots with shuffled cards
        int cardIndex = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
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
        {
            cardList.Add(new WolfCard());
        }
        
        for (int i = 0; i < traderCardCount; i++)
        {
            cardList.Add(new TraderCard());
        }
        
        for (int i = 0; i < altarCardCount; i++)
        {
            cardList.Add(new AltarCard());
        }
        
        for (int i = 0; i < hareCardCount; i++)
        {
            cardList.Add(new HareCard());
        }

        for (int i = 0; i < berryCardCount; i++)
        {
            cardList.Add(new BerryCard());
        }
        
        for (int i = 0; i < bloodpointCardCount; i++)
        {
            cardList.Add(CreateRandomBloodpointCard());
        }
        
        
    }

    private Card CreateRandomCard()
    {
        float roll = Random.value;

        if (roll < 0.4f)
            return new ForestCard();
        else if (roll < 0.7f)
            return new PathCard();
        else if (roll < 0.85f)
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
        else if (roll < 0.60f)
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
                
                // Get the appropriate prefab for this card type
                GameObject cardPrefab = GetPrefabForCard(cardLogic);
                
                if (cardPrefab == null)
                {
                    Debug.LogError($"No prefab found for card at ({x}, {y}): {cardLogic.GetType().Name}");
                    continue;
                }
                
                // Calculate position using the correct spacing variables and board offset
                Vector3 position = new Vector3(x * cardSpacingX, 0, y * cardSpacingZ) + boardOffset;
                
                // Rotate the card to lay flat (90 degrees around X axis)
                Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
                
                // Instantiate the card
                GameObject cardObj = Instantiate(cardPrefab, position, rotation, transform);
                cardObj.name = $"Card_{x}_{y}_{cardLogic.GetType().Name}";
                
                // Connect the visual to the logic
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
    
    // Helper method to get card logic at a position
    public Card GetCardAt(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return cardGrid[x, y];
        return null;
    }
    
    // Helper method to get card visual at a position
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
    
        // Adjacent means exactly one unit away in one direction, zero in the other
        return (xDiff == 1 && yDiff == 0) || (xDiff == 0 && yDiff == 1);
    }
    
    /// <summary>
    /// Initialize player at the starting position (center bottom)
    /// Call this after board generation
    /// </summary>
    public void InitializePlayerPosition()
    {
        playerPosition = new Vector2Int(gridWidth / 2, 0);
        UpdateAllCardOutlines();
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
    
    /// <summary>
    /// Debug function: Toggles all cards between face-up and face-down
    /// Useful for testing and seeing the entire board layout
    /// </summary>
    public void TurnOverAllCards()
    {
        int faceUpCount = 0;
        int faceDownCount = 0;
        
        // First, count how many cards are in each state
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
        
        // Determine which direction to toggle based on majority state
        bool shouldTurnFaceUp = faceDownCount > faceUpCount;
        
        if (shouldTurnFaceUp)
        {
            // Turn all face-down cards to face-up
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
            // To turn cards back face-down, we need to regenerate the board
            Debug.Log("Regenerating board to reset cards to face-down state");
            RegenerateBoard();
        }
    }
    
    /// <summary>
    /// Clears the current board by destroying all card GameObjects
    /// </summary>
    private void ClearBoard()
    {
        // Destroy all card GameObjects that are children of this BoardManager
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    
        // Clear the arrays
        cardGrid = null;
        cardVisualGrid = null;
    
        Debug.Log("Board cleared");
    }
    
    /// <summary>
    /// Generates a completely new board, destroying the old one
    /// </summary>
    public void RegenerateBoard()
    {
        // Clear existing board
        ClearBoard();
    
        // Generate new level
        cardGrid = GenerateLevel();
        cardVisualGrid = new CardVisual[gridWidth, gridHeight];
        PlaceCardsOnBoard(cardGrid);
    
        // Reset player position (only if player exists)
        InitializePlayerPosition();
    
        Debug.Log("Board regenerated");
    }
    
    /// <summary>
    /// Finds and returns the position of the Altar card on the board
    /// Returns Vector2Int(-1, -1) if no Altar is found
    /// </summary>
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
        return new Vector2Int(-1, -1); // Sentinel value for not found
    }
}

[System.Serializable]
public class CardPrefabMapping
{
    public string cardTypeName;
    public GameObject prefab;
}

