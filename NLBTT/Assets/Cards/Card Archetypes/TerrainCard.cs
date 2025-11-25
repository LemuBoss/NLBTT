using UnityEngine;

/// <summary>
/// Base class for terrain cards that affect stamina
/// </summary>
public abstract class TerrainCard : Card
{
    protected Player player;

    public int StaminaModifier => staminaModifier;
    
    protected virtual void Start()
    {
        // Get reference to the Player instance
        player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogError("TerrainCard: Player not found in scene!");
        }
    }

    public override void OnPlayerEnter()
    {
        // Call the base Card.OnPlayerEnter() to apply stamina modifier
        base.OnPlayerEnter();
    }
}