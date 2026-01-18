using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Verwaltet das UI für Händler-Interaktionen
/// Design konsistent mit EventUIManager - einfaches Panel mit Titel, Beschreibung und Buttons
/// </summary>
public class TraderUIManager : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject traderPanel;
    
    [Header("Panel Elements")]
    [SerializeField] private TextMeshProUGUI traderTitleText;
    [SerializeField] private TextMeshProUGUI traderDescriptionText;
    
    [Header("Item Choice Buttons")]
    [SerializeField] private Button itemSlot1Button;
    [SerializeField] private Button itemSlot2Button;
    [SerializeField] private Button itemSlot3Button;
    [SerializeField] private TextMeshProUGUI itemSlot1ButtonText;
    [SerializeField] private TextMeshProUGUI itemSlot2ButtonText;
    [SerializeField] private TextMeshProUGUI itemSlot3ButtonText;
    
    [Header("Item Descriptions")]
    [SerializeField] private TextMeshProUGUI itemSlot1DescriptionText;
    [SerializeField] private TextMeshProUGUI itemSlot2DescriptionText;
    [SerializeField] private TextMeshProUGUI itemSlot3DescriptionText;
    
    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI closeButtonText;
    
    private TraderCard currentTrader;
    private Player player;
    
    private void Awake()
    {
        // Hide panel initially
        if (traderPanel != null)
            traderPanel.SetActive(false);
        
        // Setup button listeners
        if (itemSlot1Button != null)
            itemSlot1Button.onClick.AddListener(() => OnItemSlotClicked(0));
        
        if (itemSlot2Button != null)
            itemSlot2Button.onClick.AddListener(() => OnItemSlotClicked(1));
        
        if (itemSlot3Button != null)
            itemSlot3Button.onClick.AddListener(() => OnItemSlotClicked(2));
        
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        
        // Set default texts
        if (traderTitleText != null)
            traderTitleText.text = "Händler";
        
        if (closeButtonText != null)
            closeButtonText.text = "Schließen";
        
        // Find player reference
        player = Object.FindFirstObjectByType<Player>();
    }
    
    /// <summary>
    /// Öffnet das Händler-UI mit dem angegebenen Händler
    /// </summary>
    public void ShowTraderUI(TraderCard trader)
    {
        if (trader == null)
        {
            Debug.LogError("[TraderUIManager] Cannot show trader UI - trader is null!");
            return;
        }
        
        currentTrader = trader;
        
        // Update title
        if (traderTitleText != null)
            traderTitleText.text = "Händler";
        
        // Update description with flavor text + bloodpoints
        UpdateDescriptionText();
        
        // Update all item slots
        UpdateAllItemSlots();
        
        // Show the panel
        if (traderPanel != null)
            traderPanel.SetActive(true);
        
        Debug.Log($"[TraderUIManager] Trader UI opened");
    }
    
    /// <summary>
    /// Aktualisiert den Beschreibungstext (Flavor + Blutpunkte)
    /// </summary>
    private void UpdateDescriptionText()
    {
        if (traderDescriptionText == null || currentTrader == null) return;
        
        string flavorText = currentTrader.GetFlavorText();
        int bloodpoints = player != null ? player.GetBloodpoints() : 0;
        
        traderDescriptionText.text = $"{flavorText}\n\nDeine Blutpunkte: {bloodpoints}";
    }
    
    /// <summary>
    /// Aktualisiert die Anzeige aller Item-Slots
    /// </summary>
    private void UpdateAllItemSlots()
    {
        if (currentTrader == null) return;
        
        UpdateItemSlot(0, itemSlot1ButtonText, itemSlot1DescriptionText, itemSlot1Button);
        UpdateItemSlot(1, itemSlot2ButtonText, itemSlot2DescriptionText, itemSlot2Button);
        UpdateItemSlot(2, itemSlot3ButtonText, itemSlot3DescriptionText, itemSlot3Button);
    }
    
    /// <summary>
    /// Aktualisiert einen einzelnen Item-Slot
    /// Format: "Itemname (-X BP)" oder "Itemname (Kostenlos)"
    /// </summary>
    private void UpdateItemSlot(int slotIndex, TextMeshProUGUI buttonText, TextMeshProUGUI descriptionText, Button button)
    {
        if (currentTrader == null || slotIndex >= currentTrader.GetOfferCount())
        {
            // Slot ist leer
            if (buttonText != null)
                buttonText.text = "---";
            if (descriptionText != null)
                descriptionText.text = "";
            if (button != null)
                button.interactable = false;
            return;
        }
        
        ItemManager.ItemType item = currentTrader.GetOfferedItem(slotIndex);
        int price = currentTrader.GetPriceForSlot(slotIndex);
        
        // Update button text: "Itemname (-X BP)"
        if (buttonText != null)
        {
            string itemName = GetItemDisplayName(item);
            string priceText = price == 0 ? "Kostenlos" : $"-{price} BP";
            buttonText.text = $"{itemName} ({priceText})";
        }
        
        // Update item description
        if (descriptionText != null)
        {
            descriptionText.text = GetItemDescription(item);
        }
        
        // Update button interactability
        if (button != null)
        {
            bool canAfford = player != null && player.GetBloodpoints() >= price;
            bool hasInventorySpace = player != null && player.GetItemManager() != null && 
                                     player.GetItemManager().GetItemCount() < 3;
            
            button.interactable = canAfford && hasInventorySpace;
        }
    }
    
    /// <summary>
    /// Wird aufgerufen wenn ein Item-Slot geklickt wird
    /// </summary>
    private void OnItemSlotClicked(int slotIndex)
    {
        if (currentTrader == null)
        {
            Debug.LogError("[TraderUIManager] Cannot purchase - no active trader!");
            return;
        }
        
        Debug.Log($"[TraderUIManager] Attempting to purchase item in slot {slotIndex}");
        
        bool success = currentTrader.TryPurchaseItem(slotIndex);
        
        if (success)
        {
            // Update UI after successful purchase
            UpdateDescriptionText();
            UpdateAllItemSlots();
            
            Debug.Log($"[TraderUIManager] Purchase successful!");
        }
        else
        {
            Debug.Log($"[TraderUIManager] Purchase failed!");
        }
    }
    
    /// <summary>
    /// Wird aufgerufen wenn der Schließen-Button geklickt wird
    /// </summary>
    private void OnCloseClicked()
    {
        Debug.Log("[TraderUIManager] Closing trader UI");
        
        if (traderPanel != null)
            traderPanel.SetActive(false);
        
        currentTrader = null;
    }
    
    /// <summary>
    /// Schließt das Händler-UI (falls offen)
    /// </summary>
    public void HideTraderUI()
    {
        if (traderPanel != null)
            traderPanel.SetActive(false);
        
        currentTrader = null;
    }
    
    /// <summary>
    /// Gibt true zurück wenn das Händler-UI aktuell geöffnet ist
    /// </summary>
    public bool IsTraderUIOpen()
    {
        return traderPanel != null && traderPanel.activeSelf;
    }
    
    /// <summary>
    /// Konvertiert ItemType zu lesbarem deutschen Namen
    /// </summary>
    private string GetItemDisplayName(ItemManager.ItemType item)
    {
        switch (item)
        {
            case ItemManager.ItemType.Flashlight: return "Taschenlampe";
            case ItemManager.ItemType.HuntingKnife: return "Jagdmesser";
            case ItemManager.ItemType.JarOfNeedles: return "Nadeln im Glas";
            case ItemManager.ItemType.DriedDragonfly: return "Getrocknete Libelle";
            case ItemManager.ItemType.OldBread: return "Altes Brot";
            case ItemManager.ItemType.PileOfAshes: return "Haufen Asche";
            case ItemManager.ItemType.BearClaw: return "Bärenkralle";
            case ItemManager.ItemType.EmergencyRations: return "Notrationen";
            case ItemManager.ItemType.ObsidianShard: return "Obsidianscherbe";
            case ItemManager.ItemType.BunnyStatue: return "Hasenstatue";
            case ItemManager.ItemType.ClimbingRope: return "Kletterseil";
            default: return item.ToString();
        }
    }
    
    /// <summary>
    /// Gibt die Beschreibung/Effekte eines Items zurück
    /// </summary>
    private string GetItemDescription(ItemManager.ItemType item)
    {
        switch (item)
        {
            case ItemManager.ItemType.Flashlight:
                return "Wölfe sind immer sichtbar.";
            
            case ItemManager.ItemType.HuntingKnife:
                return "Alle Minigames werden erleichtert.";
            
            case ItemManager.ItemType.JarOfNeedles:
                return "BP-Gewinn: +1 BP pro Waldkarte in derselben Reihe.\nZerstörung: -1 BP pro Waldkarte in derselben Reihe.";
            
            case ItemManager.ItemType.DriedDragonfly:
                return "BP-Gewinn: +1 BP pro Sumpfkarte in derselben Spalte.\nZerstörung: -1 BP pro Sumpfkarte in derselben Spalte.";
            
            case ItemManager.ItemType.OldBread:
                return "Selbstheilung deaktiviert (keine 5 Nahrung → 1 HP).\n+5 BP pro verlorenem Lebenspunkt.";
            
            case ItemManager.ItemType.PileOfAshes:
                return "BP-Gewinn verdoppelt (×2).\nBei HP-Verlust: Verliere ALLE BP.";
            
            case ItemManager.ItemType.BearClaw:
                return "Wenn ein Item zerstört wird: 70% Chance auf 10 BP, 30% Chance auf -1 HP, auch bei Zerstörung dieses Items.";
            
            case ItemManager.ItemType.EmergencyRations:
                return "Nahrungskapazität +10.\nZerstörung: +10 Nahrung.";
            
            case ItemManager.ItemType.ObsidianShard:
                return "Bei Tod: Volle Gesundheit wiederherstellen.\nDanach: Alle BP-Gewinne werden für den Rest der Runde halbiert.";
            
            case ItemManager.ItemType.BunnyStatue:
                return "Kann als Nahrung verzehrt werden.\nZerstörung: +10 Nahrung.";
            
            case ItemManager.ItemType.ClimbingRope:
                return "Ermöglicht Bewegung auf Felskarten.";
            
            default:
                return "Keine Beschreibung verfügbar.";
        }
    }
}


