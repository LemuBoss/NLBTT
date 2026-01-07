using UnityEngine;

public class BerryCard : ComplexEventCard
{
    public BerryCard()
    {
        // Event details
        eventTitle = "Beerenbusch";
        eventDescription = "Du stößt auf einen Beerenbusch. Seine Beeren glänzen verlockend im wenigen Licht, das durch das Blätterdach fällt. Doch sie sehen ziemlich ähnlich zu giftigen Beeren aus...";
        
        choiceAText = "Pflücken";
        choiceASuccessProbability = 0.7f; 
        outcomeASuccessText = "Das Knurren in deinem Magen übertönt die Stimme der Vorsichtig in dir. Du pflückst und verspeist die Beeren ohne dich zu vergiften. (+15 Nahrung)";
        outcomeAFailureText = "Das Knurren in deinem Magen übertönt die Stimme der Vorsichtig in dir. Du pflückst und verspeist die Beeren, leidest aber anschließend unter starkem Erbrechen. (-1 Gesundheit, -5 Nahrung)";
        
        choiceBText = "Nicht pflücken";
        choiceBSuccessProbability = 1f; // ja, 100% Erfolgschance 
        outcomeBSuccessText = "Du entscheidest dich dazu, auf deine Vernunft zu hören und die Beeren nicht zu pflücken. Vielleicht findest du unterwegs eine alternative Nahrungsquelle... Vielleicht.";
        outcomeBFailureText = "Du entscheidest dich dazu, auf deine Vernunft zu hören und die Beeren nicht zu pflücken, doch dein Hunger dreht mit dir durch. Du isst die Beeren und merkst, dass sie nicht von der giftigen Variante waren. (+10 Nahrung)";
    }

    protected override void OnChoiceASuccess()
    {
        Debug.Log("Berry Card - Choice A Success: Berries picked, Berries were not poisonous, +15 food");
        if (player != null)
        {
            player.modifyHunger(15);
        }
        else
        {
            Debug.LogError("BerryCard: Player reference is null!");
        }
    }

    protected override void OnChoiceAFailure()
    {
        Debug.Log("Berry Card - Choice A Failure: Berries picked but poisonous, -1 health, -5 food");
        if (player != null)
        {
            player.modifyHunger(-5);
            player.modifyHealth(-1);
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
        Debug.Log("Berry Card - Choice B Failure: This should literally not trigger at all.");
        if (player != null)
        {
            player.modifyHunger(10);
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
