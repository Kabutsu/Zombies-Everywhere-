using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupManager : MonoBehaviour {

    [SerializeField]
    private bool destroyAfterPickup;

    [SerializeField]
    private string itemName;

    [SerializeField]
    private int numberOfItems;

    private const float STANDARD_HEAL_AMOUNT = 12.5f;

    private bool canPickUp = true;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

    }

    public void Pickup()
    {
        canPickUp = false;

        if(itemName == "healthPack")
        {
            Inventory.Heal(Mathf.RoundToInt(STANDARD_HEAL_AMOUNT * numberOfItems));
        } else
        {
            Inventory.PickUp(itemName, numberOfItems);
        }
        QuestManager.Query("ItemsChanged", "Pickup", new Dictionary<string, int>() { { itemName, numberOfItems } });

        if (destroyAfterPickup)
        {
            GameObject.Find("FPSController").GetComponent<AudioSource>().volume = 0;
            StartCoroutine(ManageAudio());
            GameObject.Find("FPSController").GetComponent<Inventory>().ShowCrosshairs();
        }
    }

    IEnumerator ManageAudio()
    {
        yield return new WaitForSeconds(0.3f);
        GameObject.Find("FPSController").GetComponent<AudioSource>().volume = 1;
        yield return null;

        foreach (Collider col in gameObject.GetComponents<Collider>())
            Destroy(col);
        Destroy(gameObject);
    }

    public Stack<string> PickupMessage()
    {
        return new Stack<string>(new List<string>() {
            (itemName == "healthPack" ? "The health pack restored your health!\n[Click to continue]"
            : "You picked up " + numberOfItems + " " + (numberOfItems > 1 ? MoreReadable(itemName) + "s" : MoreReadable(itemName)) + "!\n[Click to continue]")
        });
    }

    private string MoreReadable(string varName)
    {
        string readableVarName = varName;

        int pos = 0;
        foreach (char character in varName)
        {
            if (Char.IsUpper(character))
            {
                readableVarName = readableVarName.Substring(0, pos) + " " + readableVarName.Substring(pos);
                pos++;
            }
            pos++;
        }

        return readableVarName.Substring(0, 1).ToUpper() + readableVarName.Substring(1);
    }

    public string ItemName()
    {
        return MoreReadable(ItemNameAsVariable());
    }

    public string ItemNameAsVariable()
    {
        return itemName;
    }

    public bool CanPickUp()
    {
        return canPickUp;
    }
}
