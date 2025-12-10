using UnityEngine;

public class ForestCard : TerrainCard
{
    public ForestCard()
    {
        title = "Wald";
        canMoveOnto = true;
        staminaModifier = -1; 
    }

}