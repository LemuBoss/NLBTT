using UnityEngine;

public class SwampCard : TerrainCard
{
    public SwampCard()
    {
        title = "Sumpf";
        canMoveOnto = true;
        hungerModifier = 3; // Triple cost
    }
}

