using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using System.Linq;

public class ItemsFirstVersion : MonoBehaviour
{
    enum Item{
        Flashlight,
        Bunny,
        Knife,
        Obsidian,
        EmergencyFood
    }

    [SerializeField] private Item item;
    [SerializeField] private int maxItem = 3;
    [SerializeField] private int totalItems = 0;
    [SerializeField] private List<Item> items = new List<Item>();
    /*
     * Methode, um Items aufzurufen und anzeigen zu lassen
     */
    public void showItem()
    {
        if (Input.GetKey(KeyCode.T))
        {
            if (totalItems == 0)
            {
                Debug.Log("Wow, keine Items, du Idiot!!");
                items = Item.GetValues(typeof(Item)).Cast<Item>().ToList();
            }
        }
    }

}
