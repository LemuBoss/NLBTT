using UnityEngine;

public class RockCard : TerrainCard
{
    public RockCard()
    {
        title = "Felsen";
        canMoveOnto = false;
    }

    public override void OnPlayerEnter()
    {
    }
}