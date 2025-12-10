using UnityEngine;

public class HareCard : SimpleEventCard
{
    
    public HareCard()
    {
        eventMessage = "Auf deiner Wanderung stößt du auf eine Lichtung, auf der sich eine Menge Hasen tummeln. Dir gelingt es, einen Hasen zu fangen. Vielleicht könnte er in Zukunft von Nutzen sein...";
    }

    protected override void TriggerEvent()
    {
        Debug.Log("Hare Card - Event Triggered: Player gains Item");
        
        // Todo: Spieler erhält Hasenstatue
    }
    
}