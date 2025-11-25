using UnityEngine;

public class StartCard : Card
{
    public StartCard()
    {
        title = "Startkarte";
        canMoveOnto = true;
        turnedAround = false; 
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        Debug.Log($"Player entered card: {title}");
    }
}