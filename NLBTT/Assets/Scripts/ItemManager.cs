using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Zentrales Item-Management System
/// Fängt ALLE Modifikationen ab und wendet Item-Effekte an
/// </summary>
public class ItemManager : MonoBehaviour
{
    public enum ItemType
    {
        Flashlight,         // Taschenlampe
        HuntingKnife,       // Jagdmesser
        JarOfNeedles,       // Nadeln im Glas
        DriedDragonfly,     // Getrocknete Libelle
        OldBread,           // Altes Brot
        PileOfAshes,        // Haufen Asche
        BearClaw,           // Bärenkralle
        EmergencyRations,   // Notrationen
        ObsidianShard,      // Obsidianscherbe
        BunnyStatue,         // Hasenstatue 
        ClimbingRope        // Kletterseil
    }

    [Header("Inventory Settings")]
    [SerializeField] private int maxItems = 3;
    
    // Internal state
    private List<ItemType> inventory = new List<ItemType>();
    private bool obsidianShardUsed = false; // Tracks if Obsidian Shard has been used (triggers 50% BP penalty)
    
    // References (automatically found)
    private Player player;
    private BoardManager boardManager;
    private WolfAI wolfAI;
    
    // Debug input state
    private bool isInputMode = false;
    private string currentInput = "";

    private void Awake()
    {
        player = GetComponent<Player>();
        boardManager = Object.FindFirstObjectByType<BoardManager>();
        wolfAI = Object.FindFirstObjectByType<WolfAI>();

        if (player == null)
            Debug.LogError("[ItemManager] Player component not found!");
        if (boardManager == null)
            Debug.LogError("[ItemManager] BoardManager not found!");
        if (wolfAI == null)
            Debug.LogWarning("[ItemManager] WolfAI not found!");
    }

    private void Update()
    {
        HandleDebugInput();
    }

    #region Debug Input System
    private void HandleDebugInput()
    {
        // 'I' key: Print inventory to console
        if (Input.GetKeyDown(KeyCode.I))
        {
            PrintInventoryToConsole();
        }

        // 'O' key: Activate input mode
        if (Input.GetKeyDown(KeyCode.O) && !isInputMode)
        {
            isInputMode = true;
            currentInput = "";
            Debug.Log("[ItemManager] === ITEM INPUT MODE ACTIVATED ===");
            Debug.Log("[ItemManager] Type item name and press ENTER:");
            Debug.Log("[ItemManager] Available items: Flashlight, HuntingKnife, JarOfNeedles, DriedDragonfly, OldBread, PileOfAshes, BearClaw, EmergencyRations, ObsidianShard");
        }

        // Handle input mode
        if (isInputMode)
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\b') // Backspace
                {
                    if (currentInput.Length > 0)
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                }
                else if (c == '\n' || c == '\r') // Enter
                {
                    ProcessItemInput(currentInput);
                    currentInput = "";
                    isInputMode = false;
                }
                else
                {
                    currentInput += c;
                }
            }

            if (currentInput.Length > 0)
                Debug.Log($"[ItemManager] Current input: {currentInput}");
        }
    }

    private void ProcessItemInput(string input)
    {
        string trimmed = input.Trim();
        
        if (System.Enum.TryParse(trimmed, true, out ItemType itemType))
        {
            if (TryAddItem(itemType))
            {
                Debug.Log($"[ItemManager] ✓ Successfully added: {GetItemDisplayName(itemType)}");
            }
        }
        else
        {
            Debug.Log($"[ItemManager] ✗ Invalid item name: '{trimmed}'");
        }
    }

    private void PrintInventoryToConsole()
    {
        Debug.Log($"[ItemManager] === INVENTORY ({inventory.Count}/{maxItems}) ===");
        
        if (inventory.Count == 0)
        {
            Debug.Log("[ItemManager] No items in inventory.");
        }
        else
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                Debug.Log($"[ItemManager] {i + 1}. {GetItemDisplayName(inventory[i])}");
            }
        }
    }
    #endregion

    #region Inventory Management
    /// <summary>
    /// Versucht ein Item hinzuzufügen. Gibt true zurück bei Erfolg.
    /// </summary>
    public bool TryAddItem(ItemType item)
    {
        // Check capacity
        if (inventory.Count >= maxItems)
        {
            Debug.Log($"[ItemManager] Inventory full! Cannot add {GetItemDisplayName(item)}");
            return false;
        }

        // Check for duplicates
        if (inventory.Contains(item))
        {
            Debug.Log($"[ItemManager] Already have {GetItemDisplayName(item)}! Cannot carry duplicates.");
            return false;
        }

        // Add item
        inventory.Add(item);
        Debug.Log($"[ItemManager] Added item: {GetItemDisplayName(item)}");

        // Apply passive effects
        ApplyPassiveItemEffect(item);

        return true;
    }

    /// <summary>
    /// Zerstört ein Item und triggert dessen Zerstörungs-Effekt
    /// </summary>
    public void DestroyItem(ItemType item)
    {
        if (!inventory.Contains(item))
        {
            Debug.Log($"[ItemManager] Cannot destroy {GetItemDisplayName(item)} - not in inventory");
            return;
        }

        Debug.Log($"[ItemManager] Destroying item: {GetItemDisplayName(item)}");

        // WICHTIG: Erst Zerstörungseffekt, DANN aus Inventar entfernen
        // (damit Bärenkralle ihren eigenen Effekt noch triggern kann)
        TriggerDestructionEffect(item);
        
        // Remove passive effects
        RemovePassiveItemEffect(item);

        // Remove from inventory
        inventory.Remove(item);
    }

    public bool HasItem(ItemType item) => inventory.Contains(item);
    
    public List<ItemType> GetInventory() => new List<ItemType>(inventory);
    
    public int GetItemCount() => inventory.Count;
    #endregion

    #region Passive Item Effects
    private void ApplyPassiveItemEffect(ItemType item)
    {
        switch (item)
        {
            case ItemType.EmergencyRations:
                // Erhöhe Nahrungskapazität um 10
                if (player != null)
                {
                    player.ModifyHungerCap(10);
                    Debug.Log("[ItemManager] Emergency Rations: Hunger capacity +10");
                }
                break;

            case ItemType.OldBread:
                // Deaktiviere Selbstheilung
                if (player != null)
                {
                    player.SetCanHealSelf(false);
                    Debug.Log("[ItemManager] Old Bread: Self-healing disabled");
                }
                break;
        }
    }

    private void RemovePassiveItemEffect(ItemType item)
    {
        switch (item)
        {
            case ItemType.EmergencyRations:
                // Reduziere Nahrungskapazität um 10
                if (player != null)
                {
                    player.ModifyHungerCap(-10);
                    Debug.Log("[ItemManager] Emergency Rations removed: Hunger capacity -10");
                }
                break;

            case ItemType.OldBread:
                // Aktiviere Selbstheilung wieder
                if (player != null)
                {
                    player.SetCanHealSelf(true);
                    Debug.Log("[ItemManager] Old Bread removed: Self-healing re-enabled");
                }
                break;
        }
    }
    #endregion

    #region Destruction Effects
    private void TriggerDestructionEffect(ItemType item)
    {
        if (player == null) return;

        switch (item)
        {
            case ItemType.JarOfNeedles:
                // Verliere 1 BP pro Waldkarte in derselben Reihe
                int forestPenalty = CountCardsInRow(player.GetPosition().y, typeof(ForestCard));
                ModifyPlayerBloodpoints(-forestPenalty);
                Debug.Log($"[ItemManager] Jar of Needles destroyed: Lost {forestPenalty} BP (forest cards in row)");
                break;

            case ItemType.DriedDragonfly:
                // Verliere 1 BP pro Sumpfkarte in derselben Spalte
                int swampPenalty = CountCardsInColumn(player.GetPosition().x, typeof(SwampCard));
                ModifyPlayerBloodpoints(-swampPenalty);
                Debug.Log($"[ItemManager] Dried Dragonfly destroyed: Lost {swampPenalty} BP (swamp cards in column)");
                break;

            case ItemType.PileOfAshes:
                // Verliere 2 Gesundheit, aber stirb nicht dadurch
                int healthBefore = player.GetHealth();
                ModifyPlayerHealth(-2);
                
                // Stelle sicher, dass Gesundheit nicht unter 1 fällt
                if (player.GetHealth() <= 0)
                {
                    player.SetHealth(1);
                    Debug.Log("[ItemManager] Pile of Ashes: Health set to 1 (cannot kill player)");
                }
                
                // WICHTIG: Dieser Effekt triggert den normalen "Gesundheitsverlust"-Effekt der Asche
                // (alle BP verlieren), aber nur wenn man tatsächlich HP verloren hat
                break;

            case ItemType.ObsidianShard:
                // Manuelles Zerstören der Scherbe gibt KEINEN BP-Malus
                Debug.Log("[ItemManager] Obsidian Shard manually destroyed: No BP penalty applied");
                break;

            case ItemType.EmergencyRations:
                // Erhalte 10 Nahrung (Kapazität wird vorher reduziert, Overflow geht verloren)
                ModifyPlayerHunger(10);
                Debug.Log("[ItemManager] Emergency Rations destroyed: Gained 10 food");
                break;

            case ItemType.BunnyStatue:
                // Kann als Nahrung verzehrt werden: +10 Hunger
                ModifyPlayerHunger(10);
                Debug.Log("[ItemManager] Bunny Statue destroyed: Gained 10 food (eaten)");
                break;

            case ItemType.BearClaw:
                // 70% Chance auf 10 BP, 30% Chance auf 1 HP Verlust
                float roll = Random.value;
                if (roll < 0.7f)
                {
                    ModifyPlayerBloodpoints(10);
                    Debug.Log("[ItemManager] Bear Claw destroyed: Gained 10 BP (70% chance)");
                }
                else
                {
                    ModifyPlayerHealth(-1);
                    Debug.Log("[ItemManager] Bear Claw destroyed: Lost 1 HP (30% chance)");
                }
                break;

            // Items ohne Zerstörungseffekt
            case ItemType.Flashlight:
            case ItemType.HuntingKnife:
                Debug.Log($"[ItemManager] {GetItemDisplayName(item)} destroyed: No destruction effect");
                break;
        }
    }
    #endregion

    #region Modification Pipeline (ZENTRALE METHODEN)
    
    /// <summary>
    /// ZENTRALE METHODE: Modifiziert Spieler-Gesundheit mit Item-Effekten
    /// ALLE Karten/Events müssen diese Methode verwenden!
    /// </summary>
    public void ModifyPlayerHealth(int baseAmount)
    {
        if (player == null) return;

        int finalAmount = baseAmount;

        Debug.Log($"[ItemManager] === HEALTH MODIFICATION ===");
        Debug.Log($"[ItemManager] Base amount: {baseAmount}");

        // Bei Gesundheitsverlust: Item-Effekte anwenden
        if (baseAmount < 0)
        {
            int lostHealth = Mathf.Abs(baseAmount);

            // Altes Brot: +5 BP pro verlorenem HP
            if (HasItem(ItemType.OldBread))
            {
                int bloodpointGain = lostHealth * 5;
                ModifyPlayerBloodpoints(bloodpointGain);
                Debug.Log($"[ItemManager] Old Bread: Gained {bloodpointGain} BP from losing {lostHealth} HP");
            }

            // Haufen Asche: Verliere ALLE Blutpunkte (wird als VORLETZTES angewendet)
            // Dies passiert NACH allen Blutpunkt-Gewinnen, aber VOR Obsidianscherbe
            if (HasItem(ItemType.PileOfAshes))
            {
                int lostBloodpoints = player.GetBloodpoints();
                player.SetBloodpoints(0);
                Debug.Log($"[ItemManager] Pile of Ashes: Lost all {lostBloodpoints} BP due to health loss");
            }
        }

        // Wende finalen Betrag an
        player.ModifyHealth(finalAmount);
        Debug.Log($"[ItemManager] Final health change: {finalAmount}");

        // NACH dem Gesundheitsverlust: Check für Obsidianscherbe (LETZTER Effekt)
        if (player.GetHealth() <= 0 && HasItem(ItemType.ObsidianShard))
        {
            Debug.Log("[ItemManager] Player died! Triggering Obsidian Shard...");
            TriggerObsidianShardRevive();
        }
    }

    /// <summary>
    /// ZENTRALE METHODE: Modifiziert Spieler-Blutpunkte mit Item-Effekten
    /// ALLE Karten/Events müssen diese Methode verwenden!
    /// </summary>
    public void ModifyPlayerBloodpoints(int baseAmount)
    {
        if (player == null) return;

        int finalAmount = baseAmount;

        Debug.Log($"[ItemManager] === BLOODPOINT MODIFICATION ===");
        Debug.Log($"[ItemManager] Base amount: {baseAmount}");

        // NUR bei Blutpunkt-GEWINN: Item-Effekte anwenden
        if (baseAmount > 0)
        {
            Vector2Int playerPos = player.GetPosition();

            // Nadeln im Glas: +1 BP pro Waldkarte in derselben Reihe
            if (HasItem(ItemType.JarOfNeedles))
            {
                int forestBonus = CountCardsInRow(playerPos.y, typeof(ForestCard));
                finalAmount += forestBonus;
                Debug.Log($"[ItemManager] Jar of Needles: +{forestBonus} BP (forest cards in row)");
            }

            // Getrocknete Libelle: +1 BP pro Sumpfkarte in derselben Spalte
            if (HasItem(ItemType.DriedDragonfly))
            {
                int swampBonus = CountCardsInColumn(playerPos.x, typeof(SwampCard));
                finalAmount += swampBonus;
                Debug.Log($"[ItemManager] Dried Dragonfly: +{swampBonus} BP (swamp cards in column)");
            }

            // Haufen Asche: Verdopple BP-Gewinn (wird als VORLETZTES angewendet)
            if (HasItem(ItemType.PileOfAshes))
            {
                finalAmount *= 2;
                Debug.Log($"[ItemManager] Pile of Ashes: Doubled BP gain (×2)");
            }

            // Obsidianscherbe: Halbiere BP-Gewinn, runde auf (wird als LETZTES angewendet)
            // WICHTIG: Nur wenn die Scherbe bereits VERBRAUCHT wurde (durch einen Tod)
            if (HasItem(ItemType.ObsidianShard) && obsidianShardUsed)
            {
                finalAmount = Mathf.CeilToInt(finalAmount / 2f);
                Debug.Log($"[ItemManager] Obsidian Shard (USED): Halved BP gain (÷2, rounded up)");
            }
            else if (HasItem(ItemType.ObsidianShard) && !obsidianShardUsed)
            {
                Debug.Log($"[ItemManager] Obsidian Shard (INTACT): No BP penalty (100% BP gain)");
            }
        }

        // Wende finalen Betrag an
        player.ModifyBloodpoints(finalAmount);
        Debug.Log($"[ItemManager] Final BP change: {finalAmount} (Total: {player.GetBloodpoints()})");
    }

    /// <summary>
    /// ZENTRALE METHODE: Modifiziert Spieler-Nahrung
    /// ALLE Karten/Events müssen diese Methode verwenden!
    /// </summary>
    public void ModifyPlayerHunger(int amount)
    {
        if (player == null) return;

        Debug.Log($"[ItemManager] === HUNGER MODIFICATION ===");
        Debug.Log($"[ItemManager] Amount: {amount}");

        player.ModifyHunger(amount);
        Debug.Log($"[ItemManager] Final hunger: {player.GetHunger()}/{player.GetHungerCap()}");
    }

    /// <summary>
    /// ZENTRALE METHODE: Modifiziert Spieler-Ausdauer
    /// DEPRECATED: Stamina system has been removed from the game
    /// </summary>
    public void ModifyPlayerStamina(int amount)
    {
        // Stamina system removed - method kept for backward compatibility
        Debug.LogWarning("[ItemManager] ModifyPlayerStamina called but stamina system is deprecated");
    }

    #endregion

    #region Special Item Mechanics

    /// <summary>
    /// Obsidianscherbe: Revive-Mechanik
    /// Füllt gesamte Gesundheit auf und zerstört sich selbst
    /// WICHTIG: Aktiviert den 50% BP-Malus für den Rest der Runde
    /// </summary>
    private void TriggerObsidianShardRevive()
    {
        if (player == null) return;

        // Stelle Gesundheit komplett wieder her
        int maxHealth = 5; // TODO: Wenn du später dynamische Max-Health hast, hier anpassen
        player.SetHealth(maxHealth);
        
        Debug.Log($"[ItemManager] Obsidian Shard: Revived player to full health ({maxHealth} HP)");

        // WICHTIG: Markiere Scherbe als verbraucht (50% BP-Malus aktiviert sich)
        obsidianShardUsed = true;
        Debug.Log("[ItemManager] Obsidian Shard: Marked as USED - 50% BP penalty now active for rest of round");

        // Zerstöre Scherbe (OHNE ihren Zerstörungseffekt zu triggern, da sie keinen hat)
        inventory.Remove(ItemType.ObsidianShard);
        Debug.Log("[ItemManager] Obsidian Shard: Destroyed itself");
    }

    /// <summary>
    /// Wird aufgerufen wenn ein Item zerstört wird (für Bärenkralle)
    /// </summary>
    public void OnAnyItemDestroyed()
    {
        if (!HasItem(ItemType.BearClaw)) return;

        // 70% Chance auf 10 BP, 30% Chance auf 1 HP Verlust
        float roll = Random.value;
        if (roll < 0.7f)
        {
            ModifyPlayerBloodpoints(10);
            Debug.Log("[ItemManager] Bear Claw: Gained 10 BP from item destruction (70% chance)");
        }
        else
        {
            ModifyPlayerHealth(-1);
            Debug.Log("[ItemManager] Bear Claw: Lost 1 HP from item destruction (30% chance)");
        }
    }

    #endregion

    #region Minigame Modifiers

    /// <summary>
    /// Erstellt Minigame-Modifiers basierend auf Items
    /// </summary>
    public MinigameModifiers GetMinigameModifiers()
    {
        MinigameModifiers mods = new MinigameModifiers();

        // Jagdmesser: Alle Minigames werden erleichtert
        if (HasItem(ItemType.HuntingKnife))
        {
            mods.timerDrainMultiplier = 0.7f; // 30% langsamer
            Debug.Log("[ItemManager] Hunting Knife: Minigame timers drain 30% slower");
        }

        return mods;
    }

    #endregion

    #region Wolf Visibility

    /// <summary>
    /// Prüft ob Wölfe aufgrund von Items sichtbar sein sollen
    /// </summary>
    public bool ShouldWolvesBeAlwaysVisible()
    {
        return HasItem(ItemType.Flashlight);
    }

    /// <summary>
    /// Aktualisiert Wolf-Sichtbarkeit basierend auf Items
    /// Sollte aufgerufen werden nachdem Taschenlampe hinzugefügt/entfernt wurde
    /// </summary>
    public void UpdateWolfVisibility()
    {
        if (wolfAI == null) return;

        bool alwaysVisible = ShouldWolvesBeAlwaysVisible();

        if (alwaysVisible)
        {
            // Mache alle Wölfe sichtbar
            foreach (Wolf wolf in wolfAI.GetWolves())
            {
                wolf.SetVisible(true);
            }
            Debug.Log("[ItemManager] Flashlight: All wolves are now visible");
        }
        else
        {
            // Normale Sichtbarkeits-Regeln
            wolfAI.UpdateAllWolfVisibility();
        }
    }

    #endregion

    #region Helper Methods

    private int CountCardsInRow(int row, System.Type cardType)
    {
        if (boardManager == null) return 0;

        int count = 0;
        int gridWidth = boardManager.GetGridWidth();

        for (int x = 0; x < gridWidth; x++)
        {
            Card card = boardManager.GetCardAt(x, row);
            if (card != null && card.GetType() == cardType)
            {
                count++;
            }
        }

        return count;
    }

    private int CountCardsInColumn(int column, System.Type cardType)
    {
        if (boardManager == null) return 0;

        int count = 0;
        int gridHeight = boardManager.GetGridHeight();

        for (int y = 0; y < gridHeight; y++)
        {
            Card card = boardManager.GetCardAt(column, y);
            if (card != null && card.GetType() == cardType)
            {
                count++;
            }
        }

        return count;
    }

    private string GetItemDisplayName(ItemType item)
    {
        switch (item)
        {
            case ItemType.Flashlight: return "Taschenlampe";
            case ItemType.HuntingKnife: return "Jagdmesser";
            case ItemType.JarOfNeedles: return "Nadeln im Glas";
            case ItemType.DriedDragonfly: return "Getrocknete Libelle";
            case ItemType.OldBread: return "Altes Brot";
            case ItemType.PileOfAshes: return "Haufen Asche";
            case ItemType.BearClaw: return "Bärenkralle";
            case ItemType.EmergencyRations: return "Notrationen";
            case ItemType.BunnyStatue: return "Hasenstatue";
            case ItemType.ClimbingRope: return "Kletterseil";
            case ItemType.ObsidianShard: 
                string state = obsidianShardUsed ? " (VERBRAUCHT)" : " (INTAKT)";
                return "Obsidianscherbe" + state;
            default: return item.ToString();
        }
    }

    
    /// <summary>
    /// Resets item states when game/round restarts
    /// Call this from Player.ResetToStartingValues() or similar
    /// </summary>
    public void ResetItemStates()
    {
        // Remove all passive effects before clearing inventory
        foreach (ItemType item in new List<ItemType>(inventory))
        {
            RemovePassiveItemEffect(item);
        }
    
        // Clear the entire inventory
        inventory.Clear();
    
        // Reset obsidian shard state
        obsidianShardUsed = false;
    
        Debug.Log("[ItemManager] Item states reset - Inventory cleared, Obsidian Shard back to INTACT");
    }

    #endregion
}

