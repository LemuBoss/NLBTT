using UnityEngine;

public class BloodpointCard_D : BloodPointEventCard //Randomizer
{
    public BloodpointCard_D()
    {
        title = "The Randomizer";
        eventTitle = "Narr des Schicksals";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event D (Randomizer) triggered.");
        int randomizedPoints = Random.Range(-5, 10);
        player.modifyBloodpoints(randomizedPoints);
        if (randomizedPoints > 0)
        {
            SetResultText($"Die dunklen Geister des Waldes treiben ihre Spielchen mit dir. Sie sind dir wohlgesonnen, und belohnen dein Vertrauen. \n\n+{randomizedPoints} Blutpunkte erhalten.");
        }
        else if (randomizedPoints == 0)
        {
            SetResultText($"Die dunklen Geister des Waldes treiben ihre Spielchen mit dir. Du belustigst sie, du und deine Naivität. Du erhältst gar nichts.");
        }
        else
        {
            SetResultText($"Die dunklen Geister des Waldes treiben ihre Spielchen mit dir. Sie verachten dich, dich und deine Naivität. \n\n{randomizedPoints} Blutpunkte.");
        }
        Debug.Log($"Player received {randomizedPoints} bloodpoints");
    }
}