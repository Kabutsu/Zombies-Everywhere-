using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationManager : MonoBehaviour {

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private string locationName;

    [SerializeField]
    private bool isGeneralLocation;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //update the player's location to this location
    private void OnTriggerEnter(Collider other)
    {
        if(player.GetComponent<Collider>().Equals(other))
        {
            if (isGeneralLocation) { WorldState.SetPlayerGeneralLocation(locationName); }
            else WorldState.SetPlayerSpecificLocation(locationName);
        }
    }

    //reset the player's location to default when they leave here
    private void OnTriggerExit(Collider other)
    {
        if (player.GetComponent<Collider>().Equals(other))
        {
            if (isGeneralLocation) { WorldState.SetPlayerGeneralLocation(""); }
            else WorldState.SetPlayerSpecificLocation("");
        }
    }
}
