using UnityEngine;

public class BloodpointCard_F : BloodPointEventCard //Codeword: Martyr
{
    public BloodpointCard_F()
    {
        title = "The Martyr";
        eventTitle = "Der Märtyrer";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event F (Martyr) triggered.");
        
        if (player.GetHunger() <= 0)
        {
            int bloodpointsMuliplied = player.GetBloodpoints() * 2;
            player.modifyBloodpoints(bloodpointsMuliplied);
            Debug.Log($"Player received {bloodpointsMuliplied} bloodpoints");
            SetResultText($"Dein Hunger, Musik in den Ohren der Geister. Deine Hingabe wird reichlich belohnt. \n\n+{bloodpointsMuliplied} Blutpunkte erhalten.");
        }
        else
        {
            Debug.Log($"Player received no bloodpoints");
            SetResultText($"Was ist ein Wanderer, wenn nicht die Spuren, die der Kampf ums Überleben an ihm hinterlassen hat? \n\nDu erhältst nichts.");
        }
        
    }
}