﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialogue : MonoBehaviour {

    ArrayList currentQuest;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //display text on screen
    public void ShowText(string message) {
        UnityEngine.UI.Text guiText = this.GetComponent<UnityEngine.UI.Text>();
        guiText.text = message;
        if (!guiText.enabled) guiText.enabled = true;
    }

    //clear the text from the screen
    public void ClearText() {
        this.GetComponent<UnityEngine.UI.Text>().enabled = false;
    }
}
