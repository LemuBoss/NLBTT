using UnityEngine;

/// <summary>
/// BloodpointCard_E: Gain 2 Bloodpoints for each adjacent terrain card
/// </summary>
public class BloodpointCard_E : BloodPointEventCard
{
    public BloodpointCard_E()
    {
        title = "The Cartographer";
        eventTitle = "Der Kartograf";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event E triggered - Adjacent Terrain Bonus");
        
        // Get the BoardManager to access grid and adjacency checking
        BoardManager boardManager = Object.FindFirstObjectByType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("BloodpointCard_E: BoardManager not found!");
            return;
        }
        
        // Get the player's current position
        Vector2Int playerPos = boardManager.GetPlayerPosition();
        
        // Count adjacent terrain cards
        int adjacentTerrainCount = 0;
        
        // Check all four adjacent positions (up, down, left, right)
        Vector2Int[] adjacentPositions = new Vector2Int[]
        {
            new Vector2Int(playerPos.x + 1, playerPos.y), // Right
            new Vector2Int(playerPos.x - 1, playerPos.y), // Left
            new Vector2Int(playerPos.x, playerPos.y + 1), // Up
            new Vector2Int(playerPos.x, playerPos.y - 1)  // Down
        };
        
        foreach (Vector2Int adjPos in adjacentPositions)
        {
            Card adjacentCard = boardManager.GetCardAt(adjPos.x, adjPos.y);
            
            if (adjacentCard != null && adjacentCard is TerrainCard)
            {
                adjacentTerrainCount++;
                Debug.Log($"Found adjacent terrain card at ({adjPos.x}, {adjPos.y}): {adjacentCard.GetType().Name}");
            }
        }
        
        // Calculate bloodpoints gained
        int bloodpointsGained = adjacentTerrainCount * 2;
        
        if (bloodpointsGained > 0)
        {
            player.modifyBloodpoints(bloodpointsGained);
            Debug.Log($"Player gained {bloodpointsGained} bloodpoints ({adjacentTerrainCount} adjacent terrain cards × 2)");
            SetResultText($"Die Schönheit der Natur ist ein wahres Wunder. Auch die Geister des Waldes erkennen dies an. \n(Erhalte Blutpunkte für jede angrenzende Terrainkarte)\n+{bloodpointsGained} Blutpunkte erhalten.");
        }
        else
        {
            Debug.Log("No adjacent terrain cards found. No bloodpoints gained.");
            SetResultText($"Die Schönheit der Natur ist ein wahres Wunder. Doch hier, inmitten der trostlosen Einöde, ist das Blattwerk der Bäume oder das Ächzen des Sumpfes nur eine ferne Erinnerung. \n(Erhalte Blutpunkte für jede angrenzende Terrainkarte)\n+Du erhältst gar nichts.");
        }
    }
}
