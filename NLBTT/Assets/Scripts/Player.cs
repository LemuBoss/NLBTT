using UnityEngine;
using static PlayerMovement;

public class Player : MonoBehaviour
{
    int hunger;
    int stamina;
    int health;
    int bloodpoints;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hunger = 20;
        stamina = 5;
        health = 5;
        bloodpoints = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
