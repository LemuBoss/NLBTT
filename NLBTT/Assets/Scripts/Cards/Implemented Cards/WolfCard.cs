using UnityEngine;

public class WolfCard : ComplexEventCard
{
    public WolfCard()
    {
        // Event details
        eventTitle = "Wolfangriff!";
        eventDescription = "Augen blitzen dich aus dem Unterholz aus an. Ein knurrendes Biest schleicht aus den Schatten hervor, Zähne gebleckt. Es sieht genauso hungrig aus wie du.";
        
        choiceAText = "Kämpfen";
        choiceASuccessProbability = 0.3f; 
        outcomeASuccessText = "Du stellst dich dem Wolf und kämpfst mit allem, was du hast. Du kehrst siegreich hervor. (+5 Blutpunkte)";
        outcomeAFailureText = "Du stellst dich dem Wolf, doch du überschätzt deine eigene Kraft. Du kommst nur knapp mit deinem Leben davon. (-2 Gesundheit)";
        
        choiceBText = "Fliehen";
        choiceBSuccessProbability = 0.75f; 
        outcomeBSuccessText = "Du bist nicht in der Verfassung dazu zu kämpfen und entschließt dich dazu, zu fliehen.";
        outcomeBFailureText = "Du nimmst die Flucht auf, stolperst aber über eine Wurzel, die aus dem Boden ragt. Der Wolf schnappt nach deinen Beinen, reißt an deiner Kleidung, doch du kannst gerade noch so wieder Fuß fassen. (-1 Gesundheit)";
    }

    protected override void OnChoiceASuccess()
    {
        Debug.Log("Wolf Card - Choice A Success: Wolf fought off, bloodpoints gained");
        
        if (player != null)
        {
            player.modifyBloodpoints(5);
        }
        else
        {
            Debug.LogError("WolfCard: Player reference is null!");
        }
    }

    protected override void OnChoiceAFailure()
    {
        Debug.Log("Wolf Card - Choice A Failure: Wolf attacks, player loses health");
        
        if (player != null)
        {
            player.modifyHealth(-2);
        }
        else
        {
            Debug.LogError("WolfCard: Player reference is null!");
        }
    }

    protected override void OnChoiceBSuccess()
    {
        Debug.Log("Wolf Card - Choice B Success: Successful Escape");
        
        // No special effect - player successfully escapes without consequence
    }

    protected override void OnChoiceBFailure()
    {
        Debug.Log("Wolf Card - Choice B Failure: Wolf catches up and hurts player");
        
        if (player != null)
        {
            player.modifyHealth(-1);
        }
        else
        {
            Debug.LogError("WolfCard: Player reference is null!");
        }
    }
}