using UnityEngine;

/// <summary>
/// BloodpointCard_G: Gain bloodpoints based on distance to the Altar
/// The further from the Altar, the greater the bonus
/// </summary>
public class BloodpointCard_G : BloodPointEventCard
{
    public BloodpointCard_G()
    {
        title = "The Path Ahead";
        eventTitle = "Der Schlafende Altar";
    }
    
    public override void TriggerBloodPointEvent()
    {
        Debug.Log("Bloodpoint Event G triggered - Altar Distance Bonus");
        
        // Get the BoardManager to access the grid
        BoardManager boardManager = Object.FindFirstObjectByType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("BloodpointCard_G: BoardManager not found!");
            return;
        }
        
        // Get the player's current position (where this bloodpoint card is)
        Vector2Int playerPos = boardManager.GetPlayerPosition();
        
        // Find the Altar card on the board
        Vector2Int altarPos = boardManager.GetAltarPosition();
        
        if (altarPos == new Vector2Int(-1, -1)) // Sentinel value for not found
        {
            Debug.LogWarning("BloodpointCard_G: No Altar card found on the board!");
            player.modifyBloodpoints(1); // Fallback bonus
            return;
        }
        
        // Calculate Manhattan distance
        int distance = Mathf.Abs(playerPos.x - altarPos.x) + Mathf.Abs(playerPos.y - altarPos.y);
        
        Debug.Log($"Distance from bloodpoint card at ({playerPos.x}, {playerPos.y}) to Altar at ({altarPos.x}, {altarPos.y}): {distance}");
        
        // Grant bloodpoints based on distance
        // You can adjust this formula as needed
        int bloodpointsGained = distance; 
        
        player.modifyBloodpoints(bloodpointsGained);
        Debug.Log($"Player gained {bloodpointsGained} bloodpoints based on Altar distance");

        if (bloodpointsGained < 3)
        {
            SetResultText($"Dein Schicksal wartet auf dich. Durch die Bäume hindurch siehst du den Glanz, der dich lockt. \n(Erhalte Blutpunkte für die Entfernung zwischen dir und dem Altar)\n+{bloodpointsGained} Blutpunkte erhalten.");
        }
        else
        {
            SetResultText($"Dein Weg wird ein langer sein. Die dunklen Geister des Waldes beobachten deine Mühen mit zunehmenden Interesse. \n(Erhalte Blutpunkte für die Entfernung zwischen dir und dem Altar)\n+{bloodpointsGained} Blutpunkte erhalten.");
        }
        
    }

}