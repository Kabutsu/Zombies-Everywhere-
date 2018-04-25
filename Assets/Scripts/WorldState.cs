using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldState : MonoBehaviour {

    public static int zombiesEncountered = 0;
    public static int completedConversations = 0;
    public static string generalLocation = "";
    public static string specificLocation = "";
    public static bool canChat;
    public static bool chatting;
    public static GameObject chattingTo;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}

    //getter & setter
    public static int GetZombiesEncountered() { return zombiesEncountered; }
    public static void ZombieEncountered()
    {
        zombiesEncountered++;
        QuestManager.Query("ZombieDefeated", "Zombie", "Zombie"); //may advance a quest
    }

    //getter and setter
    public static int getCompletedConversations() { return completedConversations; }
    public static void ConversationFinished()
    {
        completedConversations++;
        chatting = false;
        QuestManager.Query("chatEnd", "chattingTo", chattingTo); //may advance a quest
    }

    //setter
    public static void SetPlayerGeneralLocation(string location) {
        generalLocation = (location == "" ? "path" : location);
        if(generalLocation != "path") QuestManager.Query("locationChanged", "generalLocation", generalLocation); //may advance a quest
    }

    //setter
    public static void SetPlayerSpecificLocation(string location)
    {
        specificLocation = (location == "" ? generalLocation : location);
        QuestManager.Query("locationChanged", "specificLocation", specificLocation); //may advance a quest
    }

    //getters...
    public static string GetGeneralLocation() { return generalLocation; }
    public static string GetSpecificLocation() { return specificLocation; }

    public static bool CanChat() { return canChat; }

    //setter
    public static void CanChat(bool set)
    {
        canChat = set;
        if (set == false)
        {
            chattingTo = null;
            chatting = false;
        }
    }

    //setter
    public static void CanChat(GameObject chatToThis) {
        canChat = true;
        chattingTo = chatToThis;
        try
        {
            if(chatToThis.tag != "Pickup") QuestManager.Query("chatStart", "chattingTo", chattingTo); //this pick-up might advance a quest
        } catch (KeyNotFoundException) { /*Means Quest is completed, so do nothing*/ }
    }

    //setter
    public static void Chatting(bool set)
    {
        chatting = set;
    }
}
