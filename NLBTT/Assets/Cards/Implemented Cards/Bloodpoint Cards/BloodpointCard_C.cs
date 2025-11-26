using UnityEngine;

/// <summary>
/// BloodpointCard_C: Repeats the effect of the last bloodpoint card visited
/// If it's the first bloodpoint card visited, the player gains 1 bloodpoint instead
/// </summary>
public class BloodpointCard_C : BloodPointEventCard // Repeater
{
    public BloodpointCard_C()
    {
        title = "The Repeater";
        eventTitle = "Mimikry";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event C (Repeater) triggered.");
        
        BloodPointEventCard lastCard = player.GetLastBloodPointCardVisited();
        
        if (lastCard == null)
        {
            // This is the first bloodpoint card visited
            Debug.Log("No previous bloodpoint card visited. Granting 1 bloodpoint.");
            player.modifyBloodpoints(1);
            SetResultText($"Der Ort kommt dir fremd vor; gar nichts ist dir vertraut. In deinen Erinnerungen nichts als Nebel. \n(Wiederhole den Effekt der letzten Blutpunktekarte)\n+1 Blutpunkt erhalten.");
            Debug.Log("Player gained 1 bloodpoint (first card bonus)");
        }
        else if (lastCard == this)
        {
            // Edge case: somehow this card is the last visited (shouldn't happen in normal play)
            Debug.LogWarning("BloodpointCard_C is trying to repeat itself. Granting 1 bloodpoint instead.");
            player.modifyBloodpoints(1);
        }
        else
        {
            // Repeat the last card's effect by calling its TriggerBloodPointEvent
            Debug.Log($"Repeating effect from: {lastCard.GetType().Name}");
            lastCard.TriggerBloodPointEvent();
            SetResultText($"Vor dir eine schmenenhafte Gestalt, die sich wandelt und formt. Im Nebel nimmt sie die Gestalt jener Dinge an, die dir auf deiner Reise bereits begegnet sind. \n(Wiederhole den Effekt der letzten Blutpunktekarte)");
        }
    }
}