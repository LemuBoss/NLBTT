/*using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int hunger = 20;
    public Card currentCard;
    private List<Card> reachableCards = new List<Card>();
    public float moveSpeed = 3f; // Für sanftes Gleiten
    private bool isMoving = false;
    private Vector3 targetPos;

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
                isMoving = false;
        }
    }

    public void MoveTo(Card newCard)
    {
        if (!IsAdjacent(newCard))
        {
            Debug.LogWarning("❌ Bewegung nicht erlaubt – Karte ist nicht erreichbar!");
            return;
        }

        targetPos = newCard.transform.position + Vector3.up * 0.6f;
        isMoving = true;
        currentCard = newCard;
        hunger -= 1;

        Debug.Log($"🧍‍♂️ Spieler bewegt sich auf {newCard.name} – Hunger: {hunger}");

        if (newCard.type == Cardtypes.CardTypes.Altar)
            Altar.ReachAltar();

        HighlightReachableCards();
    }

    public bool IsAdjacent(Card target)
    {
        if (currentCard == null) return true; // Erste Bewegung erlaubt

        float dx = Mathf.Abs(target.transform.position.x - currentCard.transform.position.x);
        float dz = Mathf.Abs(target.transform.position.z - currentCard.transform.position.z);

        float spacing = 2f; // MUSS mit FieldGenerator übereinstimmen
        return (dx == spacing && dz == 0) || (dx == 0 && dz == spacing);
    }

    public void HighlightReachableCards()
    {
        foreach (Card c in FindObjectsOfType<Card>())
            c.SetHighlight(false);

        reachableCards.Clear();

        if (currentCard == null) return;

        Vector3 pos = currentCard.transform.position;
        float spacing = 2f;

        foreach (Card c in FindObjectsOfType<Card>())
        {
            float dx = Mathf.Abs(c.transform.position.x - pos.x);
            float dz = Mathf.Abs(c.transform.position.z - pos.z);

            if ((dx == spacing && dz == 0) || (dx == 0 && dz == spacing))
            {
                reachableCards.Add(c);
                c.SetHighlight(true);
            }
        }
    }
}*/
