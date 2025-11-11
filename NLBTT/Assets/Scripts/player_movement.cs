using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2 : MonoBehaviour
{
    private Player2 player;

    void Start()
    {
        player = GetComponent<Player2>();
        
        // Maus sichtbar und entsperrt lassen
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
        // WASD Steuerung
        if (Keyboard.current.wKey.wasPressedThisFrame)
            TryMove(Vector3.forward);
        if (Keyboard.current.sKey.wasPressedThisFrame)
            TryMove(Vector3.back);
        if (Keyboard.current.aKey.wasPressedThisFrame)
            TryMove(Vector3.left);
        if (Keyboard.current.dKey.wasPressedThisFrame)
            TryMove(Vector3.right);
        
        // Pfeiltasten als Alternative
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            TryMove(Vector3.forward);
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            TryMove(Vector3.back);
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            TryMove(Vector3.left);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            TryMove(Vector3.right);
    }

    void HandleControllerInput()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        // D-Pad Steuerung
        if (pad.dpad.up.wasPressedThisFrame) 
            TryMove(Vector3.forward);
        if (pad.dpad.down.wasPressedThisFrame) 
            TryMove(Vector3.back);
        if (pad.dpad.left.wasPressedThisFrame) 
            TryMove(Vector3.left);
        if (pad.dpad.right.wasPressedThisFrame) 
            TryMove(Vector3.right);
        
        // Linker Stick (mit Deadzone)
        Vector2 leftStick = pad.leftStick.ReadValue();
        if (leftStick.magnitude > 0.5f)
        {
            if (Mathf.Abs(leftStick.x) > Mathf.Abs(leftStick.y))
            {
                if (leftStick.x > 0)
                    TryMove(Vector3.right);
                else
                    TryMove(Vector3.left);
            }
            else
            {
                if (leftStick.y > 0)
                    TryMove(Vector3.forward);
                else
                    TryMove(Vector3.back);
            }
        }
    }

    void TryMove(Vector3 direction)
    {
        if (player == null || player.currentCard == null) return;

        // Berechne Zielposition
        Vector3 newPos = player.currentCard.transform.position + direction * 2f;
        
        // Finde Karte an dieser Position
        foreach (Card c in FindObjectsOfType<Card>())
        {
            if (Vector3.Distance(c.transform.position, newPos) < 0.1f)
            {
                player.MoveTo(c);
                return;
            }
        }
        
        Debug.Log("Keine Karte in dieser Richtung!");
    }
}