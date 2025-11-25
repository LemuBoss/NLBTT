using UnityEngine;

/// <summary>
/// Base class for all cards in the game
/// By default, all cards restore 1 stamina when entered (representing rest)
/// Subclasses can override staminaModifier to change this behavior
/// </summary>
public abstract class Card
{
    protected string title;
    protected bool canMoveOnto = true;
    protected bool turnedAround = true;
    protected bool allowedForShuffle = true;
    
    protected int staminaModifier = 1; // Default: +1 stamina (rest)

    public string Title => title;
    public bool CanMoveOnto => canMoveOnto;
    public bool TurnedAround => turnedAround;

    // Called when player moves onto this card
    public virtual void OnPlayerEnter()
    {
        // Apply stamina modifier
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null && staminaModifier != 0)
        {
            player.modifyStamina(staminaModifier);
            
            if (staminaModifier > 0)
            {
                Debug.Log($"[{this.GetType().Name}] Player rested, gained {staminaModifier} stamina. Current: {player.GetStamina()}/{player.GetStaminaCap()}");
            }
            else
            {
                Debug.Log($"[{this.GetType().Name}] Difficult terrain, lost {Mathf.Abs(staminaModifier)} stamina. Current: {player.GetStamina()}/{player.GetStaminaCap()}");
            }
        }
    }

    public virtual void TurnOver()
    {
        turnedAround = false;
    }
}