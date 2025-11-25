using UnityEngine;

public class BloodpointCard_B : BloodPointEventCard //Codeword: Traveller
{
    public BloodpointCard_B()
    {
        title = "The Traveller";
        eventTitle = "Weit gereist";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event B (Traveller) triggered.");
        int bloodpointCardsVisited = player.GetBloodpointCardsVisited() * 2;
        player.modifyBloodpoints(bloodpointCardsVisited);
        if (bloodpointCardsVisited > 0)
        {
            SetResultText($"Du bist weit gereist und hast viel gesehen. Die Geister des Waldes belohnen dein Wissen. \n\n+{bloodpointCardsVisited} Blutpunkte erhalten.");
        }
        else
        {
            SetResultText($"Du bist ein Fremdling, ungeschickt und ahnungslos in diesem fremden Territorium. In den Schatten des Waldes geht ein Seufzen umher. Waren das Augen, die dich anfunkelten? Oder nur die Sonne, die sich auf dem feinen Tau dutzender Spinnenweben widerspiegelte? \n\n+{bloodpointCardsVisited} Blutpunkte erhalten.");
        }
        Debug.Log($"Player received {bloodpointCardsVisited} bloodpoints");
    }
}