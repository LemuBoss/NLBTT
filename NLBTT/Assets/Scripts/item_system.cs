using UnityEngine;
using System;

[Serializable]
public class Item
{
    public ItemType itemType;
    public string name;
    public string description;
    public Sprite icon;
}

public enum ItemType
{
    Flashlight,        // Taschenlampe
    RabbitStatue,      // Hasenstatue
    Knife,             // Messer
    NeedlesInJar,      // Nadeln im Glas
    DriedDragonfly,    // Getrocknete Libelle
    OldBread,          // Altes Brot
    AshPile,           // Haufen Asche
    CrowFeather,       // Krähenfeder
    BearClaw,          // Bärenkralle
    EmergencyRations,  // Notrationen
    ObsidianShard      // Obsidiansplitter
}

public static class ItemDatabase
{
    public static Item CreateItem(ItemType type)
    {
        Item item = new Item { itemType = type };
        
        switch (type)
        {
            case ItemType.Flashlight:
                item.name = "Taschenlampe";
                item.description = "Zeige eine verdeckte Karte. Cooldown: 10 Züge";
                break;
            case ItemType.RabbitStatue:
                item.name = "Hasenstatue";
                item.description = "Entkommen bei Wolf oder verzehren für 10 Essen";
                break;
            case ItemType.Knife:
                item.name = "Messer";
                item.description = "+1 Bonusblutpunkt bei Wolf. +1 BP bei allen BP-Gewinnen";
                break;
            case ItemType.NeedlesInJar:
                item.name = "Nadeln im Glas";
                item.description = "+1 BP pro Wald in selber Reihe (Events/BP-Karten)";
                break;
            case ItemType.DriedDragonfly:
                item.name = "Getrocknete Libelle";
                item.description = "+2 BP pro Sumpf in selber Spalte (Events/BP-Karten)";
                break;
            case ItemType.OldBread:
                item.name = "Altes Brot";
                item.description = "+5 BP pro verlorener Gesundheit. Zerstören: +10 Essen, +3 HP, -50% BP";
                break;
            case ItemType.AshPile:
                item.name = "Haufen Asche";
                item.description = "Doppelte BP beim Verdienen. Verliere alle BP bei HP-Verlust";
                break;
            case ItemType.CrowFeather:
                item.name = "Krähenfeder";
                item.description = "+2 Max Ausdauer. Zerstören: +3 BP";
                break;
            case ItemType.BearClaw:
                item.name = "Bärenkralle";
                item.description = "+5 BP beim Zerstören jedes Items";
                break;
            case ItemType.EmergencyRations:
                item.name = "Notrationen";
                item.description = "+3 BP bei Essen-Auffüllung. Zerstören: +10 Essen";
                break;
            case ItemType.ObsidianShard:
                item.name = "Obsidiansplitter";
                item.description = "Auto-Zerstörung bei Tod: +1 HP pro 5 BP (verliere diese BP)";
                break;
        }
        
        return item;
    }
    
    public static Item GetRandomItem()
    {
        ItemType[] allTypes = (ItemType[])Enum.GetValues(typeof(ItemType));
        // Taschenlampe ausschließen (nur am Start)
        ItemType randomType = allTypes[UnityEngine.Random.Range(1, allTypes.Length)];
        return CreateItem(randomType);
    }
}