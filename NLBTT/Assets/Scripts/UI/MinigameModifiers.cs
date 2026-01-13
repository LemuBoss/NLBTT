using UnityEngine;

/// <summary>
/// Runtime modifiers that can be applied by external systems (Player, ItemManager, etc.)
/// This allows items, buffs, and debuffs to affect minigame difficulty dynamically
/// </summary>
[System.Serializable]
public class MinigameModifiers
{
    [Header("Timer Modifiers")]
    [Tooltip("Multiplier for timer drain speed (0.5 = half speed, 2.0 = double speed)")]
    public float timerDrainMultiplier = 1f;
    
    [Header("Penalty Modifiers")]
    [Tooltip("Multiplier for misclick penalties (0.5 = half penalty, 2.0 = double penalty)")]
    public float misclickPenaltyMultiplier = 1f;
    
    [Header("Circle Modifiers")]
    [Tooltip("Add or subtract fake circles from base config (-2 removes 2 fakes, +1 adds 1 fake)")]
    public int fakeCircleCountModifier = 0;
    
    [Header("Special Effects")]
    [Tooltip("Override: Invert fake circles to give bonus instead of penalty")]
    public bool invertFakeCircles = false;
    
    [Tooltip("Override: Enable drift movement for circles")]
    public bool enableDrift = false;
    
    /// <summary>
    /// Creates default modifiers (no changes)
    /// </summary>
    public MinigameModifiers()
    {
        timerDrainMultiplier = 1f;
        misclickPenaltyMultiplier = 1f;
        fakeCircleCountModifier = 0;
        invertFakeCircles = false;
        enableDrift = false;
    }
    
    /// <summary>
    /// Example: Apply modifiers based on player state
    /// Call this from your Player class or event system
    /// </summary>
    public static MinigameModifiers FromPlayerState(Player player)
    {
        MinigameModifiers mods = new MinigameModifiers();
        
        if (player == null)
            return mods;
        
        ItemManager itemManager = player.GetItemManager();
        
        // Example: Check for specific items
        if (itemManager != null)
        {
            // Lucky charm reduces fake circles
            //if (itemManager.HasItem("LuckyCharm"))
            //{
            //    mods.fakeCircleCountModifier = -2;
            //}
            
            // Time crystal slows down timers
            //if (itemManager.HasItem("TimeCrystal"))
            //{
            //    mods.timerDrainMultiplier = 0.7f;
            //}
            
            // Blessed amulet inverts fake circles
            //if (itemManager.HasItem("BlessedAmulet"))
            //{
            //    mods.invertFakeCircles = true;
            //}
            
            // Steady hands reduce penalties
            //if (itemManager.HasItem("SteadyHands"))
            //{
            //    mods.misclickPenaltyMultiplier = 0.5f;
            //}
        }
        
        // Example: Check for debuffs
        
        // Starving causes drift
        if (player.isStarving())
        {
            mods.enableDrift = true;
        }
        
        // Low stamina speeds up timers
        if (player.isStaminaEmpty())
        {
            mods.timerDrainMultiplier = 1.3f;
        }
        
        // Low health increases penalties
        if (player.GetHealth() <= 2)
        {
            mods.misclickPenaltyMultiplier = 1.5f;
        }
        
        return mods;
    }
    
    /// <summary>
    /// Combines multiple modifier sets (useful for stacking effects)
    /// </summary>
    public static MinigameModifiers Combine(params MinigameModifiers[] modifierSets)
    {
        MinigameModifiers combined = new MinigameModifiers();
        
        foreach (var mods in modifierSets)
        {
            if (mods == null)
                continue;
            
            // Multiply multipliers
            combined.timerDrainMultiplier *= mods.timerDrainMultiplier;
            combined.misclickPenaltyMultiplier *= mods.misclickPenaltyMultiplier;
            
            // Add fake circle modifiers
            combined.fakeCircleCountModifier += mods.fakeCircleCountModifier;
            
            // OR boolean flags (if any set enables it, it's enabled)
            combined.invertFakeCircles |= mods.invertFakeCircles;
            combined.enableDrift |= mods.enableDrift;
        }
        
        return combined;
    }
    
    /// <summary>
    /// Debug string representation
    /// </summary>
    public override string ToString()
    {
        return $"MinigameModifiers: TimerDrain={timerDrainMultiplier:F2}x, " +
               $"Penalty={misclickPenaltyMultiplier:F2}x, " +
               $"FakeCircles={fakeCircleCountModifier:+#;-#;0}, " +
               $"InvertFakes={invertFakeCircles}, " +
               $"Drift={enableDrift}";
    }
}
