using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityStandardAssets.Characters.FirstPerson;

public class ChatManager : MonoBehaviour {

    public UnityEngine.UI.Text uiText;
    private Stack<string> conversation;
    private string currentLine;

    bool chatting = false;
    bool canChat = false;
    private bool chatPaused = false;

    private PickupManager pickupManager;

	// Use this for initialization
	void Start () {
        pickupManager = GetComponent<PickupManager>(); //get the pickupManager if there is one
    }
	
	// Update is called once per frame
	void Update () {
        //Check to see if the player can chat with this character, and start/continue the conversation when called to
		if(canChat) {
            if(!chatting && Input.GetButtonDown("Fire1") && !ChatPaused()) {
                WorldState.CanChat(gameObject);
                StartChat();
            } else if (chatting && Input.GetButtonDown("Fire1") && !ChatPaused()) {
                ContinueChat();
            }
        }
	}

    //Give the player the option to chat and display text to notify them
    public void GiveChatOption() {
        canChat = true;
        uiText.GetComponent<Dialogue>().ShowText("[Click to "
            + (gameObject.tag == "Friendly" ? "chat to" : (gameObject.tag == "Pickup" ? "pick up" : "interact with"))
            + " the "
            + (gameObject.name[gameObject.name.Length - 1] == ')' ? gameObject.name.Substring(0, gameObject.name.Length - 4) : gameObject.name) + "]");
    }

    //pass the stack that will form the conversation
    public void SetConversation(Stack<string> conv)
    {
        conversation = new Stack<string>();
        while (conv.Count != 0)
            conversation.Push(conv.Pop());
    }

    //Take away the option to chat from the player
    public void TakeChatOption() {
        canChat = false;
        ExitChat();
    }

    //Start the dialogue off
    public void StartChat()
    {
        try
        {
            currentLine = conversation.Peek();
            GameObject.Find("FPSController").GetComponent<FirstPersonController>().enabled = false;
            chatting = true;
            canChat = true;
            WorldState.Chatting(true);
            Chat();
        } catch (InvalidOperationException)
        {
            TakeChatOption();
        }
    }

    //Get the next bit of dialogue
    private void ContinueChat() {
        conversation.Pop();
        try
        {
            currentLine = conversation.Peek();
            Chat();
        } catch (InvalidOperationException)
        {
            ExitChat();
        }
    }

    //Display the dialogue on-screen, or remove it if the conversation is over
    private void Chat() {
        try {
            uiText.GetComponent<Dialogue>().ShowText(currentLine);
        } catch (InvalidOperationException) {
            ExitChat();
        }
    }

    //Remove the conversation from the screen
    private void ExitChat() {
        GameObject.Find("FPSController").GetComponent<FirstPersonController>().enabled = true;
        chatting = false;
        uiText.GetComponent<Dialogue>().ClearText();
        WorldState.Chatting(false);
        if (canChat)
        {
            if(pickupManager == null)
            {
                WorldState.ConversationFinished();
            } else
            {
                pickupManager.Pickup();
            }
        }
    }

    //getters & setters for pausing the chat when game is paused/resumed
    public void PauseChat()
    {
        chatPaused = true;
        if (gameObject.GetComponent<Animator>() != null) gameObject.GetComponent<Animator>().enabled = false; //pause animation when game paused
    }

    public void ResumeChat()
    {
        chatPaused = false;
        if (gameObject.GetComponent<Animator>() != null) gameObject.GetComponent<Animator>().enabled = true; //resume animation when game resumed
    }

    private bool ChatPaused()
    {
        return chatPaused;
    }
}
