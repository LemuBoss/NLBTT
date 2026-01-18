using UnityEngine;

public class RockCard : TerrainCard
{
    public RockCard()
    {
        title = "Felsen";
        canMoveOnto = false; 
        hungerModifier = 4; 
    }

    public override void OnPlayerEnter()
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            ItemManager itemManager = player.GetItemManager();
            if (itemManager != null && itemManager.HasItem(ItemManager.ItemType.ClimbingRope))
            {
                // Player can climb with rope
                base.OnPlayerEnter(); 
                Debug.Log("[RockCard] Player climbed rock using Climbing Rope (4 hunger cost)");
                return;
            }
        }
        
        Debug.Log("[RockCard] Player cannot climb without Climbing Rope");
    }
}

