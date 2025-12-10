using UnityEngine;

/// <summary>
/// Altar card where players can store bloodpoints for safekeeping
/// The actual bloodpoint transfer logic is handled by the Player class
/// This class exists primarily as a type marker for position checking
/// </summary>
public class AltarCard : Card
{
    public AltarCard()
    {
        title = "Altar";
        canMoveOnto = true;
        turnedAround = true;
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        Debug.Log("Player entered Altar card - bloodpoint storage available");
    }
}