using UnityEngine;

public class ForestCard : TerrainCard
{
    public ForestCard()
    {
        title = "Wald";
        canMoveOnto = true;
        hungerModifier = 2; // Double cost
    }
}
