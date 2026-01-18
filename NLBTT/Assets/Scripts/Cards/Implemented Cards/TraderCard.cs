using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Händler-Karte: Verkauft Items gegen Blutpunkte
/// - Bietet 3 zufällige Items an
/// - Erstes Item kostenlos, danach steigender Preis
/// - Jeder Händler hat eigenes Inventar und eigene Preiskalkulation
/// - Event schließt sich nie (immer wieder besuchbar)
/// </summary>
public class TraderCard : Card
{
    // Händler-Texte
    private string traderFlavorText = "Der Händler sieht dich erwartungsvoll an. Seine Waren liegen vor dir ausgebreitet.";
    
    // Angebotene Items (3 Slots)
    private List<ItemManager.ItemType> offeredItems = new List<ItemManager.ItemType>();
    
    // Preiskalkulation
    private int itemsPurchased = 0;
    private const int BASE_PRICE_MULTIPLIER = 5; // Basis-Multiplikator für Preise
    
    // Referenzen
    private Player player;
    private ItemManager itemManager;
    
    // Seed für diesen speziellen Händler (für konsistente Zufälligkeit)
    private int traderSeed;
    
    public TraderCard()
    {
        title = "Händler";
        canMoveOnto = true;
        blocksLineOfSight = false;
        hasEvent = true; // Händler hat immer ein Event
        isEventClosed = false; // Event schließt sich NIE
        
        // Generiere einen zufälligen Seed für diesen Händler
        traderSeed = Random.Range(0, 100000);
        
        Debug.Log($"[TraderCard] Created with seed {traderSeed}");
    }
    
    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        
        // Finde Player-Referenz
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
            if (player != null)
            {
                itemManager = player.GetItemManager();
            }
        }
        
        // Initialisiere Angebot, falls noch nicht geschehen
        if (offeredItems.Count == 0)
        {
            GenerateInitialOffer();
        }
        
        Debug.Log($"[TraderCard] Player entered. Press SPACEBAR to trade.");
    }
    
    public override void TriggerEvent()
    {
        // Event ist nie geschlossen, kann immer getriggert werden
        Debug.Log($"[TraderCard] Opening trader UI");
        
        // Öffne Händler-UI
        TraderUIManager traderUI = Object.FindFirstObjectByType<TraderUIManager>();
        
        if (traderUI != null)
        {
            traderUI.ShowTraderUI(this);
        }
        else
        {
            Debug.LogError("[TraderCard] TraderUIManager not found in scene!");
        }
    }
    
    /// <summary>
    /// Generiert das initiale Angebot (3 zufällige Items)
    /// </summary>
    private void GenerateInitialOffer()
    {
        offeredItems.Clear();
        
        // Verwende Händler-spezifischen Seed für konsistente Zufälligkeit
        Random.State oldState = Random.state;
        Random.InitState(traderSeed);
        
        List<ItemManager.ItemType> availableItems = GetAvailableItems();
        
        // Wähle 3 zufällige Items aus verfügbaren Items
        for (int i = 0; i < 3 && availableItems.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableItems.Count);
            offeredItems.Add(availableItems[randomIndex]);
            availableItems.RemoveAt(randomIndex);
        }
        
        // Restore Random state
        Random.state = oldState;
        
        Debug.Log($"[TraderCard] Initial offer generated: {string.Join(", ", offeredItems)}");
    }
    
    /// <summary>
    /// Gibt alle Items zurück, die der Spieler NICHT bereits besitzt
    /// </summary>
    private List<ItemManager.ItemType> GetAvailableItems()
    {
        List<ItemManager.ItemType> available = new List<ItemManager.ItemType>();
        
        // Alle Item-Typen durchgehen
        foreach (ItemManager.ItemType itemType in System.Enum.GetValues(typeof(ItemManager.ItemType)))
        {
            // Nur Items anbieten, die der Spieler NICHT bereits hat
            if (itemManager == null || !itemManager.HasItem(itemType))
            {
                available.Add(itemType);
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// Versucht ein Item zu kaufen
    /// </summary>
    public bool TryPurchaseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= offeredItems.Count)
        {
            Debug.LogError($"[TraderCard] Invalid slot index: {slotIndex}");
            return false;
        }
        
        ItemManager.ItemType itemToBuy = offeredItems[slotIndex];
        int price = GetPriceForSlot(slotIndex);
        
        // Check: Hat Spieler genug Blutpunkte?
        if (player.GetBloodpoints() < price)
        {
            Debug.Log($"[TraderCard] Not enough bloodpoints! Need {price}, have {player.GetBloodpoints()}");
            return false;
        }
        
        // Check: Hat Spieler Platz im Inventar?
        if (itemManager.GetItemCount() >= 3)
        {
            Debug.Log($"[TraderCard] Inventory full! Cannot purchase item.");
            return false;
        }
        
        // Bezahle Preis
        itemManager.ModifyPlayerBloodpoints(-price);
        
        // Füge Item zum Inventar hinzu
        bool success = itemManager.TryAddItem(itemToBuy);
        
        if (success)
        {
            Debug.Log($"[TraderCard] Purchased {itemToBuy} for {price} BP");
            
            // Erhöhe Kaufzähler
            itemsPurchased++;
            
            // Regeneriere komplettes Angebot mit neuem Seed
            traderSeed = Random.Range(0, 100000);
            GenerateInitialOffer();
            
            return true;
        }
        else
        {
            // Refund if item couldn't be added
            itemManager.ModifyPlayerBloodpoints(price);
            Debug.LogError($"[TraderCard] Failed to add item to inventory, refunded {price} BP");
            return false;
        }
    }
    
    /// <summary>
    /// Berechnet den Preis für einen Item-Slot
    /// Erstes Item kostenlos, danach exponentiell steigend
    /// </summary>
    public int GetPriceForSlot(int slotIndex)
    {
        if (itemsPurchased == 0)
        {
            // Erstes Item ist immer kostenlos
            return 0;
        }
        
        // Exponentieller Preisanstieg: 5, 15, 35, 75, 155, ...
        // Formel: BASE_PRICE_MULTIPLIER * (2^n - 1)
        int price = BASE_PRICE_MULTIPLIER * (Mathf.RoundToInt(Mathf.Pow(2, itemsPurchased)) - 1);
        
        return price;
    }
    
    /// <summary>
    /// Gibt das angebotene Item an einem Slot zurück
    /// </summary>
    public ItemManager.ItemType GetOfferedItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= offeredItems.Count)
        {
            Debug.LogError($"[TraderCard] Invalid slot index: {slotIndex}");
            return ItemManager.ItemType.Flashlight; // Fallback
        }
        
        return offeredItems[slotIndex];
    }
    
    /// <summary>
    /// Gibt die Anzahl der angebotenen Items zurück
    /// </summary>
    public int GetOfferCount()
    {
        return offeredItems.Count;
    }
    
    /// <summary>
    /// Gibt den Flavour-Text des Händlers zurück
    /// </summary>
    public string GetFlavorText()
    {
        return traderFlavorText;
    }
    
    /// <summary>
    /// Gibt die Anzahl der gekauften Items zurück (für UI-Anzeige)
    /// </summary>
    public int GetPurchaseCount()
    {
        return itemsPurchased;
    }
    
    /// <summary>
    /// Setzt den Händler zurück (wird von BoardManager bei Reset aufgerufen)
    /// </summary>
    public void ResetTrader()
    {
        itemsPurchased = 0;
        traderSeed = Random.Range(0, 100000);
        offeredItems.Clear();
        GenerateInitialOffer();
        
        Debug.Log($"[TraderCard] Reset complete. New seed: {traderSeed}");
    }
}

