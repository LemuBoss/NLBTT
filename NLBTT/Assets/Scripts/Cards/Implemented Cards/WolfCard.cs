using UnityEngine;

/// <summary>
/// Wolf encounter event with minigame integration
/// Both choices use minigames
/// Fighting has a harder minigame than fleeing
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
        choiceASuccessProbability = 0.3f; // Fallback if minigame not available
        outcomeASuccessText = "Du stellst dich dem Wolf und kämpfst mit allem, was du hast. Deine Reflexe sind scharf, deine Bewegungen präzise. Du kehrst siegreich hervor. (+5 Blutpunkte)";
        outcomeAFailureText = "Du stellst dich dem Wolf, doch deine Reflexe sind zu langsam. Du überschätzt deine eigene Kraft und kommst nur knapp mit deinem Leben davon. (-2 Gesundheit)";
        
        // Load minigame config for Choice A (harder)
        choiceAMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_WolfFight");
        if (choiceAMinigameConfig == null)
        {
            Debug.LogWarning("WolfCard: Could not load MinigameConfig_WolfFight, will use random roll instead");
        }
        
        // CHOICE B: Flee (uses easier minigame)
        choiceBText = "Fliehen";
        choiceBSuccessProbability = 0.75f; // Fallback if minigame not available
        outcomeBSuccessText = "Du nimmst die Flucht auf und navigierst geschickt durch das Unterholz. Der Wolf verliert deine Spur.";
        outcomeBFailureText = "Du nimmst die Flucht auf, stolperst aber über eine Wurzel, die aus dem Boden ragt. Der Wolf schnappt nach deinen Beinen, reißt an deiner Kleidung, doch du kannst gerade noch so wieder Fuß fassen. (-1 Gesundheit)";
        
        // Load minigame config for Choice B (easier)
        choiceBMinigameConfig = Resources.Load<MinigameConfig>("MinigameConfigs/MinigameConfig_WolfFlee");
        if (choiceBMinigameConfig == null)
        {
            Debug.LogWarning("WolfCard: Could not load MinigameConfig_WolfFlee, will use random roll instead");
        }
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
