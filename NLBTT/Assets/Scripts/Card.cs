/*using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Card : MonoBehaviour
{
    public Cardtypes.CardTypes type;
    private Renderer rend;
    private Color baseColor;
    private bool isHighlighted = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            baseColor = rend.material.color;
    }

    public void SetHighlight(bool state)
    {
        isHighlighted = state;
        if (rend == null) return;
        rend.material.color = state ? Color.yellow : baseColor;
    }

    void OnMouseEnter()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null) return;

        if (player.IsAdjacent(this))
            SetHighlight(true);
    }

    void OnMouseExit()
    {
        if (!isHighlighted)
            SetHighlight(false);
    }

    void OnMouseDown()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null) return;

        if (player.IsAdjacent(this))
            player.MoveTo(this);
    }
}*/
