using UnityEngine;

/// <summary>
/// Berry picking event with minigame integration
/// Choice A (Pflücken) uses a minigame instead of random roll
/// Choice B (Nicht pflücken) remains random (100% success)
/// </summary>
public class BerryCard : ComplexEventCard
{
    public BerryCard()
    {
        // Event details
        eventTitle = "Beerenbusch";
        eventDescription = "Du stößt auf einen Beerenbusch. Seine Beeren glänzen verlockend im wenigen Licht, das durch das Blätterdach fällt. Doch sie sehen ziemlich ähnlich zu giftigen Beeren aus...";
        
        // CHOICE A: Pick berries (uses minigame)
        choiceAText = "Pflücken";
        choiceASuccessProbability = 0.7f; // Fallback if minigame not available
        outcomeASuccessText = "Das Knurren in deinem Magen übertönt die Stimme der Vorsichtig in dir. Du pflückst sorgfältig die richtigen Beeren und verspeist sie ohne dich zu vergiften. (+15 Nahrung)";
        outcomeAFailureText = "Das Knurren in deinem Magen übertönt die Stimme der Vorsichtig in dir. Du pflückst hastig und verletzt dich an den Stacheln der Ranken. (-1 Gesundheit)";
        
        // Load minigame config for Choice A
        choiceAMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_BerryPicking");
        if (choiceAMinigameConfig == null)
        {
            Debug.LogWarning("BerryCard: Could not load MinigameConfig_BerryPicking, will use random roll instead");
        }
        
        // CHOICE B: Don't pick (no minigame, always succeeds)
        choiceBText = "Nicht pflücken";
        choiceBSuccessProbability = 1f; // 100% success chance 
        outcomeBSuccessText = "Du entscheidest dich dazu, auf deine Vernunft zu hören und die Beeren nicht zu pflücken. Vielleicht findest du unterwegs eine alternative Nahrungsquelle... Vielleicht.";
        outcomeBFailureText = "Du entscheidest dich dazu, auf deine Vernunft zu hören und die Beeren nicht zu pflücken, doch dein Hunger dreht mit dir durch. Du isst die Beeren und merkst, dass sie nicht von der giftigen Variante waren. (+10 Nahrung)";
    }

    protected override void OnChoiceASuccess()
    {
        Debug.Log("Berry Card - Choice A Success: Berries picked carefully, +15 food");
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.ModifyPlayerHunger(15);
            }
        }
        else
        {
            Debug.LogError("BerryCard: Player reference is null!");
        }
    }

    protected override void OnChoiceAFailure()
    {
        Debug.Log("Berry Card - Choice A Failure: Picked wrong berries, -1 health, -5 food");
        
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
            Debug.LogError("BerryCard: Player reference is null!");
        }
    }

    protected override void OnChoiceBSuccess()
    {
        Debug.Log("Berry Card - Choice B Success: Player leaves Berries alone");
        // Player leaves berries alone - event stays open so they can try again
    }

    protected override void OnChoiceBFailure()
    {
        Debug.Log("Berry Card - Choice B Failure: Player gives in to hunger");
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null)
            {
                itemManager.ModifyPlayerHunger(10);
            }
        }
        else
        {
            Debug.LogError("BerryCard: Player reference is null!");
        }
    }

    /// <summary>
    /// Choice A (Pflücken) always closes the event, regardless of success or failure
    /// Once berries are picked, they're gone
    /// </summary>
    protected override void HandleChoiceAEventClosure(bool wasSuccess)
    {
        CloseEvent(); // Berries have been picked - event is now closed
        Debug.Log("Berry Card: Berries picked, event closed");
    }

    /// <summary>
    /// Choice B (Nicht pflücken) does NOT close the event
    /// Player can come back and try again later
    /// </summary>
    protected override void HandleChoiceBEventClosure(bool wasSuccess)
    {
        // Don't close the event - player can return and pick berries later
        Debug.Log("Berry Card: Player didn't pick berries, event remains open");
    }
}

