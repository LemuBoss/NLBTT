using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the player's inventory and item interactions
/// Handles UI display and item usage
/// </summary>
public class ItemManager : MonoBehaviour
{
    public enum Item
    {
        Flashlight,      // Taschenlampe
        BunnyStatue,     // Hasenstatue
        Knife,           // Messer
        JarOfNeedles,    // Nadeln im Glas
        DriedDragonfly,  // Getrocknete Libelle
        OldBread,        // Altes Brot
        PileOfAshes,     // Haufen Asche
        CrowFeather,     // Krähenfeder
        BearClaw,        // Bärenkralle
        EmergencyFood,   // Notrationen
        ObsidianShard    // Obsidiansplitter
    }

    [Header("Inventory Settings")]
    [SerializeField] private int maxItems = 3;
    private List<Item> inventory = new List<Item>();

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

    private Player player;
    private bool isPanelOpen = false;

    private void Awake()
    {
        player = GetComponent<Player>();

        if (player == null)
        {
            Debug.LogError("[ItemManager] Player component not found!");
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
        AddItem(Item.Flashlight);
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

        // Update flashlight cooldown
        if (flashlightCooldown > 0)
        {
            // Cooldown decreases each turn (when player moves)
            // This will be called externally by Player after movement
        }
    }

    /// <summary>
    /// Toggles the inventory UI panel
    /// </summary>
    public void ToggleInventoryUI()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[ItemManager] Inventory panel not assigned!");
            LogInventoryToConsole();
            return;
        }

        isPanelOpen = !isPanelOpen;
        inventoryPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            UpdateInventoryUI();
        }

        Debug.Log($"[ItemManager] Inventory UI toggled: {(isPanelOpen ? "Open" : "Closed")}");
    }

    /// <summary>
    /// Updates the inventory UI text with current items
    /// </summary>
    private void UpdateInventoryUI()
    {
        if (inventoryListText == null)
            return;

        string inventoryText = $"Items ({inventory.Count}/{maxItems}):\n\n";

        if (inventory.Count == 0)
        {
            inventoryText += "Keine Items vorhanden.";
        }
        else
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                inventoryText += $"{i + 1}. {GetItemDisplayName(inventory[i])}\n";
                inventoryText += $"   {GetItemDescription(inventory[i])}\n\n";
            }
        }

        if (HasItem(Item.Flashlight))
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
        Debug.Log($"[ItemManager] === INVENTAR ({inventory.Count}/{maxItems}) ===");

        if (inventory.Count == 0)
        {
            Debug.Log("[ItemManager] Keine Items vorhanden.");
        }
        else
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                Debug.Log($"[ItemManager] {i + 1}. {GetItemDisplayName(inventory[i])}");
            }
        }

        if (HasItem(Item.Flashlight))
        {
            Debug.Log($"[ItemManager] Taschenlampe Cooldown: {flashlightCooldown} Züge");
        }
    }

    /// <summary>
    /// Adds an item to the inventory if there's space
    /// </summary>
    public bool AddItem(Item item)
    {
        if (inventory.Count >= maxItems)
        {
            Debug.Log("[ItemManager] Inventar voll! Kann kein weiteres Item aufnehmen.");
            return false;
        }

        inventory.Add(item);
        Debug.Log($"[ItemManager] Item hinzugefügt: {GetItemDisplayName(item)}");

        // Apply passive effects
        ApplyPassiveEffect(item);

        LogInventoryToConsole();
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (inventory.Contains(item))
        {
            inventory.Remove(item);
            Debug.Log($"[ItemManager] Item entfernt: {GetItemDisplayName(item)}");

            // Remove passive effects
            RemovePassiveEffect(item);

            LogInventoryToConsole();
            return true;
        }

        Debug.Log($"[ItemManager] Item nicht im Inventar: {GetItemDisplayName(item)}");
        return false;
    }

    /// <summary>
    /// Destroys an item and triggers its destruction effect
    /// </summary>
    public void DestroyItem(Item item)
    {
        if (!inventory.Contains(item))
        {
            Debug.Log($"[ItemManager] Kann Item nicht zerstören: {GetItemDisplayName(item)} - nicht im Inventar");
            return;
        }

        Debug.Log($"[ItemManager] Zerstöre Item: {GetItemDisplayName(item)}");

        // Trigger destruction effect
        OnItemDestroyed(item);

        // Remove from inventory
        RemoveItem(item);
    }

    /// <summary>
    /// Checks if the player has a specific item
    /// </summary>
    public bool HasItem(Item item)
    {
        return inventory.Contains(item);
    }

    /// <summary>
    /// Gets the current number of items
    /// </summary>
    public int GetItemCount()
    {
        return inventory.Count;
    }

    /// <summary>
    /// Gets a copy of the inventory list
    /// </summary>
    public List<Item> GetInventory()
    {
        return new List<Item>(inventory);
    }

    /// <summary>
    /// Decrements flashlight cooldown (called after player movement)
    /// </summary>
    public void DecrementFlashlightCooldown()
    {
        if (flashlightCooldown > 0)
        {
            flashlightCooldown--;
            Debug.Log($"[ItemManager] Taschenlampe Cooldown: {flashlightCooldown}");
        }
    }

    /// <summary>
    /// Applies passive effects when item is added
    /// </summary>
    private void ApplyPassiveEffect(Item item)
    {
        if (player == null) return;

        switch (item)
        {
            case Item.CrowFeather:
                // Increase stamina cap by 2
                player.modifyStamina(2);
                Debug.Log("[ItemManager] Krähenfeder: Ausdauer um 2 erhöht");
                break;
        }
    }

    /// <summary>
    /// Removes passive effects when item is removed
    /// </summary>
    private void RemovePassiveEffect(Item item)
    {
        if (player == null) return;

        switch (item)
        {
            case Item.CrowFeather:
                // Decrease stamina (but not below 0)
                player.modifyStamina(-2);
                Debug.Log("[ItemManager] Krähenfeder entfernt: Ausdauer um 2 verringert");
                break;
        }
    }

    /// <summary>
    /// Handles item destruction effects
    /// </summary>
    private void OnItemDestroyed(Item item)
    {
        if (player == null) return;

        switch (item)
        {
            case Item.BunnyStatue:
                // Can be eaten for 10 food
                player.modifyHunger(10);
                Debug.Log("[ItemManager] Hasenstatue verzehrt: +10 Essen");
                break;

            case Item.OldBread:
                // Get 10 food and 3 health, lose half carried bloodpoints
                player.modifyHunger(10);
                player.modifyHealth(3);
                int halfBloodpoints = player.GetBloodpoints() / 2;
                player.modifyBloodpoints(-halfBloodpoints);
                Debug.Log($"[ItemManager] Altes Brot zerstört: +10 Essen, +3 Gesundheit, -{halfBloodpoints} Blutpunkte");
                break;

            case Item.PileOfAshes:
                // Lose 1 health
                player.modifyHealth(-1);
                Debug.Log("[ItemManager] Haufen Asche zerstört: -1 Gesundheit");
                break;

            case Item.CrowFeather:
                // Get 3 bloodpoints
                player.modifyBloodpoints(3);
                Debug.Log("[ItemManager] Krähenfeder zerstört: +3 Blutpunkte");
                break;

            case Item.BearClaw:
                // Get 5 bloodpoints (including for destroying itself)
                player.modifyBloodpoints(5);
                Debug.Log("[ItemManager] Bärenkralle zerstört: +5 Blutpunkte");
                break;

            case Item.EmergencyFood:
                // Get 10 food
                player.modifyHunger(10);
                Debug.Log("[ItemManager] Notrationen zerstört: +10 Essen");
                break;
        }
    }

    /// <summary>
    /// Called when player gains bloodpoints (for item effects)
    /// </summary>
    public void OnBloodpointsGained(int amount)
    {
        if (player == null) return;

        // Knife: Get bonus bloodpoint
        if (HasItem(Item.Knife))
        {
            player.modifyBloodpoints(1);
            Debug.Log("[ItemManager] Messer-Bonus: +1 Blutpunkt");
        }

        // Pile of Ashes: Double bloodpoints gained
        if (HasItem(Item.PileOfAshes))
        {
            player.modifyBloodpoints(amount);
            Debug.Log($"[ItemManager] Haufen Asche-Bonus: +{amount} Blutpunkte (verdoppelt)");
        }
    }

    /// <summary>
    /// Called when player loses health (for item effects)
    /// </summary>
    public void OnHealthLost(int amount)
    {
        if (player == null) return;

        // Old Bread: Get 5 bloodpoints per health lost
        if (HasItem(Item.OldBread))
        {
            int bloodpointsGained = amount * 5;
            player.modifyBloodpoints(bloodpointsGained);
            Debug.Log($"[ItemManager] Altes Brot-Bonus: +{bloodpointsGained} Blutpunkte");
        }

        // Pile of Ashes: Lose all carried bloodpoints
        if (HasItem(Item.PileOfAshes))
        {
            int lostBloodpoints = player.GetBloodpoints();
            player.modifyBloodpoints(-lostBloodpoints);
            Debug.Log($"[ItemManager] Haufen Asche-Malus: -{lostBloodpoints} Blutpunkte verloren");
        }

        // Obsidian Shard: Auto-trigger on death
        if (player.GetHealth() <= 0 && HasItem(Item.ObsidianShard))
        {
            TriggerObsidianShard();
        }
    }

    /// <summary>
    /// Called when player gains food (for item effects)
    /// </summary>
    public void OnFoodGained(int amount)
    {
        if (player == null) return;

        // Emergency Food: Get 3 bloodpoints
        if (HasItem(Item.EmergencyFood))
        {
            player.modifyBloodpoints(3);
            Debug.Log("[ItemManager] Notrationen-Bonus: +3 Blutpunkte");
        }
    }

    /// <summary>
    /// Triggers Obsidian Shard resurrection effect
    /// </summary>
    private void TriggerObsidianShard()
    {
        int carriedBloodpoints = player.GetBloodpoints();
        int healthRestored = carriedBloodpoints / 5;

        if (healthRestored > 0)
        {
            player.modifyHealth(healthRestored);
            player.modifyBloodpoints(-carriedBloodpoints);
            Debug.Log($"[ItemManager] Obsidiansplitter aktiviert: +{healthRestored} Gesundheit, -{carriedBloodpoints} Blutpunkte");
        }

        DestroyItem(Item.ObsidianShard);
    }

    /// <summary>
    /// Called when item is destroyed (for BearClaw effect)
    /// </summary>
    public void OnItemDestroyedTrigger()
    {
        if (player == null) return;

        if (HasItem(Item.BearClaw))
        {
            player.modifyBloodpoints(5);
            Debug.Log("[ItemManager] Bärenkralle-Bonus: +5 Blutpunkte (Item zerstört)");
        }
    }

    /// <summary>
    /// Gets the display name for an item
    /// </summary>
    private string GetItemDisplayName(Item item)
    {
        switch (item)
        {
            case Item.Flashlight: return "Taschenlampe";
            case Item.BunnyStatue: return "Hasenstatue";
            case Item.Knife: return "Messer";
            case Item.JarOfNeedles: return "Nadeln im Glas";
            case Item.DriedDragonfly: return "Getrocknete Libelle";
            case Item.OldBread: return "Altes Brot";
            case Item.PileOfAshes: return "Haufen Asche";
            case Item.CrowFeather: return "Krähenfeder";
            case Item.BearClaw: return "Bärenkralle";
            case Item.EmergencyFood: return "Notrationen";
            case Item.ObsidianShard: return "Obsidiansplitter";
            default: return item.ToString();
        }
    }

    /// <summary>
    /// Gets the description for an item
    /// </summary>
    private string GetItemDescription(Item item)
    {
        switch (item)
        {
            case Item.Flashlight:
                return "Zeige eine verdeckte Karte (1x pro 10 Züge)";
            case Item.BunnyStatue:
                return "Entkomme Wolfsangriffen oder verzehre für 10 Essen";
            case Item.Knife:
                return "Bessere Wolfangriffe, +1 Blutpunkt bei jedem Gewinn";
            case Item.JarOfNeedles:
                return "+1 Blutpunkt pro Waldkarte in Reihe bei Events";
            case Item.DriedDragonfly:
                return "+2 Blutpunkte pro Sumpfkarte in Spalte bei Events";
            case Item.OldBread:
                return "+5 Blutpunkte pro verlorenem Leben";
            case Item.PileOfAshes:
                return "Doppelte Blutpunkte, verliere alle bei Schaden";
            case Item.CrowFeather:
                return "+2 maximale Ausdauer";
            case Item.BearClaw:
                return "+5 Blutpunkte pro zerstörtem Item";
            case Item.EmergencyFood:
                return "+3 Blutpunkte bei Essensgewinn";
            case Item.ObsidianShard:
                return "Verhindere Tod: +1 Leben pro 5 Blutpunkte";
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
        Debug.Log("[ItemManager] Inventory UI closed");
    }

    /// <summary>
    /// Resets inventory to starting state (only flashlight)
    /// </summary>
    public void ResetInventory()
    {
        inventory.Clear();
        flashlightCooldown = 0;
        AddItem(Item.Flashlight);
        Debug.Log("[ItemManager] Inventory reset to starting state");
    }
}