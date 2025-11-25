using UnityEngine;

public class SwampCard : TerrainCard
{
    public SwampCard()
    {
        title = "Sumpf";
        canMoveOnto = true;
        staminaModifier = -2; 
    }
    
}
