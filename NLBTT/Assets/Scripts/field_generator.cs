using UnityEngine;

public class FieldGenerator : MonoBehaviour
{
    [Header("Spielfeld Einstellungen")]
    public int gridSize = 5;
    public float tileSpacing = 2f;
    
    [Header("Prefabs")]
    public GameObject cardPrefab;
    
    [Header("Materialien")]
    public Material startMaterial;
    public Material altarMaterial;
    public Material cardBackMaterial;
    public Material pathMaterial;
    public Material forestMaterial;
    public Material swampMaterial;
    public Material eventMaterial;
    public Material bloodEventMaterial;
    
    public GameObject playerInstance;
    private Card[,] cardGrid;

    void Start()
    {
        GameObject playerInstance = GameObject.FindWithTag("Player");
        
        // Maus sichtbar und entsperrt
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        GenerateField();
    }

    public void GenerateField()
    {
        cardGrid = new Card[gridSize, gridSize];
        
        // Zufällige Positionen für Start und Altar
        int startX = Random.Range(0, gridSize);
        int startY = Random.Range(0, gridSize);

        int altarX, altarY;
        do
        {
            altarX = Random.Range(0, gridSize);
            altarY = Random.Range(0, gridSize);
        } while (altarX == startX && altarY == startY);

        // Generiere Karten
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 position = new Vector3(x * tileSpacing, 0, y * tileSpacing);
                GameObject cardObj = Instantiate(cardPrefab, position, Quaternion.identity);
                cardObj.transform.parent = transform;
                
                Card card = cardObj.GetComponent<Card>();
                if (card == null)
                {
                    card = cardObj.AddComponent<Card>();
                }
                
                // Collider hinzufügen falls nicht vorhanden
                if (cardObj.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = cardObj.AddComponent<BoxCollider>();
                    collider.size = new Vector3(1.8f, 0.2f, 1.8f);
                }
                
                cardGrid[x, y] = card;
                
                // Kartentyp zuweisen
                if (x == startX && y == startY)
                {
                    SetupStartCard(card);
                    
                    // Spieler positionieren
                    if (playerInstance != null)
                    {
                        playerInstance.transform.position = position + Vector3.up * 0.6f;
                        Player2 playerScript = playerInstance.GetComponent<Player2>();
                        if (playerScript != null)
                        {
                            playerScript.currentCard = card;
                            playerScript.HighlightReachableCards();
                        }
                    }
                }
                else if (x == altarX && y == altarY)
                {
                    SetupAltarCard(card);
                }
                else
                {
                    SetupRandomCard(card);
                }
                
                // Material zuweisen
                AssignCardMaterial(card);
            }
        }

        Debug.Log($"Spielfeld {gridSize}x{gridSize} generiert!");
        Debug.Log($"Start: ({startX},{startY}) | Altar: ({altarX},{altarY})");
    }

    void SetupStartCard(Card card)
    {
        card.cardType = CardType.Start;
        card.cardName = "Start";
        card.isRevealed = true;
    }

    void SetupAltarCard(Card card)
    {
        card.cardType = CardType.Altar;
        card.cardName = "Altar";
        card.isRevealed = true;
    }

    void SetupRandomCard(Card card)
    {
        // Verteilung: 40% Terrain, 30% Event, 20% BloodEvent, 10% Blank
        float roll = Random.value;
        
        if (roll < 0.4f)
        {
            // Terrain
            card.cardType = CardType.Terrain;
            
            // 50% Pfad, 30% Wald, 20% Sumpf
            float terrainRoll = Random.value;
            if (terrainRoll < 0.5f)
                card.terrainType = TerrainType.Path;
            else if (terrainRoll < 0.8f)
                card.terrainType = TerrainType.Forest;
            else
                card.terrainType = TerrainType.Swamp;
        }
        else if (roll < 0.7f)
        {
            // Event
            card.cardType = CardType.Event;
            card.eventType = (EventType)Random.Range(0, 5);
        }
        else if (roll < 0.9f)
        {
            // Blood Event
            card.cardType = CardType.BloodEvent;
            card.bloodEventType = (BloodEventType)Random.Range(0, 8);
        }
        else
        {
            // Blank
            card.cardType = CardType.Blank;
            card.cardName = "Leere Karte";
        }
    }

    void AssignCardMaterial(Card card)
    {
        Renderer rend = card.GetComponent<Renderer>();
        if (rend == null) return;
        
        if (card.isRevealed)
        {
            // Aufgedeckte Karten
            switch (card.cardType)
            {
                case CardType.Start:
                    if (startMaterial != null)
                        rend.material = startMaterial;
                    break;
                case CardType.Altar:
                    if (altarMaterial != null)
                        rend.material = altarMaterial;
                    break;
            }
        }
        else
        {
            // Verdeckte Karten
            if (cardBackMaterial != null)
            {
                card.cardBackMaterial = cardBackMaterial;
                rend.material = cardBackMaterial;
            }
        }
        
        // Front-Material für Aufdecken vorbereiten
        switch (card.cardType)
        {
            case CardType.Terrain:
                switch (card.terrainType)
                {
                    case TerrainType.Path:
                        card.cardFrontMaterial = pathMaterial;
                        break;
                    case TerrainType.Forest:
                        card.cardFrontMaterial = forestMaterial;
                        break;
                    case TerrainType.Swamp:
                        card.cardFrontMaterial = swampMaterial;
                        break;
                }
                break;
            case CardType.Event:
                card.cardFrontMaterial = eventMaterial;
                break;
            case CardType.BloodEvent:
                card.cardFrontMaterial = bloodEventMaterial;
                break;
        }
    }

    public Card GetCardAt(int x, int y)
    {
        if (x < 0 || x >= gridSize || y < 0 || y >= gridSize)
            return null;
        return cardGrid[x, y];
    }

    public Vector2Int GetCardGridPosition(Card card)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (cardGrid[x, y] == card)
                    return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }
}