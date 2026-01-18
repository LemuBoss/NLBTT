using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the player's inventory and item interactions
/// Handles UI display and item usage
/// </summary>
public class PlayerItemManager : MonoBehaviour
{
    public enum ItemType
    {
        Flashlight,      // Taschenlampe
        BunnyStatue,     // Hasenstatue
        Knife,           // Messer - Blutpunkte bei Gewinn
        JarOfNeedles,    // Nadeln im Glas - Blutpunkte
        DriedDragonfly,  // Getrocknete Libelle - Blutpunkte
        OldBread,        // Altes Brot - Blutpunkte bei Gesundheitverlust
        PileOfAshes,     // Haufen Asche - Blutpunkte verdoppeln/verlieren
        CrowFeather,     // Krähenfeder - Keine Geruchsspur
        BearClaw,        // Bärenkralle - Blutpunkte bei Itemzerstörung
        EmergencyFood,   // Notrationen - Hunger cap erhöhen
        ObsidianShard,   // Obsidiansplitter - Wiederbelebung
        ClimbingRope     // Kletterseil - Felskarten betreten
    }

    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 3;
    private List<ItemType> playerInventory = new List<ItemType>();

    [Header("UI Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI inventoryTitleText;
    [SerializeField] private TextMeshProUGUI inventoryListText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI closeButtonText;

    [Header("Flashlight Cooldown")]
    private int flashlightCooldown = 0;
    private const int FLASHLIGHT_COOLDOWN_MAX = 10;

    private Player playerComponent;
    private bool isPanelOpen = false;

    private void Awake()
    {
        playerComponent = GetComponent<Player>();

        if (playerComponent == null)
        {
            Debug.LogError("[PlayerItemManager] Player component not found!");
        }

        // Hide panel initially
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // Set up button listener
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        // Set default button text
        if (closeButtonText != null)
            closeButtonText.text = "Schließen";

        if (inventoryTitleText != null)
            inventoryTitleText.text = "Inventar";

        // Player starts with Flashlight
        AddItemToInventory(ItemType.Flashlight);
    }

    private void Update()
    {
        // Handle inventory toggle with 'I' key
        if (Input.GetKeyDown(KeyCode.I) ||
            (UnityEngine.InputSystem.Keyboard.current != null &&
             UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame))
        {
            ToggleInventoryUI();
        }
    }

    /// <summary>
    /// Toggles the inventory UI panel
    /// </summary>
    public void ToggleInventoryUI()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[PlayerItemManager] Inventory panel not assigned!");
            LogInventoryToConsole();
            return;
        }

        isPanelOpen = !isPanelOpen;
        inventoryPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            UpdateInventoryUI();
        }

        Debug.Log($"[PlayerItemManager] Inventory UI toggled: {(isPanelOpen ? "Open" : "Closed")}");
    }

    /// <summary>
    /// Updates the inventory UI text with current items
    /// </summary>
    private void UpdateInventoryUI()
    {
        if (inventoryListText == null)
            return;

        string inventoryText = $"Items ({playerInventory.Count}/{maxInventorySlots}):\n\n";

        if (playerInventory.Count == 0)
        {
            inventoryText += "Keine Items vorhanden.";
        }
        else
        {
            for (int i = 0; i < playerInventory.Count; i++)
            {
                inventoryText += $"{i + 1}. {GetItemDisplayName(playerInventory[i])}\n";
                inventoryText += $"   {GetItemDescription(playerInventory[i])}\n\n";
            }
        }

        if (HasItem(ItemType.Flashlight))
        {
            inventoryText += $"\n[Taschenlampe Cooldown: {flashlightCooldown} Züge]";
        }

        inventoryListText.text = inventoryText;
    }

    /// <summary>
    /// Logs inventory to console for debugging
    /// </summary>
    public void LogInventoryToConsole()
    {
        Debug.Log($"[PlayerItemManager] === INVENTAR ({playerInventory.Count}/{maxInventorySlots}) ===");

        if (playerInventory.Count == 0)
        {
            Debug.Log("[PlayerItemManager] Keine Items vorhanden.");
        }
        else
        {
            for (int i = 0; i < playerInventory.Count; i++)
            {
                Debug.Log($"[PlayerItemManager] {i + 1}. {GetItemDisplayName(playerInventory[i])}");
            }
        }

        if (HasItem(ItemType.Flashlight))
        {
            Debug.Log($"[PlayerItemManager] Taschenlampe Cooldown: {flashlightCooldown} Züge");
        }
    }

    /// <summary>
    /// Adds an item to the inventory if there's space
    /// </summary>
    public bool AddItemToInventory(ItemType item)
    {
        if (playerInventory.Count >= maxInventorySlots)
        {
            Debug.Log("[PlayerItemManager] Inventar voll! Kann kein weiteres Item aufnehmen.");
            return false;
        }

        playerInventory.Add(item);
        Debug.Log($"[PlayerItemManager] Item hinzugefügt: {GetItemDisplayName(item)}");

        // Apply passive effects
        ApplyPassiveEffect(item);

        LogInventoryToConsole();
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory
    /// </summary>
    public bool RemoveItemFromInventory(ItemType item)
    {
        if (playerInventory.Contains(item))
        {
            playerInventory.Remove(item);
            Debug.Log($"[PlayerItemManager] Item entfernt: {GetItemDisplayName(item)}");

            // Remove passive effects
            RemovePassiveEffect(item);

            LogInventoryToConsole();
            return true;
        }

        Debug.Log($"[PlayerItemManager] Item nicht im Inventar: {GetItemDisplayName(item)}");
        return false;
    }

    /// <summary>
    /// Destroys an item and triggers its destruction effect
    /// </summary>
    public void DestroyItem(ItemType item)
    {
        if (!playerInventory.Contains(item))
        {
            Debug.Log($"[PlayerItemManager] Kann Item nicht zerstören: {GetItemDisplayName(item)} - nicht im Inventar");
            return;
        }

        Debug.Log($"[PlayerItemManager] Zerstöre Item: {GetItemDisplayName(item)}");

        // Trigger destruction effect
        OnItemDestroyed(item);

        // Remove from inventory
        RemoveItemFromInventory(item);
    }

    /// <summary>
    /// Checks if the player has a specific item
    /// </summary>
    public bool HasItem(ItemType item)
    {
        return playerInventory.Contains(item);
    }

    /// <summary>
    /// Gets the current number of items
    /// </summary>
    public int GetCurrentItemCount()
    {
        return playerInventory.Count;
    }

    /// <summary>
    /// Gets a copy of the inventory list
    /// </summary>
    public List<ItemType> GetInventoryList()
    {
        return new List<ItemType>(playerInventory);
    }

    /// <summary>
    /// Decrements flashlight cooldown (called after player movement)
    /// </summary>
    public void DecrementFlashlightCooldown()
    {
        if (flashlightCooldown > 0)
        {
            flashlightCooldown--;
            Debug.Log($"[PlayerItemManager] Taschenlampe Cooldown: {flashlightCooldown}");
        }
    }

    /// <summary>
    /// Applies passive effects when item is added
    /// </summary>
    private void ApplyPassiveEffect(ItemType item)
    {
        if (playerComponent == null) return;

        switch (item)
        {
            case ItemType.EmergencyFood:
                // Increase hunger cap by 10
                playerComponent.ModifyHungerCap(10);
                Debug.Log("[PlayerItemManager] Notrationen: Hunger-Kapazität um 10 erhöht");
                break;

            case ItemType.OldBread:
                // Prevent self-healing
                playerComponent.SetCanHealSelf(false);
                Debug.Log("[PlayerItemManager] Altes Brot: Selbstheilung deaktiviert");
                break;
        }
    }

    /// <summary>
    /// Removes passive effects when item is removed
    /// </summary>
    private void RemovePassiveEffect(ItemType item)
    {
        if (playerComponent == null) return;

        switch (item)
        {
            case ItemType.EmergencyFood:
                // Decrease hunger cap
                playerComponent.ModifyHungerCap(-10);
                Debug.Log("[PlayerItemManager] Notrationen entfernt: Hunger-Kapazität um 10 verringert");
                break;

            case ItemType.OldBread:
                // Re-enable self-healing
                playerComponent.SetCanHealSelf(true);
                Debug.Log("[PlayerItemManager] Altes Brot entfernt: Selbstheilung aktiviert");
                break;
        }
    }

    /// <summary>
    /// Handles item destruction effects
    /// </summary>
    private void OnItemDestroyed(ItemType item)
    {
        if (playerComponent == null) return;

        switch (item)
        {
            case ItemType.BunnyStatue:
                // Can be eaten for 10 food
                playerComponent.ModifyHunger(10);
                Debug.Log("[PlayerItemManager] Hasenstatue verzehrt: +10 Essen");
                break;

            case ItemType.OldBread:
                // Get 10 food and 3 health, lose half carried bloodpoints
                playerComponent.ModifyHunger(10);
                playerComponent.ModifyHealth(3);
                int halfBloodpoints = playerComponent.GetBloodpoints() / 2;
                playerComponent.ModifyBloodpoints(-halfBloodpoints);
                Debug.Log($"[PlayerItemManager] Altes Brot zerstört: +10 Essen, +3 Gesundheit, -{halfBloodpoints} Blutpunkte");
                break;

            case ItemType.PileOfAshes:
                // Lose 2 health (but not below 1)
                int currentHealth = playerComponent.GetHealth();
                int healthLoss = Mathf.Min(2, currentHealth - 1);
                if (healthLoss > 0)
                {
                    playerComponent.ModifyHealth(-healthLoss);
                }
                Debug.Log($"[PlayerItemManager] Haufen Asche zerstört: -{healthLoss} Gesundheit");
                break;

            case ItemType.CrowFeather:
                // No effect on destruction
                Debug.Log("[PlayerItemManager] Krähenfeder zerstört");
                break;

            case ItemType.BearClaw:
                // Random: 5 bloodpoints or -1 health
                if (Random.value > 0.5f)
                {
                    playerComponent.ModifyBloodpoints(5);
                    Debug.Log("[PlayerItemManager] Bärenkralle zerstört: +5 Blutpunkte");
                }
                else
                {
                    playerComponent.ModifyHealth(-1);
                    Debug.Log("[PlayerItemManager] Bärenkralle zerstört: -1 Leben");
                }
                break;

            case ItemType.EmergencyFood:
                // Get 10 food
                playerComponent.ModifyHunger(10);
                Debug.Log("[PlayerItemManager] Notrationen zerstört: +10 Essen");
                break;
        }
    }

    /// <summary>
    /// Called when player gains bloodpoints (for item effects)
    /// </summary>
    public void OnBloodpointsGained(int amount)
    {
        if (playerComponent == null) return;

        // Knife: Get bonus bloodpoint
        if (HasItem(ItemType.Knife))
        {
            playerComponent.ModifyBloodpoints(1);
            Debug.Log("[PlayerItemManager] Messer-Bonus: +1 Blutpunkt");
        }

        // Pile of Ashes: Double bloodpoints gained
        if (HasItem(ItemType.PileOfAshes))
        {
            playerComponent.ModifyBloodpoints(amount);
            Debug.Log($"[PlayerItemManager] Haufen Asche-Bonus: +{amount} Blutpunkte (verdoppelt)");
        }
    }

    /// <summary>
    /// Called when player loses health (for item effects)
    /// </summary>
    public void OnHealthLost(int amount)
    {
        if (playerComponent == null) return;

        // Old Bread: Get 5 bloodpoints per health lost
        if (HasItem(ItemType.OldBread))
        {
            int bloodpointsGained = amount * 5;
            playerComponent.ModifyBloodpoints(bloodpointsGained);
            Debug.Log($"[PlayerItemManager] Altes Brot-Bonus: +{bloodpointsGained} Blutpunkte");
        }

        // Pile of Ashes: Lose all carried bloodpoints
        if (HasItem(ItemType.PileOfAshes))
        {
            int lostBloodpoints = playerComponent.GetBloodpoints();
            playerComponent.ModifyBloodpoints(-lostBloodpoints);
            Debug.Log($"[PlayerItemManager] Haufen Asche-Malus: -{lostBloodpoints} Blutpunkte verloren");
        }

        // Obsidian Shard: Auto-trigger on death
        if (playerComponent.GetHealth() <= 0 && HasItem(ItemType.ObsidianShard))
        {
            TriggerObsidianShard();
        }
    }

    /// <summary>
    /// Called when player gains food (for item effects)
    /// </summary>
    public void OnFoodGained(int amount)
    {
        if (playerComponent == null) return;

        // Note: EmergencyFood passive effect is handled in ApplyPassiveEffect
    }

    /// <summary>
    /// Triggers Obsidian Shard resurrection effect
    /// </summary>
    private void TriggerObsidianShard()
    {
        int carriedBloodpoints = playerComponent.GetBloodpoints();
        int healthRestored = carriedBloodpoints / 5;

        if (healthRestored > 0)
        {
            playerComponent.ModifyHealth(healthRestored);
            playerComponent.ModifyBloodpoints(-carriedBloodpoints);
            Debug.Log($"[PlayerItemManager] Obsidiansplitter aktiviert: +{healthRestored} Gesundheit, -{carriedBloodpoints} Blutpunkte");
        }

        DestroyItem(ItemType.ObsidianShard);
    }

    /// <summary>
    /// Called when item is destroyed (for BearClaw effect)
    /// </summary>
    public void OnItemDestroyedTrigger()
    {
        if (playerComponent == null) return;

        if (HasItem(ItemType.BearClaw))
        {
            playerComponent.ModifyBloodpoints(5);
            Debug.Log("[PlayerItemManager] Bärenkralle-Bonus: +5 Blutpunkte (Item zerstört)");
        }
    }

    /// <summary>
    /// Gets the display name for an item
    /// </summary>
    private string GetItemDisplayName(ItemType item)
    {
        switch (item)
        {
            case ItemType.Flashlight: return "Taschenlampe";
            case ItemType.BunnyStatue: return "Hasenstatue";
            case ItemType.Knife: return "Messer";
            case ItemType.JarOfNeedles: return "Nadeln im Glas";
            case ItemType.DriedDragonfly: return "Getrocknete Libelle";
            case ItemType.OldBread: return "Altes Brot";
            case ItemType.PileOfAshes: return "Haufen Asche";
            case ItemType.CrowFeather: return "Krähenfeder";
            case ItemType.BearClaw: return "Bärenkralle";
            case ItemType.EmergencyFood: return "Notrationen";
            case ItemType.ObsidianShard: return "Obsidiansplitter";
            case ItemType.ClimbingRope: return "Kletterseil";
            default: return item.ToString();
        }
    }

    /// <summary>
    /// Gets the description for an item
    /// </summary>
    private string GetItemDescription(ItemType item)
    {
        switch (item)
        {
            case ItemType.Flashlight:
                return "Zeige eine verdeckte Karte (1x pro 10 Züge)";
            case ItemType.BunnyStatue:
                return "Entkomme Wolfsangriffen oder verzehre für 10 Essen";
            case ItemType.Knife:
                return "Bessere Wolfangriffe, +1 Blutpunkt bei jedem Gewinn";
            case ItemType.JarOfNeedles:
                return "+1 Blutpunkt pro Waldkarte in Reihe bei Events";
            case ItemType.DriedDragonfly:
                return "+2 Blutpunkte pro Sumpfkarte in Spalte bei Events";
            case ItemType.OldBread:
                return "+5 Blutpunkte pro verlorenem Leben, verhindert Selbstheilung";
            case ItemType.PileOfAshes:
                return "Doppelte Blutpunkte, verliere alle bei Schaden";
            case ItemType.CrowFeather:
                return "Keine Geruchsspur mehr";
            case ItemType.BearClaw:
                return "+5 Blutpunkte pro zerstörtem Item";
            case ItemType.EmergencyFood:
                return "+10 maximales Essen";
            case ItemType.ObsidianShard:
                return "Verhindere Tod: +1 Leben pro 5 Blutpunkte";
            case ItemType.ClimbingRope:
                return "Betrete Felskarten für 4 Essen";
            default:
                return "";
        }
    }

    /// <summary>
    /// Called when close button is clicked
    /// </summary>
    private void OnCloseClicked()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        isPanelOpen = false;
        Debug.Log("[PlayerItemManager] Inventory UI closed");
    }

    /// <summary>
    /// Resets inventory to starting state (only flashlight)
    /// </summary>
    public void ResetItemStates()
    {
        playerInventory.Clear();
        flashlightCooldown = 0;
        AddItemToInventory(ItemType.Flashlight);
        Debug.Log("[PlayerItemManager] Inventory reset to starting state");
    }
}