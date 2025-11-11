/*using UnityEngine;
using static Cardtypes;

public class FieldGenerator : MonoBehaviour
{
    public int gridSize = 5;
    public float tileSpacing = 2f; // Abstand zwischen Karten
    public GameObject startCardPrefab;
    public GameObject altarCardPrefab;
    public GameObject blankCardPrefab;

    private GameObject playerInstance;

    void Start()
    {
        playerInstance = GameObject.FindWithTag("Player");
        GenerateField();
    }

    void GenerateField()
    {
        int startX = Random.Range(0, gridSize);
        int startY = Random.Range(0, gridSize);

        int altarX, altarY;
        do
        {
            altarX = Random.Range(0, gridSize);
            altarY = Random.Range(0, gridSize);
        } while (altarX == startX && altarY == startY);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject cardPrefab;

                if (x == startX && y == startY)
                    cardPrefab = startCardPrefab;
                else if (x == altarX && y == altarY)
                    cardPrefab = altarCardPrefab;
                else
                    cardPrefab = blankCardPrefab;

                Vector3 position = new Vector3(x * tileSpacing, 0, y * tileSpacing);
                GameObject card = Instantiate(cardPrefab, position, Quaternion.identity);
                Card cardComp = card.GetComponent<Card>();
                cardComp.type = cardPrefab.GetComponent<Card>().type;

                // Spieler auf Startfeld positionieren
                if (x == startX && y == startY && playerInstance != null)
                {
                    playerInstance.transform.position = position + Vector3.up * 0.6f;
                    Player playerScript = playerInstance.GetComponent<Player>();
                    playerScript.currentCard = cardComp;
                    playerScript.HighlightReachableCards();
                }
            }
        }

        Debug.Log("Spielfeld generiert!");
    }
}*/
