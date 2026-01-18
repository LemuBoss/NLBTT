using UnityEngine;

/// <summary>
/// Base class for terrain cards that affect hunger consumption
/// </summary>
public abstract class TerrainCard : Card
{
    protected Player player;
    
    [Header("Terrain Properties")]
    [SerializeField] protected int hungerModifier = 1; // Default movement cost

    public int HungerModifier => hungerModifier;
    
    protected virtual void Start()
    {
        player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogError("TerrainCard: Player not found in scene!");
        }
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        // Hunger cost is handled by Player.TryMoveTo()
    }
}

