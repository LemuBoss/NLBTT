using UnityEngine;

public class BloodpointCard_A : BloodPointEventCard //Codeword: Price of Gluttony
{
    public BloodpointCard_A()
    {
        title = "Price of Gluttony";
        eventTitle = "Preis der Völlerei";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event A (Price of Gluttony) triggered.");
        
        // Use public getter methods instead of accessing private fields
        if (player.GetHunger() <= 5)
        {
            int hungerDeficit = player.GetHungerCap() - player.GetHunger();
            player.modifyBloodpoints(hungerDeficit);
            SetResultText($"Dein Hunger quält dich. Die dunklen Geister des Waldes belohnen dein Leiden.\n\n+{hungerDeficit} Blutpunkte erhalten.");
            Debug.Log($"Player received {hungerDeficit} bloodpoints");
        }
        else
        {
            int hungerSurplus = player.GetHunger();
            player.modifyBloodpoints(-hungerSurplus);
            SetResultText($"Du bist gut genährt und zufrieden. Die dunklen Geister des Waldes verachten deine Völlerei.\n\n-{hungerSurplus} Blutpunkte verloren.");
            Debug.Log($"Player lost {hungerSurplus} bloodpoints");
        }

        // Todo: Spezielle Effekte einbauen, wie z.B. Spieler erhält/verliert so und so viele Blutpunkte nach bestimmten Konditionen. 
        // An dieser Stelle auch Checks nach bestimmten Items, die Blutpunkte beeinflussen, einbauen
    }
}