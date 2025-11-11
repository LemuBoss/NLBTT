/*using UnityEngine;
using UnityEngine.InputSystem; // Wichtig für neuen Input System Support
using static Player2;

public class PlayerMovement : MonoBehaviour
{
    /*public float moveDistance = 1f; // Schrittweite pro Bewegung
    private bool isChoosingDirection = false;
    private Vector3 targetPosition;

    private void Start()
    {
        // Maus NICHT sperren
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
        HandleControllerInput();
    }

    // -------------------------------
    // 🖱️ Maussteuerung
    // -------------------------------
    void HandleMouseInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Spieler wählt eine Richtung (Beispiel: über UI oder Position auf Karte)
            ChooseDirection();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelAction();
        }
    }

    // -------------------------------
    // ⌨️ Tastatursteuerung
    // -------------------------------
    void HandleKeyboardInput()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            OpenItems();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenMap();
        }
    }

    // -------------------------------
    // 🎮 Controller (PlayStation-Layout)
    // -------------------------------
    void HandleControllerInput()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return; // Kein Controller verbunden

        // Bewegung mit Steuerkreuz
        if (pad.dpad.up.wasPressedThisFrame) Move(Vector3.forward);
        if (pad.dpad.down.wasPressedThisFrame) Move(Vector3.back);
        if (pad.dpad.left.wasPressedThisFrame) Move(Vector3.left);
        if (pad.dpad.right.wasPressedThisFrame) Move(Vector3.right);

        // Aktionen
        if (pad.buttonEast.wasPressedThisFrame) CancelAction(); // Kreis
        if (pad.buttonNorth.wasPressedThisFrame) OpenMap();    // Dreieck
        if (pad.buttonWest.wasPressedThisFrame) OpenItems();    // Viereck
        if (pad.buttonSouth.wasPressedThisFrame) StartAction();
    }

    // -------------------------------
    // 🔁 Bewegungslogik
    // -------------------------------
    void ChooseDirection()
    {
        // Beispielhaft: Spieler klickt und es bewegt sich eine Zelle nach oben
        // Später kannst du hier deine Richtungs-Auswahl per UI einbauen.
        Move(Vector3.forward);
        //Player health = health - 1;
    }

    void Move(Vector3 direction)
    {
        targetPosition = transform.position + direction * moveDistance;
        transform.position = targetPosition;
        Debug.Log("Moved to: " + targetPosition);
    }

    void CancelAction()
    {
        Debug.Log("Aktion abgebrochen");
        isChoosingDirection = false;
    }

    void OpenItems()
    {
        Debug.Log("Items geöffnet (Q oder Viereck)");
    }

    void OpenMap()
    {
        Debug.Log("Karte geöffnet (E oder Dreieck)");
    }

    void StartAction()
    {
        Debug.Log("Aktion gestartet");
    }


    private Player player;

    private void Start()
    {
        player = GetComponent<Player>();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleControllerInput();
    }

    void HandleKeyboardInput()
    {
        if (Keyboard.current.wKey.wasPressedThisFrame)
            TryMove(Vector3.forward);
        if (Keyboard.current.sKey.wasPressedThisFrame)
            TryMove(Vector3.back);
        if (Keyboard.current.aKey.wasPressedThisFrame)
            TryMove(Vector3.left);
        if (Keyboard.current.dKey.wasPressedThisFrame)
            TryMove(Vector3.right);
    }

    void HandleControllerInput()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        if (pad.dpad.up.wasPressedThisFrame) TryMove(Vector3.forward);
        if (pad.dpad.down.wasPressedThisFrame) TryMove(Vector3.back);
        if (pad.dpad.left.wasPressedThisFrame) TryMove(Vector3.left);
        if (pad.dpad.right.wasPressedThisFrame) TryMove(Vector3.right);
    }

    void TryMove(Vector3 direction)
    {
        if (player == null || player.currentCard == null) return;

        Vector3 newPos = player.currentCard.transform.position + direction * 2f;
        foreach (Card c in FindObjectsOfType<Card>())
        {
            if (Vector3.Distance(c.transform.position, newPos) < 0.1f)
            {
                player.MoveTo(c);
                return;
            }
        }
    }


}*/
