using UnityEngine;

public class HareCard : ComplexEventCard
{
    public HareCard()
    {
        // Event details
        eventTitle = "Hasenbau";
        eventDescription = "Du stößt auf einen geschäftigen Hasenbau. Die kleinen Tiere bleiben aufrecht stehen und sehen dich aus Neugierigen, aber auch furchtsamen Augen an.";
        
        // CHOICE A: Attempt to catch a rabbit (uses minigame)
        choiceAText = "Fangen";
        choiceASuccessProbability = 0.3f; // Fallback if minigame not available
        // NOTE: outcomeASuccessText is set dynamically in OnChoiceASuccess()
        outcomeAFailureText = "Du versuchst, einen der Hasen zu fangen, doch als du dich ihnen näherst, ergreifen sie die Flucht und verschwinden in ihren Höhlen. Du gehst leer aus.";
        
        // Load minigame config for Choice A
        choiceAMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_Hares");
        if (choiceAMinigameConfig == null)
        {
            Debug.LogWarning("HareCard: Could not load MinigameConfig_Hares, will use random roll instead");
        }
        
        // CHOICE B: Don't pick (no minigame, always succeeds)
        choiceBText = "Gehen";
        choiceBSuccessProbability = 1f; // 100% success chance 
        outcomeBSuccessText = "Du entscheidest dich dazu, die Hasen am Leben zu lassen. Für's Erste.";
        outcomeBFailureText = "Wenn du das hier liest, hast du ganz großes Glück (oder Pech): Eigentlich sollte diese Auswahl zu 100% gelingen, aber du hast versagt. Na dann, weil die Geister des Waldes so beeindruckt von deiner Unfähigkeit sind, wirst du verschont.";
    }
    
    protected override void OnChoiceASuccess()
    {
        Debug.Log("Hare Card - Choice A Success: Attempting to give player Hare Statue");
        
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                // Check if player already has Bunny Statue
                if (itemManager.HasItem(ItemManager.ItemType.BunnyStatue))
                {
                    // Player already has statue - give bloodpoints instead
                    int bloodpointReward = 5; // You can adjust this value
                    itemManager.ModifyPlayerBloodpoints(bloodpointReward);
                    
                    // CRITICAL: Set the outcome text BEFORE the base class uses it
                    outcomeASuccessText = $"Du versuchst, einen der Hasen zu fangen. Deine Hände schnappen schnell zu - doch als das Tier stirbt, verwandelt es sich nicht in eine Statue. Du hast bereits eine Hasenstatue bei dir, und die dunkle Magie des Waldes scheint nicht zweimal zu wirken. Stattdessen fühlst du, wie deine Entschlossenheit wächst. (+{bloodpointReward} Blutpunkte)";
                    
                    Debug.Log($"Hare Card: Player already has Bunny Statue, gave {bloodpointReward} BP instead");
                }
                else
                {
                    // Player doesn't have statue yet - try to give it
                    bool added = itemManager.TryAddItem(ItemManager.ItemType.BunnyStatue);
                    
                    if (added)
                    {
                        Debug.Log("Hare Card: Successfully gave player Bunny Statue");
                        // Set success text for getting the statue
                        outcomeASuccessText = "Du versuchst, einen der Hasen zu fangen. Deine Hände schnappen schnell zu, und als das Tier in deinen Händen sein Leben lässt, bemerkst du, wie es sich in deiner Hand zu seiner hölzernen Statue verwandelt. (Du erhältst \"Hasenstatue\")";
                    }
                    else
                    {
                        // Inventory full - give bloodpoints as consolation
                        int bloodpointReward = 5;
                        itemManager.ModifyPlayerBloodpoints(bloodpointReward);
                        outcomeASuccessText = $"Du versuchst, einen der Hasen zu fangen und erwischst einen - doch dein Inventar ist voll! Die Hasenstatue zerfällt zu Staub, aber du spürst einen Energieschub. (+{bloodpointReward} Blutpunkte)";
                        Debug.Log($"Hare Card: Inventory full, gave {bloodpointReward} BP instead");
                    }
                }
            }
            else
            {
                Debug.LogError("Hare Card: ItemManager not found!");
                outcomeASuccessText = "Du fängst einen Hasen, aber etwas geht schief...";
            }
        }
        else
        {
            Debug.LogError("Hare Card: Player reference is null!");
            outcomeASuccessText = "Du fängst einen Hasen, aber etwas geht schief...";
        }
    }
    
    protected override void OnChoiceAFailure()
    {
        Debug.Log("Hare Card - Choice A Failure: Hares escaped");
        // No changes needed here - outcomeAFailureText is already set in constructor
    }
    
    protected override void OnChoiceBSuccess()
    {
        Debug.Log("Hare Card - Choice B Success: Player leaves Hares alone");
        // Player leaves hares alone - event stays open so they can try again
    }
    
    protected override void OnChoiceBFailure()
    {
        Debug.Log("Hare Card - Choice B Failure: Player is really lucky");
        // This shouldn't happen
    }
    
    /// <summary>
    /// Choice A (Fangen) always closes the event, regardless of success or failure
    /// Once hares are caught or scared away, they're gone
    /// </summary>
    protected override void HandleChoiceAEventClosure(bool wasSuccess)
    {
        CloseEvent(); // Hares were caught/scared - event is now closed
        Debug.Log("Hare Card: Catch attempt made, event closed");
    }

    /// <summary>
    /// Choice B (Gehen) does NOT close the event
    /// Player can come back and try again later
    /// </summary>
    protected override void HandleChoiceBEventClosure(bool wasSuccess)
    {
        // Don't close the event - player can return and catch hares later
        Debug.Log("Hare Card: Player didn't attempt to catch hares, event remains open");
    }
}

