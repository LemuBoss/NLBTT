using UnityEngine;

/// <summary>
/// A card that serves as a spawn point for a wolf
/// Acts like a regular walkable card but spawns a wolf at the start of the game
/// </summary>
public class WolfdenCard : Card
{
    private Wolf assignedWolf;

    public WolfdenCard()
    {
        title = "Wolf Den";
        canMoveOnto = true;
        blocksLineOfSight = false;
        staminaModifier = 1; // Normal rest bonus
    }

    /// <summary>
    /// Assigns a wolf to this den
    /// Called by WolfAI during wolf spawning
    /// </summary>
    public void AssignWolf(Wolf wolf)
    {
        assignedWolf = wolf;
        Debug.Log($"[WolfdenCard] Wolf assigned to den");
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
    }
}
