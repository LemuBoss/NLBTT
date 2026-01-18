using UnityEngine;

public class PathCard : TerrainCard
{
    public PathCard()
    {
        title = "Pfad";
        canMoveOnto = true;
        hungerModifier = 1; // Standard cost
    }
}