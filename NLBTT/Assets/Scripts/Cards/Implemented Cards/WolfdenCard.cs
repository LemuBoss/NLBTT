using UnityEngine;

/// <summary>
/// A card that serves as a spawn point for a wolf
/// Acts like a regular walkable card but spawns a wolf at the start of the game
/// Wolves respawn here after being defeated
/// </summary>
public class WolfdenCard : Card
{
    private Wolf assignedWolf;

    public WolfdenCard()
    {
        title = "Wolf Den";
        canMoveOnto = true;
        blocksLineOfSight = false;
    }

    /// <summary>
    /// Assigns a wolf to this den
    /// Called by WolfAI during wolf spawning
    /// </summary>
    public void AssignWolf(Wolf wolf)
    {
        assignedWolf = wolf;
        Vector2Int denPosition = wolf.GetSpawnPosition();
        Debug.Log($"[WolfdenCard] Wolf assigned to den at ({denPosition.x}, {denPosition.y})");
    }

    /// <summary>
    /// Gets the wolf assigned to this den
    /// </summary>
    public Wolf GetAssignedWolf()
    {
        return assignedWolf;
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        Debug.Log($"[WolfdenCard] Player entered wolf den");
        
        // Optional: Log if wolf is currently despawned
        if (assignedWolf != null && assignedWolf.IsDespawned())
        {
            int turnsLeft = assignedWolf.GetTurnsUntilRespawn();
            Debug.Log($"[WolfdenCard] Wolf is currently despawned. Will respawn in {turnsLeft} turns.");
        }
    }
}

