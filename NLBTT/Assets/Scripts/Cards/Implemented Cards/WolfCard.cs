using UnityEngine;

/// <summary>
/// Wolf encounter event with minigame integration
/// NEW ARCHITECTURE: No longer tracks individual wolves
/// Delegates wolf management to WolfAI
/// </summary>
public class WolfCard : ComplexEventCard
{
    public WolfCard()
    {
        // Event details
        eventTitle = "Wolfangriff!";
        eventDescription = "Augen blitzen dich aus dem Unterholz aus an. Ein knurrendes Biest schleicht aus den Schatten hervor, Zähne gebleckt. Es sieht genauso hungrig aus wie du.";
        
        // CHOICE A: Fight (uses hard minigame)
        choiceAText = "Kämpfen";
        choiceASuccessProbability = 0.3f;
        outcomeASuccessText = "Du stellst dich dem Wolf und kämpfst mit allem, was du hast. Deine Reflexe sind scharf, deine Bewegungen präzise. Du kehrst siegreich hervor. Der Wolf flieht verwundet in die Schatten. (+5 Blutpunkte)";
        outcomeAFailureText = "Du stellst dich dem Wolf, doch deine Reflexe sind zu langsam. Du überschätzt deine eigene Kraft und kommst nur knapp mit deinem Leben davon. (-2 Gesundheit)";
        
        choiceAMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_WolfFight");
        if (choiceAMinigameConfig == null)
        {
            Debug.LogWarning("WolfCard: Could not load MinigameConfig_WolfFight, will use random roll instead");
        }
        
        // CHOICE B: Flee (uses easier minigame)
        choiceBText = "Fliehen";
        choiceBSuccessProbability = 0.75f;
        outcomeBSuccessText = "Du nimmst die Flucht auf und navigierst geschickt durch das Unterholz. Der Wolf verliert deine Spur.";
        outcomeBFailureText = "Du nimmst die Flucht auf, stolperst aber über eine Wurzel, die aus dem Boden ragt. Der Wolf schnappt nach deinen Beinen, reißt an deiner Kleidung, doch du kannst gerade noch so wieder Fuß fassen. (-1 Gesundheit)";
        
        choiceBMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_WolfFlee");
        if (choiceBMinigameConfig == null)
        {
            Debug.LogWarning("WolfCard: Could not load MinigameConfig_WolfFlee, will use random roll instead");
        }
        
        CheckForBunnyStatue();
    }
    
    /// <summary>
    /// NEW: Override TriggerEvent to log wolf positions when event starts
    /// </summary>
    public override void TriggerEvent()
    {
        Debug.Log($"[WolfCard] ⚡ === WOLF EVENT TRIGGERED ===");
        
        // Log player position
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            Vector2Int playerPos = player.GetPosition();
            Debug.Log($"[WolfCard] 🎯 Player position at trigger: ({playerPos.x}, {playerPos.y})");
        }
        
        // Log all wolf positions
        WolfAI wolfAI = Object.FindFirstObjectByType<WolfAI>();
        if (wolfAI != null)
        {
            var allWolves = wolfAI.GetWolves();
            Debug.Log($"[WolfCard] 🐺 Total wolves at event trigger: {allWolves.Count}");
            
            for (int i = 0; i < allWolves.Count; i++)
            {
                Wolf wolf = allWolves[i];
                if (wolf != null)
                {
                    Vector2Int wolfPos = wolf.GetPosition();
                    bool isDespawned = wolf.IsDespawned();
                    bool isOnCooldown = wolf.IsOnCooldown();
                    string status = isDespawned ? "(DESPAWNED)" : isOnCooldown ? "(COOLDOWN)" : "(ACTIVE)";
                    
                    Debug.Log($"[WolfCard] 🐺 Wolf {i}: Position ({wolfPos.x}, {wolfPos.y}) {status}");
                }
            }
        }
        
        // Call base implementation to show UI
        base.TriggerEvent();
    }

    private void CheckForBunnyStatue()
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null && itemManager.HasItem(ItemManager.ItemType.BunnyStatue))
            {
                choiceCText = "Hasenstatue opfern";
                outcomeCText = "Du wirfst die Hasenstatue dem Wolf entgegen. Das Tier hält inne, schnuppert neugierig daran, bevor er sie aufnimmt und dir einen wissenden Blick zuwirft. Der Wolf kehrt dir den Rücken zu und verschwindet mit der Statue in der Dunkelheit.";
                
                Debug.Log("[WolfCard] Player has Bunny Statue - third choice available!");
            }
        }
    }

    protected override void OnChoiceASuccess()
    {
        Debug.Log("[WolfCard] Choice A Success: Wolf defeated, bloodpoints gained");
        
        // Find player reference if needed
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                Debug.Log($"[WolfCard] 🔍 ItemManager found! Adding 5 bloodpoints...");
                Debug.Log($"[WolfCard] 🔍 Bloodpoints BEFORE: {player.GetBloodpoints()}");
                itemManager.ModifyPlayerBloodpoints(5);
                Debug.Log($"[WolfCard] 🔍 Bloodpoints AFTER: {player.GetBloodpoints()}");
            }
            else
            {
                Debug.LogError("[WolfCard] ItemManager is null!");
            }
        }
        else
        {
            Debug.LogError("[WolfCard] Player reference is null!");
        }
        
        // NEW: Delegate wolf despawning to WolfAI
        DespawnWolfAtPlayerPosition();
    }

    protected override void OnChoiceAFailure()
    {
        Debug.Log("[WolfCard] Choice A Failure: Wolf attacks, player loses health");
        
        // Find player reference if needed
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                Debug.Log($"[WolfCard] 🔍 ItemManager found! Removing 2 health...");
                Debug.Log($"[WolfCard] 🔍 Health BEFORE: {player.GetHealth()}");
                itemManager.ModifyPlayerHealth(-2);
                Debug.Log($"[WolfCard] 🔍 Health AFTER: {player.GetHealth()}");
            }
            else
            {
                Debug.LogError("[WolfCard] ItemManager is null!");
            }
        }
        else
        {
            Debug.LogError("[WolfCard] Player reference is null!");
        }
        
        // Wolf stays active after failed fight
    }

    protected override void OnChoiceBSuccess()
    {
        Debug.Log("[WolfCard] Choice B Success: Successful Escape");
        
        // No special effect - player successfully escapes without consequence
        // Wolf remains active
    }

    protected override void OnChoiceBFailure()
    {
        Debug.Log("[WolfCard] Choice B Failure: Wolf catches up and hurts player");
        
        // Find player reference if needed
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.ModifyPlayerHealth(-1);
            }
        }
        else
        {
            Debug.LogError("[WolfCard] Player reference is null!");
        }
        
        // Wolf remains active after failed flee
    }

    protected override void OnChoiceC()
    {
        Debug.Log("[WolfCard] Choice C: Bunny Statue sacrificed for safe escape");
        
        // Find player reference if needed
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.DestroyItem(ItemManager.ItemType.BunnyStatue);
                Debug.Log("[WolfCard] Bunny Statue destroyed - player escapes safely");
            }
        }
        else
        {
            Debug.LogError("[WolfCard] Player reference is null!");
        }
        
        // Wolf remains active after bunny statue sacrifice
    }

    /// <summary>
    /// NEW METHOD: Asks WolfAI to despawn any wolf at the player's position
    /// WolfAI handles finding the correct wolf and despawning it
    /// </summary>
    private void DespawnWolfAtPlayerPosition()
    {
        WolfAI wolfAI = Object.FindFirstObjectByType<WolfAI>();
        
        if (wolfAI == null)
        {
            Debug.LogError("[WolfCard] WolfAI not found in scene!");
            return;
        }
        
        // Find player position
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
        
        if (player == null)
        {
            Debug.LogError("[WolfCard] Player not found!");
            return;
        }
        
        Vector2Int playerPosition = player.GetPosition();
        
        // DEBUG: Log all wolf positions BEFORE despawn attempt
        Debug.Log($"[WolfCard] 🐺 === WOLF POSITIONS BEFORE DESPAWN ATTEMPT ===");
        Debug.Log($"[WolfCard] 🎯 Player is at: ({playerPosition.x}, {playerPosition.y})");
        
        var allWolves = wolfAI.GetWolves();
        Debug.Log($"[WolfCard] 🐺 Total wolves in game: {allWolves.Count}");
        
        for (int i = 0; i < allWolves.Count; i++)
        {
            Wolf wolf = allWolves[i];
            if (wolf != null)
            {
                Vector2Int wolfPos = wolf.GetPosition();
                bool isDespawned = wolf.IsDespawned();
                bool isAtPlayerPos = (wolfPos == playerPosition);
                
                string marker = isAtPlayerPos ? "⭐ THIS ONE!" : "";
                string status = isDespawned ? "(DESPAWNED)" : "(ACTIVE)";
                
                Debug.Log($"[WolfCard] 🐺 Wolf {i}: Position ({wolfPos.x}, {wolfPos.y}) {status} {marker}");
            }
        }
        
        // Ask WolfAI to handle the despawn
        bool success = wolfAI.DespawnWolfAtPosition(playerPosition);
        
        if (success)
        {
            Debug.Log($"[WolfCard] ✓ Successfully despawned wolf at position ({playerPosition.x}, {playerPosition.y})");
        }
        else
        {
            Debug.LogWarning($"[WolfCard] ⚠ No wolf found at position ({playerPosition.x}, {playerPosition.y}) to despawn!");
        }
        
        // DEBUG: Log all wolf positions AFTER despawn attempt
        Debug.Log($"[WolfCard] 🐺 === WOLF POSITIONS AFTER DESPAWN ATTEMPT ===");
        for (int i = 0; i < allWolves.Count; i++)
        {
            Wolf wolf = allWolves[i];
            if (wolf != null)
            {
                Vector2Int wolfPos = wolf.GetPosition();
                bool isDespawned = wolf.IsDespawned();
                string status = isDespawned ? "(DESPAWNED)" : "(ACTIVE)";
                
                Debug.Log($"[WolfCard] 🐺 Wolf {i}: Position ({wolfPos.x}, {wolfPos.y}) {status}");
            }
        }
    }
}

