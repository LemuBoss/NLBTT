using UnityEngine;

/// <summary>
/// Simple prototype card for testing visual behaviors
/// </summary>
public class PrototypeCard : Card
{
    public PrototypeCard(string cardTitle, bool isWalkable = true)
    {
        title = cardTitle;
        canMoveOnto = isWalkable;
        turnedAround = true; // Starts face down
    }

    public override void OnPlayerEnter()
    {
        Debug.Log($"Player entered card: {title}");
    }
}