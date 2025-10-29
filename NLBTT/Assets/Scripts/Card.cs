using System.Collections.Generic;
using UnityEngine;
using static Cardtypes;

public class Card : MonoBehaviour
{
    [SerializeField] public string name;
    [SerializeField] public CardTypes type;

    // Statische Listen für verschiedene Kartentypen
    public static List<Card> eventCards = new List<Card>();
    public static List<Card> terrainCards = new List<Card>();
    public static List<Card> itemCards = new List<Card>();

    // Konstruktor
    public Card(string Name, CardTypes Type)
    {
        name = Name;
        type = Type;

        // Karte automatisch einsortieren
        AddToList(this);
    }

    // Methode, die Karten je nach Typ in die passende Liste legt
    public static void AddToList(Card card)
    {
        switch (card.type)
        {
            case CardTypes.Event:
                eventCards.Add(card);
                break;

            case CardTypes.Terrain:
                terrainCards.Add(card);
                break;

            case CardTypes.Item:
                itemCards.Add(card);
                break;

            default:
                Debug.LogWarning($"Unbekannter Kartentyp: {card.type}");
                break;
        }
    }
}
