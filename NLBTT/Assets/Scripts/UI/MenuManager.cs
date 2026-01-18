using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the main menu with animated transitions for Rules, Credits, and Exit confirmation
/// </summary>
public class MenuManager2 : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup; // F�r Fade-Effekt der Buttons
    [SerializeField] private Button startButton;
    [SerializeField] private Button rulesButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Rules Panel")]
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private Image rulesOverlay;
    [SerializeField] private RectTransform rulesTopBar;
    [SerializeField] private RectTransform rulesBottomBar;
    [SerializeField] private CanvasGroup rulesTextCanvasGroup;
    [SerializeField] private TextMeshProUGUI rulesText;
    [SerializeField] private Button rulesBackButton;

    [Header("Credits Panel")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Image creditsOverlay;
    [SerializeField] private RectTransform creditsTopBar;
    [SerializeField] private RectTransform creditsBottomBar;
    [SerializeField] private CanvasGroup creditsTextCanvasGroup;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private Button creditsBackButton;

    [Header("Exit Confirmation Panel")]
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Image exitOverlay;
    [SerializeField] private CanvasGroup exitCanvasGroup;
    [SerializeField] private Button confirmExitButton;
    [SerializeField] private Button cancelExitButton;

    [Header("Animation Settings")]
    [SerializeField] private float barSlideDistance = 150f;
    [SerializeField] private float barAnimationDuration = 0.5f;
    [SerializeField] private float textFadeDuration = 0.8f;
    [SerializeField] private float textFadeDelay = 0.3f;
    [SerializeField] private float overlayFadeDuration = 0.3f;
    [SerializeField] private AnimationCurve barAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve textFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Content Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private Color overlayColor = new Color(0.5f, 0f, 0f, 0.7f); // Rot mit Transparenz

    [Header("Rules Text")]
    [TextArea(10, 20)]
    [SerializeField] private string rulesContent = @"SPIELREGELN

<size=18>Steuerung:</size>
� Klicke auf benachbarte Karten um dich zu bewegen
� [1] - Hunger in Blutpunkte umwandeln
� [2] - Gesundheit in Blutpunkte umwandeln
� [ENTER] - Blutpunkte am Altar deponieren
� [ESC] - Spiel pausieren/fortsetzen

<size=18>Spielziel:</size>
Sammle Blutpunkte und �berlebe die Reise durch den Wald.

<size=18>Ressourcen:</size>
� Terrain zieht dir Ausdauer ab
� Bewegungen kosten Nahrung
� Spezielle Karten verleihen dir Blutpunkte
� Verwalte deine Ressourcen weise!

<size=18>Tipps:</size>
� Plane deine Route sorgf�ltig
� Achte auf deine Ressourcen
� Nutze Blutpunkte strategisch";

    [Header("Credits Text")]
    [TextArea(10, 20)]
    [SerializeField] private string creditsContent = @"CREDITS

<size=24>Entwickelt von</size>
Dein Name

<size=18>Programmierung:</size>
Dein Name

<size=18>Game Design:</size>
Dein Name

<size=18>Grafik & UI:</size>
Dein Name

<size=18>Sound & Musik:</size>
Dein Name

<size=18>Besonderer Dank an:</size>
Unity Community
TextMeshPro Team
Claude.ai

<size=14>� 2025 - Alle Rechte vorbehalten</size>";

    private bool isAnimating = false;
    private MenuState currentState = MenuState.Main;

    // Store initial positions for animations
    private Vector2 rulesTopBarStart, rulesTopBarEnd;
    private Vector2 rulesBottomBarStart, rulesBottomBarEnd;
    private Vector2 creditsTopBarStart, creditsTopBarEnd;
    private Vector2 creditsBottomBarStart, creditsBottomBarEnd;

    private enum MenuState
    {
        Main,
        Rules,
        Credits,
        ExitConfirm
    }

    private void Awake()
    {
        SetupButtons();
        SetupTexts();
        InitializePositions();
        HideAllPanels();
    }

    private void SetupButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (rulesButton != null)
            rulesButton.onClick.AddListener(ShowRules);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);

        if (exitButton != null)
            exitButton.onClick.AddListener(ShowExitConfirmation);

        if (rulesBackButton != null)
            rulesBackButton.onClick.AddListener(HideRules);

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(HideCredits);

        if (confirmExitButton != null)
            confirmExitButton.onClick.AddListener(ConfirmExit);

        if (cancelExitButton != null)
            cancelExitButton.onClick.AddListener(CancelExit);
    }

    private void SetupTexts()
    {
        if (rulesText != null)
            rulesText.text = rulesContent;

        if (creditsText != null)
            creditsText.text = creditsContent;
    }

    private void InitializePositions()
    {
        // Rules bars
        if (rulesTopBar != null)
        {
            rulesTopBarStart = rulesTopBar.anchoredPosition;
            rulesTopBarEnd = rulesTopBarStart + Vector2.down * barSlideDistance;
        }

        if (rulesBottomBar != null)
        {
            rulesBottomBarStart = rulesBottomBar.anchoredPosition;
            rulesBottomBarEnd = rulesBottomBarStart + Vector2.up * barSlideDistance;
        }

        // Credits bars
        if (creditsTopBar != null)
        {
            creditsTopBarStart = creditsTopBar.anchoredPosition;
            creditsTopBarEnd = creditsTopBarStart + Vector2.down * barSlideDistance;
        }

        if (creditsBottomBar != null)
        {
            creditsBottomBarStart = creditsBottomBar.anchoredPosition;
            creditsBottomBarEnd = creditsBottomBarStart + Vector2.up * barSlideDistance;
        }
    }

    private void HideAllPanels()
    {
        if (rulesPanel != null)
            rulesPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
    }

    private void Update()
    {
        // ESC key handling based on current state
        if (Input.GetKeyDown(KeyCode.Escape) && !isAnimating)
        {
            switch (currentState)
            {
                case MenuState.Main:
                    ShowExitConfirmation();
                    break;

                case MenuState.Rules:
                    HideRules();
                    break;

                case MenuState.Credits:
                    HideCredits();
                    break;

                case MenuState.ExitConfirm:
                    ConfirmExit();
                    break;
            }
        }
    }

    // ===== MAIN MENU ACTIONS =====

    public void StartGame()
    {
        if (isAnimating)
            return;

        Debug.Log("[MenuManager] Starting game...");
        SceneManager.LoadScene("SampleScene");
    }

    // ===== RULES =====

    public void ShowRules()
    {
        if (isAnimating)
            return;

        currentState = MenuState.Rules;
        StartCoroutine(ShowPanelAnimation(rulesPanel, rulesOverlay, rulesTopBar, rulesBottomBar,
            rulesTextCanvasGroup, rulesTopBarStart, rulesTopBarEnd, rulesBottomBarStart, rulesBottomBarEnd));
    }

    public void HideRules()
    {
        if (isAnimating)
            return;

        currentState = MenuState.Main;
        StartCoroutine(HidePanelAnimation(rulesPanel, rulesOverlay, rulesTopBar, rulesBottomBar,
            rulesTextCanvasGroup, rulesTopBarStart, rulesTopBarEnd, rulesBottomBarStart, rulesBottomBarEnd));
    }

    // ===== CREDITS =====

    public void ShowCredits()
    {
        if (isAnimating)
            return;

        currentState = MenuState.Credits;
        StartCoroutine(ShowPanelAnimation(creditsPanel, creditsOverlay, creditsTopBar, creditsBottomBar,
            creditsTextCanvasGroup, creditsTopBarStart, creditsTopBarEnd, creditsBottomBarStart, creditsBottomBarEnd));
    }

    public void HideCredits()
    {
        if (isAnimating)
            return;

        currentState = MenuState.Main;
        StartCoroutine(HidePanelAnimation(creditsPanel, creditsOverlay, creditsTopBar, creditsBottomBar,
            creditsTextCanvasGroup, creditsTopBarStart, creditsTopBarEnd, creditsBottomBarStart, creditsBottomBarEnd));
    }

    // ===== EXIT CONFIRMATION =====

    public void ShowExitConfirmation()
    {
        if (isAnimating)
            return;

        currentState = MenuState.ExitConfirm;
        StartCoroutine(ShowExitConfirmAnimation());
    }

    public void CancelExit()
    {
        if (isAnimating)
            return;

        currentState = MenuState.Main;
        StartCoroutine(HideExitConfirmAnimation());
    }

    public void ConfirmExit()
    {
        Debug.Log("[MenuManager] Exiting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // ===== ANIMATIONS =====

    private IEnumerator ShowPanelAnimation(GameObject panel, Image overlay, RectTransform topBar,
        RectTransform bottomBar, CanvasGroup textGroup, Vector2 topStart, Vector2 topEnd,
        Vector2 bottomStart, Vector2 bottomEnd)
    {
        isAnimating = true;

        // Fade out main menu buttons first
        yield return StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, false));

        // Activate panel
        if (panel != null)
            panel.SetActive(true);

        // Reset positions and alpha
        if (topBar != null)
            topBar.anchoredPosition = topStart;

        if (bottomBar != null)
            bottomBar.anchoredPosition = bottomStart;

        if (textGroup != null)
            textGroup.alpha = 0f;

        if (overlay != null)
        {
            Color col = overlayColor;
            col.a = 0f;
            overlay.color = col;
        }

        // Fade in overlay
        yield return StartCoroutine(FadeOverlay(overlay, true));

        // Slide in bars
        yield return StartCoroutine(AnimateBars(topBar, bottomBar, topStart, topEnd, bottomStart, bottomEnd, true));

        // Wait and fade in text
        yield return new WaitForSeconds(textFadeDelay);
        yield return StartCoroutine(FadeCanvasGroup(textGroup, true));

        isAnimating = false;
    }

    private IEnumerator HidePanelAnimation(GameObject panel, Image overlay, RectTransform topBar,
        RectTransform bottomBar, CanvasGroup textGroup, Vector2 topStart, Vector2 topEnd,
        Vector2 bottomStart, Vector2 bottomEnd)
    {
        isAnimating = true;

        // Fade out text
        yield return StartCoroutine(FadeCanvasGroup(textGroup, false));

        // Slide out bars
        yield return StartCoroutine(AnimateBars(topBar, bottomBar, topStart, topEnd, bottomStart, bottomEnd, false));

        // Fade out overlay
        yield return StartCoroutine(FadeOverlay(overlay, false));

        // Deactivate panel
        if (panel != null)
            panel.SetActive(false);

        // Fade in main menu buttons again
        yield return StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, true));

        isAnimating = false;
    }

    private IEnumerator ShowExitConfirmAnimation()
    {
        isAnimating = true;

        // Fade out main menu buttons first
        yield return StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, false));

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(true);

        if (exitCanvasGroup != null)
            exitCanvasGroup.alpha = 0f;

        if (exitOverlay != null)
        {
            Color col = overlayColor;
            col.a = 0f;
            exitOverlay.color = col;
        }

        // Fade in overlay
        yield return StartCoroutine(FadeOverlay(exitOverlay, true));

        // Fade in confirmation dialog
        yield return StartCoroutine(FadeCanvasGroup(exitCanvasGroup, true));

        isAnimating = false;
    }

    private IEnumerator HideExitConfirmAnimation()
    {
        isAnimating = true;

        // Fade out dialog
        yield return StartCoroutine(FadeCanvasGroup(exitCanvasGroup, false));

        // Fade out overlay
        yield return StartCoroutine(FadeOverlay(exitOverlay, false));

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        // Fade in main menu buttons again
        yield return StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, true));

        isAnimating = false;
    }

    private IEnumerator AnimateBars(RectTransform topBar, RectTransform bottomBar,
        Vector2 topStart, Vector2 topEnd, Vector2 bottomStart, Vector2 bottomEnd, bool slideIn)
    {
        float elapsed = 0f;

        while (elapsed < barAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / barAnimationDuration);
            float curveValue = barAnimationCurve.Evaluate(t);

            if (slideIn)
            {
                if (topBar != null)
                    topBar.anchoredPosition = Vector2.Lerp(topStart, topEnd, curveValue);

                if (bottomBar != null)
                    bottomBar.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, curveValue);
            }
            else
            {
                if (topBar != null)
                    topBar.anchoredPosition = Vector2.Lerp(topEnd, topStart, curveValue);

                if (bottomBar != null)
                    bottomBar.anchoredPosition = Vector2.Lerp(bottomEnd, bottomStart, curveValue);
            }

            yield return null;
        }

        // Set final positions
        if (slideIn)
        {
            if (topBar != null)
                topBar.anchoredPosition = topEnd;
            if (bottomBar != null)
                bottomBar.anchoredPosition = bottomEnd;
        }
        else
        {
            if (topBar != null)
                topBar.anchoredPosition = topStart;
            if (bottomBar != null)
                bottomBar.anchoredPosition = bottomStart;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, bool fadeIn)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;

        while (elapsed < textFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / textFadeDuration);
            float curveValue = textFadeCurve.Evaluate(t);

            group.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);

            yield return null;
        }

        group.alpha = endAlpha;
    }

    private IEnumerator FadeOverlay(Image overlay, bool fadeIn)
    {
        if (overlay == null)
            yield break;

        float elapsed = 0f;
        Color startColor = overlayColor;
        Color endColor = overlayColor;

        startColor.a = fadeIn ? 0f : overlayColor.a;
        endColor.a = fadeIn ? overlayColor.a : 0f;

        while (elapsed < overlayFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / overlayFadeDuration);

            overlay.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        overlay.color = endColor;
    }
}