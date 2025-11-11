using System;
using System.Collections.Generic;
using UnityEngine;

public class Player2 : MonoBehaviour
{
    // Ressourcen
    public int health = 5;
    public int food = 20;
    public int stamina = 5;
    public int maxStamina = 5;
    public int bloodPoints = 0;
    public int bloodPointsAtAltar = 0;
    public int turnNumber = 0;
    public int turnsHungry = 0;
    public int flashlightCooldown = 0;
    
    // Inventar
    public List<Item> inventory = new List<Item>();
    public int maxInventorySize = 3;
    
    // Bewegung
    public Card currentCard;
    private List<Card> reachableCards = new List<Card>();
    public float moveSpeed = 5f;
    public float jumpHeight = 0.8f;
    private bool isMoving = false;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float moveProgress = 0f;
    
    // UI Referenzen (im Inspector zuweisen)
    public GameUI gameUI;

    void Start()
    {
        // Spieler startet immer mit Taschenlampe
        AddItem(new Item { itemType = ItemType.Flashlight, name = "Taschenlampe" });
        UpdateUI();
    }

    void Update()
    {
        if (isMoving)
        {
            moveProgress += moveSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(moveProgress);
            
            // Parabelförmiger Sprung
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += height;
            
            transform.position = currentPos;
            
            if (t >= 1f)
            {
                isMoving = false;
                OnArriveAtCard();
            }
        }
        
        // Flashlight Cooldown
        if (flashlightCooldown > 0 && turnNumber % 10 == 0)
        {
            flashlightCooldown--;
        }
    }

    public void MoveTo(Card newCard)
    {
        if (!IsAdjacent(newCard) || isMoving)
        {
            Debug.LogWarning("Bewegung nicht erlaubt!");
            return;
        }

        // Bewegung konsumiert Essen
        food -= 1;
        turnNumber++;
        
        // Hunger-Schaden
        if (food <= 0)
        {
            turnsHungry++;
            if (turnsHungry <= 4)
            {
                health -= 1;
                Debug.Log($"Hunger! Gesundheit: {health}");
                OnHealthLost(1);
            }
        }
        else
        {
            turnsHungry = 0;
        }
        
        // Bewegungsanimation starten
        startPos = transform.position;
        targetPos = newCard.transform.position + Vector3.up * 0.6f;
        moveProgress = 0f;
        isMoving = true;
        currentCard = newCard;
        
        Debug.Log($"Bewege zu {newCard.cardName} - Essen: {food}, Gesundheit: {health}");
        
        UpdateUI();
        HighlightReachableCards();
    }

    void OnArriveAtCard()
    {
        if (currentCard == null) return;
        
        // Karte aufdecken, falls verdeckt
        if (!currentCard.isRevealed)
        {
            currentCard.RevealCard();
        }
        
        // Karteneffekte ausführen
        ExecuteCardEffect(currentCard);
        
        // Prüfe Ausdauer-Schaden
        if (stamina <= 0)
        {
            health -= 1;
            Debug.Log($"Keine Ausdauer! Gesundheit: {health}");
            OnHealthLost(1);
        }
        
        // Prüfe Tod
        if (health <= 0)
        {
            CheckObsidianShard();
            if (health <= 0)
            {
                GameOver();
            }
        }
        
        UpdateUI();
    }

    void ExecuteCardEffect(Card card)
    {
        switch (card.cardType)
        {
            case CardType.Terrain:
                HandleTerrainCard(card);
                break;
            case CardType.Event:
                HandleEventCard(card);
                break;
            case CardType.Altar:
                ReachAltar();
                break;
        }
    }

    void HandleTerrainCard(Card card)
    {
        switch (card.terrainType)
        {
            case TerrainType.Path:
                stamina = Mathf.Min(stamina + 1, maxStamina);
                Debug.Log("Pfad: +1 Ausdauer");
                break;
            case TerrainType.Forest:
                stamina -= 1;
                Debug.Log("Wald: -1 Ausdauer");
                break;
            case TerrainType.Swamp:
                stamina -= 2;
                Debug.Log("Sumpf: -2 Ausdauer");
                break;
        }
    }

    void HandleEventCard(Card card)
    {
        if (card.hasBeenTriggered) return;
        card.hasBeenTriggered = true;
        
        if (gameUI != null)
        {
            gameUI.ShowEventDialog(card, this);
        }
    }

    void ReachAltar()
    {
        if (bloodPoints > 0)
        {
            bloodPointsAtAltar += bloodPoints;
            bloodPoints = 0;
            Debug.Log($"Blutpunkte abgeliefert! Am Altar: {bloodPointsAtAltar}");
        }
        
        if (bloodPointsAtAltar >= 25)
        {
            WinGame();
        }
        else
        {
            Debug.Log($"Noch {25 - bloodPointsAtAltar} Blutpunkte benötigt!");
        }
        
        UpdateUI();
    }

    public void AddBloodPoints(int amount)
    {
        int actualAmount = amount;
        
        // Messer Bonus
        if (HasItem(ItemType.Knife) && amount > 0)
        {
            actualAmount += 1;
            Debug.Log("Messer: +1 Bonusblutpunkt");
        }
        
        // Aschehaufen verdoppelt
        if (HasItem(ItemType.AshPile) && amount > 0)
        {
            actualAmount *= 2;
            Debug.Log("Aschehaufen: Doppelte Blutpunkte!");
        }
        
        bloodPoints += actualAmount;
        Debug.Log($"Blutpunkte: {bloodPoints}");
        UpdateUI();
    }

    public void OnHealthLost(int amount)
    {
        // Altes Brot gibt Blutpunkte
        if (HasItem(ItemType.OldBread))
        {
            AddBloodPoints(5 * amount);
            Debug.Log("Altes Brot: +5 Blutpunkte pro Gesundheit");
        }
        
        // Aschehaufen verliert alle Blutpunkte
        if (HasItem(ItemType.AshPile))
        {
            bloodPoints = 0;
            Debug.Log("Aschehaufen: Alle Blutpunkte verloren!");
        }
    }

    void CheckObsidianShard()
    {
        if (HasItem(ItemType.ObsidianShard))
        {
            int healing = bloodPoints / 5;
            health = healing;
            bloodPoints = 0;
            RemoveItem(ItemType.ObsidianShard);
            Debug.Log($"Obsidiansplitter: Wiederbelebt mit {healing} Gesundheit!");
        }
    }

    public bool AddItem(Item item)
    {
        if (inventory.Count >= maxInventorySize)
        {
            Debug.Log("Inventar voll!");
            return false;
        }
        
        inventory.Add(item);
        
        // Krähenfeder erhöht Ausdauer
        if (item.itemType == ItemType.CrowFeather)
        {
            maxStamina += 2;
            stamina += 2;
        }
        
        Debug.Log($"Item erhalten: {item.name}");
        UpdateUI();
        return true;
    }

    public void RemoveItem(ItemType itemType)
    {
        Item item = inventory.Find(i => i.itemType == itemType);
        if (item != null)
        {
            inventory.Remove(item);
            
            // Krähenfeder reduziert Ausdauer
            if (itemType == ItemType.CrowFeather)
            {
                maxStamina -= 2;
                stamina = Mathf.Min(stamina, maxStamina);
            }
            
            UpdateUI();
        }
    }

    public void DestroyItem(ItemType itemType)
    {
        Item item = inventory.Find(i => i.itemType == itemType);
        if (item == null) return;
        
        Debug.Log($"Zerstöre Item: {item.name}");
        
        // Zerstörungs-Effekte
        switch (itemType)
        {
            case ItemType.CrowFeather:
                AddBloodPoints(3);
                break;
            case ItemType.BearClaw:
                AddBloodPoints(5);
                break;
            case ItemType.EmergencyRations:
                food += 10;
                break;
            case ItemType.OldBread:
                food += 10;
                health += 3;
                bloodPoints /= 2;
                break;
            case ItemType.AshPile:
                health -= 1;
                OnHealthLost(1);
                break;
        }
        
        // Bärenkralle Bonus
        if (HasItem(ItemType.BearClaw))
        {
            AddBloodPoints(5);
            Debug.Log("Bärenkralle: +5 Blutpunkte für Zerstörung");
        }
        
        RemoveItem(itemType);
    }

    public bool HasItem(ItemType itemType)
    {
        return inventory.Exists(i => i.itemType == itemType);
    }

    public bool IsAdjacent(Card target)
    {
        if (currentCard == null) return true;

        float dx = Mathf.Abs(target.transform.position.x - currentCard.transform.position.x);
        float dz = Mathf.Abs(target.transform.position.z - currentCard.transform.position.z);

        float spacing = 2f;
        return (dx == spacing && dz == 0) || (dx == 0 && dz == spacing);
    }

    public void HighlightReachableCards()
    {
        foreach (Card c in FindObjectsOfType<Card>())
            c.SetHighlight(false);

        reachableCards.Clear();

        if (currentCard == null) return;

        Vector3 pos = currentCard.transform.position;
        float spacing = 2f;

        foreach (Card c in FindObjectsOfType<Card>())
        {
            float dx = Mathf.Abs(c.transform.position.x - pos.x);
            float dz = Mathf.Abs(c.transform.position.z - pos.z);

            if ((dx == spacing && dz == 0) || (dx == 0 && dz == spacing))
            {
                reachableCards.Add(c);
                c.SetHighlight(true);
            }
        }
    }

    void UpdateUI()
    {
        if (gameUI != null)
        {
            gameUI.UpdateResourceDisplay(this);
        }
    }

    void WinGame()
    {
        Debug.Log("GEWONNEN! 25 Blutpunkte am Altar abgeliefert!");
        Time.timeScale = 0;
        if (gameUI != null)
        {
            gameUI.ShowWinScreen();
        }
    }

    void GameOver()
    {
        Debug.Log("GAME OVER - Gestorben!");
        Time.timeScale = 0;
        if (gameUI != null)
        {
            gameUI.ShowGameOverScreen();
        }
    }

    internal void UpdateResourceDisplay(Player2 currentPlayer)
    {
        throw new NotImplementedException();
    }
}