using UnityEngine;

/// <summary>
/// Wolf encounter event with minigame integration
/// Both choices use minigames
/// Fighting has a harder minigame than fleeing
/// NEW: Winning the fight despawns the wolf for 10 turns
/// NEW: If player has Bunny Statue, third option to sacrifice it for safe escape
/// </summary>
public class WolfCard : ComplexEventCard
{
    private Wolf wolfOnThisCard; // The wolf currently on this card (set by OnWolfEnter)

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
    /// Called by Wolf.cs when a wolf enters this card
    /// Stores reference to the wolf for later use
    /// </summary>
    public override void OnWolfEnter(Wolf wolf)
    {
        base.OnWolfEnter(wolf);
        wolfOnThisCard = wolf;
        Debug.Log($"[WolfCard] Wolf entered card, stored reference");
    }

    /// <summary>
    /// Called by Wolf.cs when a wolf leaves this card
    /// Clears the wolf reference
    /// </summary>
    public override void OnWolfExit(Wolf wolf)
    {
        base.OnWolfExit(wolf);
        if (wolfOnThisCard == wolf)
        {
            wolfOnThisCard = null;
            Debug.Log($"[WolfCard] Wolf left card, cleared reference");
        }
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
        Debug.Log("Wolf Card - Choice A Success: Wolf defeated, bloodpoints gained");
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.ModifyPlayerBloodpoints(5);
            }
        }
        else
        {
            Debug.LogError("WolfCard: Player reference is null!");
        }
        
        // NEW: Despawn the wolf after successful fight
        if (wolfOnThisCard != null)
        {
            wolfOnThisCard.Despawn();
            Debug.Log($"[WolfCard] 🐺💀 Wolf defeated and despawned! Will respawn in 10 turns.");
            wolfOnThisCard = null; // Clear reference since wolf is despawned
        }
        else
        {
            Debug.LogWarning("[WolfCard] ⚠ No wolf on this card to despawn!");
        }
    }

    protected override void OnChoiceAFailure()
    {
        Debug.Log("Wolf Card - Choice A Failure: Wolf attacks, player loses health");
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.ModifyPlayerHealth(-2);
            }
        }
        else
        {
            Debug.LogError("WolfCard: Player reference is null!");
        }
        
        // Wolf stays active after failed fight
    }

    protected override void OnChoiceBSuccess()
    {
        Debug.Log("Wolf Card - Choice B Success: Successful Escape");
        
        // No special effect - player successfully escapes without consequence
        // Wolf remains active
    }

    protected override void OnChoiceBFailure()
    {
        Debug.Log("Wolf Card - Choice B Failure: Wolf catches up and hurts player");
        
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
            Debug.LogError("WolfCard: Player reference is null!");
        }
        
        // Wolf remains active after failed flee
    }

    protected override void OnChoiceC()
    {
        Debug.Log("Wolf Card - Choice C: Bunny Statue sacrificed for safe escape");
        
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
            Debug.LogError("WolfCard: Player reference is null!");
        }
        
        // Wolf remains active after bunny statue sacrifice
    }
}

