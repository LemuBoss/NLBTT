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
        outcomeASuccessText = "Du versuchst, einen der Hasen zu fangen. Deine Hände schnappen schnell zu, und als das Tier in deinen Händen sein Leben lässt, bemerkst du, wie es sich in deiner Hand zu seiner hölzernen Statue verwandelt. (Du erhältst \"Hastenstatue\")";
        outcomeAFailureText = "Du versuchst, einen der Hasen zu fangen, doch als du dich ihnen näherst, ergreifen sie die Flucht und verschwinden in ihren Höhlen. Du gehst leer aus.";
        
        // Load minigame config for Choice A
        choiceAMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_Hares");
        if (choiceAMinigameConfig == null)
        {
            Debug.LogWarning("BerryCard: Could not load MinigameConfig_BerryPicking, will use random roll instead");
        }
        
        // CHOICE B: Don't pick (no minigame, always succeeds)
        choiceBText = "Gehen";
        choiceBSuccessProbability = 1f; // 100% success chance 
        outcomeBSuccessText = "Du entscheidest dich dazu, die Hasen am Leben zu lassen. Für's Erste.";
        outcomeBFailureText = "Wenn du das hier liest, hast du ganz großes Glück (oder Pech): Eigentlich sollte diese Auswahl zu 100% gelingen, aber du hast versagt. Na dann, weil die Geister des Waldes so beeindruckt von deiner Unfähigkeit sind, wirst du verschont.";
    }
    
    protected override void OnChoiceASuccess()
    {
        Debug.Log("Hare Card - Choice A Success: Hare caught, gained Hare Statue");
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.TryAddItem(ItemManager.ItemType.BunnyStatue);
            }
        }
        else
        {
            Debug.LogError("Hare Card: Player reference is null!");
        }
    }
    
    protected override void OnChoiceAFailure()
    {

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
    /// Once hares are shooed away, they're gone
    /// </summary>
    protected override void HandleChoiceAEventClosure(bool wasSuccess)
    {
        CloseEvent(); // Hares were caught - event is now closed
        Debug.Log("Hare Card: Hares caught, event closed");
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