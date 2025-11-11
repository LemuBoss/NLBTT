using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Card : MonoBehaviour
{
    [Header("Kartentyp")]
    public CardType cardType;
    public TerrainType terrainType;
    public EventType eventType;
    public BloodEventType bloodEventType;

    [Header("Kartendaten")]
    public string cardName;
    public bool isRevealed = false;
    public bool hasBeenTriggered = false;

    [Header("Visuals")]
    public Material cardFrontMaterial;
    public Material cardBackMaterial;
    public Color highlightColor = Color.yellow;

    public Renderer rend;
    public Color baseColor;
    public bool isHighlighted = false;
    public bool isFlipping = false;
    public float flipProgress = 0f;
    public float flipSpeed = 3f;

    public void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            baseColor = rend.material.color;

            // Startkarte und Altar sind aufgedeckt
            if (cardType == CardType.Start || cardType == CardType.Altar)
            {
                isRevealed = true;
            }
            else
            {
                // Andere Karten verdeckt starten
                if (cardBackMaterial != null)
                {
                    rend.material = cardBackMaterial;
                }
            }
        }

        // Namen setzen
        SetCardName();
    }

    void Update()
    {
        if (isFlipping)
        {
            flipProgress += flipSpeed * Time.deltaTime;

            // Rotation von 0 zu 180 Grad um Y-Achse
            float angle = Mathf.Lerp(0, 180, flipProgress);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            // Bei 90 Grad Material wechseln
            if (flipProgress > 0.5f && rend != null && cardFrontMaterial != null)
            {
                rend.material = cardFrontMaterial;
            }

            if (flipProgress >= 1f)
            {
                isFlipping = false;
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
    }

    void SetCardName()
    {
        switch (cardType)
        {
            case CardType.Start:
                cardName = "Start";
                break;
            case CardType.Altar:
                cardName = "Altar";
                break;
            case CardType.Terrain:
                cardName = terrainType.ToString();
                break;
            case CardType.Event:
                cardName = eventType.ToString();
                break;
            case CardType.BloodEvent:
                cardName = "Blutpunkt-Event";
                break;
        }
        gameObject.name = cardName;
    }

    public void RevealCard()
    {
        if (isRevealed) return;

        isRevealed = true;
        isFlipping = true;
        flipProgress = 0f;

        Debug.Log($"Karte aufgedeckt: {cardName}");
    }

    public void SetHighlight(bool state)
    {
        isHighlighted = state;
        if (rend == null) return;

        if (state)
        {
            rend.material.color = highlightColor;
        }
        else
        {
            rend.material.color = baseColor;
        }
    }

    void OnMouseEnter()
    {
        Player2 player = FindObjectOfType<Player2>();
        if (player == null) return;

        if (player.IsAdjacent(this))
        {
            SetHighlight(true);

            // Zeige Karteninfo
            if (isRevealed)
            {
                ShowCardTooltip();
            }
        }
    }

    void OnMouseExit()
    {
        if (!isHighlighted)
        {
            SetHighlight(false);
        }
        HideCardTooltip();
    }

    void OnMouseDown()
    {
        Player2 player = FindObjectOfType<Player2>();
        if (player == null) return;

        if (player.IsAdjacent(this))
        {
            player.MoveTo(this);
        }
    }

    void ShowCardTooltip()
    {
        // Wird vom GameUI System gehandhabt
        GameUI ui = FindObjectOfType<GameUI>();
        if (ui != null)
        {
            ui.ShowCardTooltip(this);
        }
    }

    void HideCardTooltip()
    {
        GameUI ui = FindObjectOfType<GameUI>();
        if (ui != null)
        {
            ui.HideCardTooltip();
        }
    }
}

// Enums für Kartentypen
public enum CardType
{
    Start,
    Altar,
    Terrain,
    Event,
    BloodEvent,
    Blank
}

public enum TerrainType
{
    Path,
    Forest,
    Swamp
}

public enum EventType
{
    Wolf,
    Rabbit,
    BerryBush,
    OldHouse,
    Merchant
}

public enum BloodEventType
{
    GainFive,              // 1: Erhalte 5 Blutpunkte
    GainPerBloodCard,      // 2: 2 BP pro aufgedeckte BP-Karte
    RepeatLast,            // 3: Wiederhole letztes BP-Event
    GainPerHealthLost,     // 4: 2 BP pro verlorene Gesundheit
    LosePerItem,           // 5: Verliere 5 BP pro Item
    LosePerHunger,         // 6: Verliere 1 BP pro Hunger
    GainIfFood,            // 7: 3 BP wenn Essen, sonst -5
    GainPerAdjacent        // 8: 2 BP pro angrenzende Geländekarte
}