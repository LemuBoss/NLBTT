using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays item tooltip near mouse cursor
/// Shows item name, description, destruction effect, and destroy progress
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI destructionEffectText;
    [SerializeField] private TextMeshProUGUI destroyPromptText;
    
    [Header("Destroy Progress Indicator")]
    [SerializeField] private Image mouseIconImage; // Your custom mouse sprite (always visible)
    [SerializeField] private Image progressCircleImage; // Circular progress fill (only visible when holding)
    
    [Header("Positioning")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, -20f);
    [SerializeField] private float smoothSpeed = 10f;
    
    [Header("Colors")]
    [SerializeField] private Color nameColor = Color.white;
    [SerializeField] private Color descriptionColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color destructionColor = new Color(1f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color promptColor = new Color(1f, 1f, 0.6f, 1f);
    
    private RectTransform tooltipRect;
    private Canvas canvas;
    private bool isVisible = false;
    private Vector2 targetPosition;

    private void Awake()
    {
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[ItemTooltipUI] Canvas not found! Make sure this is a child of a Canvas.");
        }

        // Mouse icon is always visible, progress circle starts hidden
        if (mouseIconImage != null)
        {
            mouseIconImage.gameObject.SetActive(true);
        }

        if (progressCircleImage != null)
        {
            progressCircleImage.gameObject.SetActive(false);
            progressCircleImage.fillAmount = 0f;
        }

        Hide();
    }

    private void Update()
    {
        if (isVisible)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Shows tooltip for the specified item
    /// </summary>
    public void Show(ItemManager.ItemType itemType)
    {
        isVisible = true;
        tooltipPanel.SetActive(true);

        // Set item info
        itemNameText.text = GetItemName(itemType);
        itemNameText.color = nameColor;

        itemDescriptionText.text = GetItemDescription(itemType);
        itemDescriptionText.color = descriptionColor;

        destructionEffectText.text = "Bei Zerstörung: " + GetDestructionEffect(itemType);
        destructionEffectText.color = destructionColor;

        destroyPromptText.text = "Halte Maustaste zum Zerstören";
        destroyPromptText.color = promptColor;

        // Ensure mouse icon is visible
        if (mouseIconImage != null)
        {
            mouseIconImage.gameObject.SetActive(true);
        }

        // Reset progress (hide circle)
        UpdateDestroyProgress(0f);

        UpdatePosition();
    }

    /// <summary>
    /// Hides the tooltip
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Updates the destroy progress indicator (0 to 1)
    /// Mouse icon is always visible, progress circle only shows when holding
    /// </summary>
    public void UpdateDestroyProgress(float progress)
    {
        if (progressCircleImage == null)
            return;

        // Show/hide ONLY the progress circle (not the mouse icon)
        bool showProgress = progress > 0f;
        progressCircleImage.gameObject.SetActive(showProgress);

        if (showProgress)
        {
            // Update circular progress fill
            progressCircleImage.fillAmount = progress;

            // Optional: Change color as it fills
            Color progressColor = Color.Lerp(Color.yellow, Color.red, progress);
            progressCircleImage.color = progressColor;
        }
    }

    /// <summary>
    /// Updates tooltip position to follow mouse cursor
    /// </summary>
    private void UpdatePosition()
    {
        if (tooltipRect == null)
            return;

        // Ensure canvas reference is valid
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ItemTooltipUI] Canvas not found!");
                return;
            }
        }

        // Get mouse position in canvas space
        Vector2 mousePos;
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mousePos
        );

        // Apply offset
        targetPosition = mousePos + cursorOffset;

        // Smooth movement
        tooltipRect.anchoredPosition = Vector2.Lerp(
            tooltipRect.anchoredPosition,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );

        // Keep tooltip within canvas bounds
        ClampToCanvas();
    }

    /// <summary>
    /// Ensures tooltip stays within canvas bounds
    /// </summary>
    private void ClampToCanvas()
    {
        if (canvas == null || tooltipRect == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        Vector3[] tooltipCorners = new Vector3[4];
        tooltipRect.GetWorldCorners(tooltipCorners);

        // Get canvas bounds
        float canvasWidth = canvasCorners[2].x - canvasCorners[0].x;
        float canvasHeight = canvasCorners[2].y - canvasCorners[0].y;

        // Get tooltip size
        float tooltipWidth = tooltipCorners[2].x - tooltipCorners[0].x;
        float tooltipHeight = tooltipCorners[2].y - tooltipCorners[0].y;

        // Clamp position
        Vector2 pos = tooltipRect.anchoredPosition;
        
        // Adjust if going off right edge
        if (pos.x + tooltipWidth / 2 > canvasWidth / 2)
        {
            pos.x = canvasWidth / 2 - tooltipWidth / 2;
        }
        
        // Adjust if going off left edge
        if (pos.x - tooltipWidth / 2 < -canvasWidth / 2)
        {
            pos.x = -canvasWidth / 2 + tooltipWidth / 2;
        }
        
        // Adjust if going off top edge
        if (pos.y + tooltipHeight / 2 > canvasHeight / 2)
        {
            pos.y = canvasHeight / 2 - tooltipHeight / 2;
        }
        
        // Adjust if going off bottom edge
        if (pos.y - tooltipHeight / 2 < -canvasHeight / 2)
        {
            pos.y = -canvasHeight / 2 + tooltipHeight / 2;
        }

        tooltipRect.anchoredPosition = pos;
    }

    #region Item Text Data

    private string GetItemName(ItemManager.ItemType itemType)
    {
        switch (itemType)
        {
            case ItemManager.ItemType.Flashlight: return "Taschenlampe";
            case ItemManager.ItemType.HuntingKnife: return "Jagdmesser";
            case ItemManager.ItemType.JarOfNeedles: return "Nadeln im Glas";
            case ItemManager.ItemType.DriedDragonfly: return "Getrocknete Libelle";
            case ItemManager.ItemType.OldBread: return "Altes Brot";
            case ItemManager.ItemType.PileOfAshes: return "Haufen Asche";
            case ItemManager.ItemType.BearClaw: return "Bärenkralle";
            case ItemManager.ItemType.EmergencyRations: return "Notrationen";
            case ItemManager.ItemType.ObsidianShard: return "Obsidianscherbe";
            case ItemManager.ItemType.BunnyStatue: return "Hasenstatue";
            case ItemManager.ItemType.ClimbingRope: return "Kletterseil";
            default: return itemType.ToString();
        }
    }

    private string GetItemDescription(ItemManager.ItemType itemType)
    {
        switch (itemType)
        {
            case ItemManager.ItemType.Flashlight:
                return "Alle Wölfe sind stänig sichtbar, auch auf verdeckten Karten.";
            
            case ItemManager.ItemType.HuntingKnife:
                return "Alle Minigames werden erleichtert (Timer laufen 30% langsamer).";
            
            case ItemManager.ItemType.JarOfNeedles:
                return "+1 Blutpunkt pro Waldkarte in derselben Reihe, wenn du Blutpunkte erhältst.";
            
            case ItemManager.ItemType.DriedDragonfly:
                return "+1 Blutpunkt pro Sumpfkarte in derselben Spalte, wenn du Blutpunkte erhältst.";
            
            case ItemManager.ItemType.OldBread:
                return "+5 Blutpunkte pro verlorenem Gesundheitspunkt. Du kannst dich nicht mehr selbst heilen.";
            
            case ItemManager.ItemType.PileOfAshes:
                return "Alle erhaltenen Blutpunkte werden verdoppelt. Bei Gesundheitsverlust verlierst du alle Blutpunkte.";
            
            case ItemManager.ItemType.BearClaw:
                return "Jedes Mal wenn ein Item zerstört wird: 70% Chance auf +10 BP, 30% Chance auf -1 HP.";
            
            case ItemManager.ItemType.EmergencyRations:
                return "Maximale Nahrungskapazität +10.";
            
            case ItemManager.ItemType.ObsidianShard:
                return "Bei Tod: Stellt einmalig deine gesamte Gesundheit wieder her. Danach: Alle BP-Gewinne halbiert.";
            
            case ItemManager.ItemType.BunnyStatue:
                return "Opfere die Statue, wenn du einem Wolf begegnest, um zu entkommen. Kann auch als Nahrung verzehrt werden.";
            
            case ItemManager.ItemType.ClimbingRope:
                return "Ermöglicht es dir, Felsenkarten zu betreten (verbraucht 4 Nahrung).";
            
            default:
                return "Keine Beschreibung verfügbar.";
        }
    }

    private string GetDestructionEffect(ItemManager.ItemType itemType)
    {
        switch (itemType)
        {
            case ItemManager.ItemType.Flashlight:
                return "Kein Effekt.";
            
            case ItemManager.ItemType.HuntingKnife:
                return "Kein Effekt.";
            
            case ItemManager.ItemType.JarOfNeedles:
                return "Verliere 1 BP pro Waldkarte in derselben Reihe.";
            
            case ItemManager.ItemType.DriedDragonfly:
                return "Verliere 1 BP pro Sumpfkarte in derselben Spalte.";
            
            case ItemManager.ItemType.OldBread:
                return "Kein Effekt.";
            
            case ItemManager.ItemType.PileOfAshes:
                return "Verliere 2 Gesundheit (Minimum 1 HP bleibt übrig).";
            
            case ItemManager.ItemType.BearClaw:
                return "Zufällig +10 BP oder -1 HP.";
            
            case ItemManager.ItemType.EmergencyRations:
                return "Erhalte 10 Nahrung (Überschuss geht verloren).";
            
            case ItemManager.ItemType.BunnyStatue:
                return "Erhalte 10 Nahrung.";
            
            case ItemManager.ItemType.ObsidianShard:
                return "Kein Effekt.";
            
            case ItemManager.ItemType.ClimbingRope:
                return "Kein Effekt.";
            
            default:
                return "Unbekannt.";
        }
    }

    #endregion
}


