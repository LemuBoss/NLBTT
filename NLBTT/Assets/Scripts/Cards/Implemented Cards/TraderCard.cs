using UnityEngine;

/// <summary>
/// Trader card that allows players to purchase random items
/// First purchase is free, subsequent purchases cost increasing bloodpoints
/// </summary>
public class TraderCard : Card
{
    private int purchaseCount = 0; // Anzahl an Items, die bereits gekauft wurden
    private int baseBloodpointCost = 5; // wie viel ein Item am Anfang kostet; mit jedem Kauf erhöht sich der Preis um den Multiplikator. Das erste Item ist immer kostenlos.
    private float costMultiplier = 2f;
    
    private string eventTitle = "Einsamer Händler";
    private string eventDescription = "Eine vermummte Gestalt sitzt im Schatten, vor ihr liegen seltsame Waren auf einem Teppich ausgebreitet. Sie fordert dich mit einer Handbewegung dazu auf, näher zu kommen.";
    
    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        // Todo: Händler UI: Zwei Auswahlen, nämlich etwas kaufen (Item immer random), oder gehen. Das erste Item ist kostenlos, aber danach kostet es immer mehr
    }
    
    public void BuyItem()
    {
        // Todo: Berechnete Anzahl an Blutpunkten wird abgezogen, Spieler erhält im Gegenzug zufälliges Item (das er noch nicht besitzt)
        
        // GiveRandomItem();
    }
    
    public void LeaveTrader()
    {
        Debug.Log("Trader Card - Player left without buying");
        
    }

    private void GiveRandomItem()
    {
        // Todo: Spieler erhält hier zufälliges Item
        
        Debug.Log("Trader Card - Random item given to player");
    }
    
    public int GetCurrentPurchaseCost()
    {
        if (purchaseCount == 0)
        {
            return 0; // Erster Kauf ist kostenlos
        }
        
        return Mathf.RoundToInt(baseBloodpointCost * Mathf.Pow(costMultiplier, purchaseCount - 1));
    }

    // Getters for UI
    public string GetEventTitle() => eventTitle;
    public string GetEventDescription() => eventDescription;
    public int GetPurchaseCount() => purchaseCount;
    public string GetBuyButtonText()
    {
        int cost = GetCurrentPurchaseCost();
        if (cost == 0)
        {
            return "Zufälliges Item kaufen (ERSTER KAUF GRATIS)";
        }
        return $"Zufälliges Item kaufen (-{cost} Blutpunkte)";
    }
    public string GetLeaveButtonText() => "Verlassen";
}