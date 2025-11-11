using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    [Header("Ressourcen Display")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI bloodPointsText;
    public TextMeshProUGUI altarBloodText;
    public TextMeshProUGUI turnText;
    
    [Header("Inventar")]
    public Transform inventoryPanel;
    public GameObject inventorySlotPrefab;
    private List<GameObject> inventorySlots = new List<GameObject>();
    
    [Header("Event Dialog")]
    public GameObject eventDialogPanel;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescriptionText;
    public Button eventOption1Button;
    public Button eventOption2Button;
    public TextMeshProUGUI option1Text;
    public TextMeshProUGUI option2Text;
    
    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    
    [Header("Game Over")]
    public GameObject gameOverPanel;
    public GameObject winPanel;
    
    private Card currentEventCard;
    private Player2 currentPlayer;

    void Start()
    {
        if (eventDialogPanel != null)
            eventDialogPanel.SetActive(false);
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    public void UpdateResourceDisplay(Player2 player)
    {
        if (healthText != null)
            healthText.text = $"❤️ {player.health}";
        if (foodText != null)
            foodText.text = $"🍖 {player.food}";
        if (staminaText != null)
            staminaText.text = $"⚡ {player.stamina}/{player.maxStamina}";
        if (bloodPointsText != null)
            bloodPointsText.text = $"🩸 {player.bloodPoints}";
        if (altarBloodText != null)
            altarBloodText.text = $"🗿 {player.bloodPointsAtAltar}/25";
        if (turnText != null)
            turnText.text = $"Zug: {player.turnNumber}";
        
        UpdateInventoryDisplay(player);
    }

    void UpdateInventoryDisplay(Player2 player)
    {
        if (inventoryPanel == null) return;
        
        // Alte Slots löschen
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        inventorySlots.Clear();
        
        // Neue Slots erstellen
        for (int i = 0; i < player.maxInventorySize; i++)
        {
            GameObject slot = Instantiate(inventorySlotPrefab, inventoryPanel);
            inventorySlots.Add(slot);
            
            if (i < player.inventory.Count)
            {
                Item item = player.inventory[i];
                TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();
                if (slotText != null)
                {
                    slotText.text = item.name;
                }
                
                // Click Handler für Item-Zerstörung
                Button btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    ItemType itemType = item.itemType;
                    btn.onClick.AddListener(() => OnItemClicked(player, itemType));
                }
            }
        }
    }

    void OnItemClicked(Player2 player, ItemType itemType)
    {
        // Rechtsklick-Menü oder Zerstörungs-Dialog hier implementieren
        Debug.Log($"Item geklickt: {itemType}");
    }

    public void ShowEventDialog(Card card, Player2 player)
    {
        currentEventCard = card;
        currentPlayer = player;
        
        if (eventDialogPanel == null) return;
        
        eventDialogPanel.SetActive(true);
        
        switch (card.eventType)
        {
            case EventType.Wolf:
                ShowWolfEvent();
                break;
            case EventType.Rabbit:
                ShowRabbitEvent();
                break;
            case EventType.BerryBush:
                ShowBerryBushEvent();
                break;
            case EventType.OldHouse:
                ShowOldHouseEvent();
                break;
            case EventType.Merchant:
                ShowMerchantEvent();
                break;
        }
        
        if (card.cardType == CardType.BloodEvent)
        {
            ExecuteBloodEvent(card, player);
            eventDialogPanel.SetActive(false);
        }
    }

    void ShowWolfEvent()
    {
        if (eventTitleText != null)
            eventTitleText.text = "🐺 Wolf!";
        if (eventDescriptionText != null)
            eventDescriptionText.text = "Ein Wolf versperrt den Weg!";
        
        if (option1Text != null)
            option1Text.text = "Fliehen";
        if (option2Text != null)
            option2Text.text = "Kämpfen";
        
        eventOption1Button.onClick.RemoveAllListeners();
        eventOption1Button.onClick.AddListener(() => WolfFlee());
        
        eventOption2Button.onClick.RemoveAllListeners();
        eventOption2Button.onClick.AddListener(() => WolfFight());
    }

    void WolfFlee()
    {
        int roll = Random.Range(1, 7);
        bool hasKnife = currentPlayer.HasItem(ItemType.Knife);
        int threshold = hasKnife ? 1 : 2;
        
        if (roll <= threshold)
        {
            currentPlayer.health -= 1;
            currentPlayer.OnHealthLost(1);
            ShowEventResult($"Flucht fehlgeschlagen (Würfel: {roll})! -1 Gesundheit");
        }
        else
        {
            ShowEventResult($"Erfolgreich entkommen (Würfel: {roll})!");
        }
        
        CloseEventDialog();
    }

    void WolfFight()
    {
        int roll = Random.Range(1, 7);
        bool hasKnife = currentPlayer.HasItem(ItemType.Knife);
        int threshold = hasKnife ? 3 : 4;
        
        if (roll < threshold)
        {
            currentPlayer.health -= 2;
            currentPlayer.OnHealthLost(2);
            ShowEventResult($"Kampf verloren (Würfel: {roll})! -2 Gesundheit");
        }
        else
        {
            currentPlayer.AddBloodPoints(5);
            ShowEventResult($"Kampf gewonnen (Würfel: {roll})! +5 Blutpunkte");
        }
        
        CloseEventDialog();
    }

    void ShowRabbitEvent()
    {
        if (currentPlayer.HasItem(ItemType.RabbitStatue))
        {
            currentPlayer.food += 5;
            ShowEventResult("Hasenstatue bereits vorhanden! +5 Essen");
            CloseEventDialog();
        }
        else
        {
            currentPlayer.AddItem(ItemDatabase.CreateItem(ItemType.RabbitStatue));
            ShowEventResult("Hasenstatue erhalten!");
            CloseEventDialog();
        }
    }

    void ShowBerryBushEvent()
    {
        if (eventTitleText != null)
            eventTitleText.text = "🫐 Beerenbusch";
        if (eventDescriptionText != null)
            eventDescriptionText.text = "Beeren pflücken? Sie könnten giftig sein...";
        
        if (option1Text != null)
            option1Text.text = "Pflücken";
        if (option2Text != null)
            option2Text.text = "Ignorieren";
        
        eventOption1Button.onClick.RemoveAllListeners();
        eventOption1Button.onClick.AddListener(() => PickBerries());
        
        eventOption2Button.onClick.RemoveAllListeners();
        eventOption2Button.onClick.AddListener(() => CloseEventDialog());
    }

    void PickBerries()
    {
        int roll = Random.Range(1, 7);
        
        currentPlayer.food += 5;
        
        if (roll <= 2)
        {
            currentPlayer.maxStamina -= 1;
            currentPlayer.stamina = Mathf.Min(currentPlayer.stamina, currentPlayer.maxStamina);
            ShowEventResult($"Giftige Beeren! (Würfel: {roll}) +5 Essen, aber -1 Max Ausdauer");
        }
        else
        {
            ShowEventResult($"Leckere Beeren! (Würfel: {roll}) +5 Essen");
        }
        
        CloseEventDialog();
    }

    void ShowOldHouseEvent()
    {
        int roll = Random.Range(1, 7);
        
        if (roll <= 2)
        {
            currentPlayer.health -= 1;
            currentPlayer.OnHealthLost(1);
            ShowEventResult($"Falle! (Würfel: {roll}) -1 Gesundheit");
        }
        else if (roll <= 4)
        {
            Item randomItem = ItemDatabase.GetRandomItem();
            if (currentPlayer.AddItem(randomItem))
            {
                ShowEventResult($"Item gefunden! (Würfel: {roll}) {randomItem.name}");
            }
            else
            {
                ShowEventResult($"Item gefunden aber Inventar voll! (Würfel: {roll})");
            }
        }
        else
        {
            currentPlayer.food += 10;
            ShowEventResult($"Essen gefunden! (Würfel: {roll}) +10 Essen");
        }
        
        CloseEventDialog();
    }

    void ShowMerchantEvent()
    {
        // Händler-Logik später implementieren
        ShowEventResult("Händler begegnet! (Noch nicht implementiert)");
        CloseEventDialog();
    }

    void ExecuteBloodEvent(Card card, Player2 player)
    {
        switch (card.bloodEventType)
        {
            case BloodEventType.GainFive:
                player.AddBloodPoints(5);
                ShowEventResult("+5 Blutpunkte");
                break;
            case BloodEventType.GainPerBloodCard:
                // Zähle aufgedeckte BP-Karten
                int count = 0;
                foreach (Card c in FindObjectsOfType<Card>())
                {
                    if (c.cardType == CardType.BloodEvent && c.isRevealed)
                        count++;
                }
                player.AddBloodPoints(count * 2);
                ShowEventResult($"+{count * 2} Blutpunkte ({count} BP-Karten)");
                break;
            case BloodEventType.GainPerHealthLost:
                int healthLost = 5 - player.health;
                player.AddBloodPoints(healthLost * 2);
                ShowEventResult($"+{healthLost * 2} Blutpunkte ({healthLost} HP verloren)");
                break;
            case BloodEventType.LosePerItem:
                int itemCount = player.inventory.Count;
                player.bloodPoints -= itemCount * 5;
                ShowEventResult($"-{itemCount * 5} Blutpunkte ({itemCount} Items)");
                break;
            case BloodEventType.LosePerHunger:
                player.bloodPoints -= player.turnsHungry;
                ShowEventResult($"-{player.turnsHungry} Blutpunkte ({player.turnsHungry} Hunger)");
                break;
            case BloodEventType.GainIfFood:
                if (player.food > 0)
                {
                    player.AddBloodPoints(3);
                    ShowEventResult("+3 Blutpunkte (Essen vorhanden)");
                }
                else
                {
                    player.bloodPoints -= 5;
                    ShowEventResult("-5 Blutpunkte (kein Essen)");
                }
                break;
            case BloodEventType.GainPerAdjacent:
                int adjacent = CountAdjacentTerrainCards(card);
                player.AddBloodPoints(adjacent * 2);
                ShowEventResult($"+{adjacent * 2} Blutpunkte ({adjacent} angrenzende Geländekarten)");
                break;
        }
    }

    int CountAdjacentTerrainCards(Card card)
    {
        int count = 0;
        Vector3 pos = card.transform.position;
        float spacing = 2f;
        
        foreach (Card c in FindObjectsOfType<Card>())
        {
            if (c.cardType != CardType.Terrain) continue;
            
            float dx = Mathf.Abs(c.transform.position.x - pos.x);
            float dz = Mathf.Abs(c.transform.position.z - pos.z);
            
            if ((dx == spacing && dz == 0) || (dx == 0 && dz == spacing))
            {
                count++;
            }
        }
        
        return count;
    }

    void ShowEventResult(string message)
    {
        Debug.Log($"Event: {message}");
        // Hier könnte ein temporäres Popup erscheinen
    }

    void CloseEventDialog()
    {
        if (eventDialogPanel != null)
            eventDialogPanel.SetActive(false);
        
        if (currentPlayer != null)
            currentPlayer.UpdateResourceDisplay(currentPlayer);
    }

    public void ShowCardTooltip(Card card)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        
        tooltipPanel.SetActive(true);
        tooltipText.text = $"{card.cardName}\n{GetCardDescription(card)}";
    }

    public void HideCardTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    string GetCardDescription(Card card)
    {
        switch (card.cardType)
        {
            case CardType.Terrain:
                return card.terrainType switch
                {
                    TerrainType.Path => "+1 Ausdauer",
                    TerrainType.Forest => "-1 Ausdauer",
                    TerrainType.Swamp => "-2 Ausdauer",
                    _ => ""
                };
            default:
                return "";
        }
    }

    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void ShowWinScreen()
    {
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    /*internal void ShowEventDialog(Card card, Player2 player2)
    {
        throw new System.NotImplementedException();
    }

  /* internal void UpdateResourceDisplay(Player2 player2)
    {
        throw new System.NotImplementedException();
    }*/
}