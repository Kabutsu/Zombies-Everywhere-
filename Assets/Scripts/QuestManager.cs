using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class QuestManager : MonoBehaviour {

    public WorldState worldState;

    private static Dictionary<string, Quest> quests;
    private static List<string> questsToRemove;

    [SerializeField]
    private TextAsset jsonFile;

	// Use this for initialization
	void Start ()
    {
        worldState = new WorldState();
        questsToRemove = new List<string>();
        FillQuests();
	}
	
	// Update is called once per frame
	void Update () {
        
    }

    //WorldState updates and queries whether that should change anything in QuestManager
    public static void Query(string situation, string varName, System.Object value)
    {
        //set "value" to a quest's "thisGameObject" or "thisItem" if possible for easy setting/comparing of runtime variables
        try
        {
            if (value.GetType() == typeof(GameObject))
            {
                foreach (Quest quest in quests.Values.ToArray()) quest.ThisGameObject((GameObject)value);
            }
            else if (value.GetType() == typeof(Dictionary<string, int>))
            {
                foreach (Quest quest in quests.Values.ToArray()) quest.ThisItem(((Dictionary<string, int>)value).First().Key);
            }
        } catch (NullReferenceException) { }

        //based on what has just happened, effects might be different, hence big ol' if-statement
        switch (situation)
        {
            //return dialogue for a conversation. Return the dialogue from the first quest matched; if none matched, return some default dialogue
            case "chatStart":
                bool conversationSet = false;
                try
                {
                    //go through all the quests
                    foreach (string questName in quests.Keys.ToArray())
                    {
                        //only advance if a previous quest hasn't already been matched
                        if (!conversationSet)
                        {
                            Quest revertToThis = new Quest(quests[questName]); //useful for when trying to skip quest steps
                            
                            bool checkForObjective = true;
                            bool exit = false;
                            //get the first available quest step that is satisfied by the current action, if any
                            do
                            {
                                if (quests[questName].CurrentStep().IsCompleted()) //current step completed but preconditions of next step possibly not met
                                {
                                    checkForObjective = false;
                                    exit = true;
                                    if (quests[questName].CurrentStep().Equals(quests[questName].GetNextStep(quests[questName].Steps())) == false) checkForObjective = true; //if preconditions now met, move on to next step and check for completion
                                }
                                
                                //check to see if this action satisfies the current step
                                if(checkForObjective)
                                {
                                    quests[questName].CurrentStep().CheckObjective(value);
                                    
                                    //if so, set the conversation to what is required
                                    if (quests[questName].CurrentStep().IsCompleted())
                                    {
                                        conversationSet = true;
                                        exit = true;
                                        ((GameObject)value).GetComponent<ChatManager>().SetConversation(quests[questName].CurrentStep().GetDialogue());
                                        quests[questName].CurrentStep().ApplyResult(); //apply the results from the quest step
                                    }
                                    //otherwise, try and skip ahead to the first satisfied quest step, if possible
                                    else if (quests[questName].CurrentStep().IsOptional())
                                    {
                                        Quest.GoFetch temp = (Quest.GoFetch)quests[questName].CurrentStep();
                                        if (temp.Equals((Quest.GoFetch)quests[questName].GetNextStep(quests[questName].Steps())))
                                        {
                                            quests[questName] = revertToThis;
                                            quests[questName].CurrentStep().CheckObjective(value);
                                            exit = true;
                                        }
                                    }
                                    else
                                    {
                                        //if no steps satisfied, this quest isn't satisfied by this action, so just revert back to how it was
                                        quests[questName] = revertToThis;
                                        quests[questName].CurrentStep().CheckObjective(value);
                                        exit = true;
                                    }
                                }
                            } while (exit == false);
                        }
                    }
                } catch (KeyNotFoundException) { /*No Quests to cycle through, so just skip onto default conversations*/ }
                //set default conversation if you're not required to talk to this object for a quest
                if(!conversationSet)
                {
                    switch (((GameObject)value).tag)
                    {
                        case "Friendly":
                            if(((GameObject)value).name.Substring(0, 7) == "soldier" || ((GameObject)value).name == "Officer" || ((GameObject)value).name == "General")
                            {
                                ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "\"Nothing to see here.\"\n\"Just the marines doing their job,\nclearing up this mess.\"\n[Click to continue]" }));
                            }
                            else ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "\"Stay back!\"\n\"Oh, sorry. Thought you were a zombie.\"\n\"The world is awfully scary now.\"\n[Click to continue]" }));
                            break;
                        case "Idol":
                            try
                            {
                                //other than for the strictly structured quest of course... then we need to set the dialogue according to where the player is in the quest
                                if (quests["The Ancient Druid Tome"].GetVar("StepFour").Equals(true))
                                {
                                    ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "Just the red totem left to interact with!\n[Click to continue]" }));
                                }
                                else if (quests["The Ancient Druid Tome"].GetVar("StepThree").Equals(true))
                                {
                                    if(((GameObject)value).name == "Runestone")
                                    {
                                        ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "This one sits quietly now the tome\nhas been read in front of it.\nThe blue one should be next!\n[Click to continue]" }));
                                    } else
                                    {
                                        ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "It doesn't seem to be responding to the words.\nThe scary man said to go \"yellow, blue, red\" -\nmaybe the blue idol will respond?\n[Click to continue]" }));
                                    }
                                }
                                else if (quests["The Ancient Druid Tome"].GetVar("stepTwoCompleted").Equals(true))
                                {
                                    ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "Remember - read the words from the druid tome\nnext to each runestone in the order \"yellow, blue, red\"!\n[Click to continue]" }));
                                }
                                else if (quests["The Ancient Druid Tome"].GetVar("stepOneCompleted").Equals(true))
                                {
                                    ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "Maybe that druid tome holds the runestone's secrets...\nIf only someone around here knew how?\n[Click to continue]" }));
                                }
                                else ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "There's some sort of aura about the runestone...\nInteresting.\n[Click to continue]" }));
                            } catch (KeyNotFoundException)
                            {
                                ((GameObject)value).GetComponent<ChatManager>().SetConversation(new Stack<string>(new string[] { "The runestone sits silently.\n[Click to continue]" }));
                            }
                            break;
                        default:
                            break;
                    }
                }
                break;

                //handle location queries
            case "locationChanged":
                foreach(string questName in quests.Keys.ToArray())
                {
                    //update runtime variables
                    if(varName == "generalLocation" && quests[questName].HasVar("generalLocation"))
                    {
                        quests[questName].SetVar("generalLocation", (string)value);
                    } else if (varName == "specificLocation" && quests[questName].HasVar("specificLocation"))
                    {
                        quests[questName].SetVar("specificLocation", (string)value);
                    } else if (quests[questName].HasVar((string)value + "Visited"))
                    {
                        quests[questName].SetVar((string)value + "Visited", true);
                    }

                    //all of this is practically the same as for conversations above

                    Quest revertToThis = new Quest(quests[questName]);

                    bool checkForObjective = true;
                    bool exit = false;
                    do
                    {

                        if (quests[questName].CurrentStep().IsCompleted()) //current step completed but preconditions of next step possibly not met
                        {
                            checkForObjective = false;
                            exit = true;
                            if (quests[questName].CurrentStep().Equals(quests[questName].GetNextStep(quests[questName].Steps())) == false) checkForObjective = true; //if preconditions now met, move on to next step and check for completion
                        }

                        if (checkForObjective)
                        {
                            quests[questName].CurrentStep().CheckObjective(value);
                            
                            //if this action satisfies a step..
                            if (quests[questName].CurrentStep().IsCompleted())
                            {
                                exit = true;
                                quests[questName].CurrentStep().ApplyResult(); //apply the results for the quest step
                            }
                            //try and find first satisfied step..
                            else if (quests[questName].CurrentStep().IsOptional())
                            {
                                Quest.GoFetch temp = (Quest.GoFetch)quests[questName].CurrentStep();
                                if (temp.Equals((Quest.GoFetch)quests[questName].GetNextStep(quests[questName].Steps())))
                                {
                                    quests[questName] = revertToThis;
                                    quests[questName].CurrentStep().CheckObjective(value);
                                    exit = true;
                                }
                            }
                            else
                            {
                                //no steps satisfied in this quest, so revert
                                quests[questName] = revertToThis;
                                quests[questName].CurrentStep().CheckObjective(value);
                                exit = true;
                            }
                        }
                    } while (exit == false);
                    
                }
                break;

                //when the player picks up or drops an item
            case "ItemsChanged":
                KeyValuePair<string, int> itemsChanged = ((Dictionary<string, int>)value).First();
                foreach(string questName in quests.Keys.ToArray())
                {
                    //update runtime variables
                    if(quests[questName].HasVar(itemsChanged.Key)) {
                        quests[questName].SetVar(itemsChanged.Key, Inventory.CarryingHowMany(itemsChanged.Key));
                    }

                    //getting the drill now...

                    Quest revertToThis = new Quest(quests[questName]);
                    
                    bool checkForObjective = true;
                    bool exit = false;
                    do
                    {

                        if (quests[questName].CurrentStep().IsCompleted()) //current step completed but preconditions of next step possibly not met
                        {
                            checkForObjective = false;
                            exit = true;
                            if (quests[questName].CurrentStep().Equals(quests[questName].GetNextStep(quests[questName].Steps())) == false) checkForObjective = true; //if preconditions now met, move on to next step and check for completion
                        }

                        if (checkForObjective)
                        {
                            quests[questName].CurrentStep().CheckObjective(itemsChanged);

                            //if this step satisfied...
                            if (quests[questName].CurrentStep().IsCompleted() && quests[questName].CurrentStep().PreconditionsMet())
                            {
                                exit = true;
                                if (questName == "Your Typical Go-Fetch Quest") //yeah so this quest was super buggy
                                {
                                    GameObject.Find("FPSController").GetComponent<ChatManager>().SetConversation(quests["Your Typical Go-Fetch Quest"].CurrentStep().GetDialogue());
                                    GameObject.Find("FPSController").GetComponent<ChatManager>().StartChat();
                                }
                                quests[questName].CurrentStep().ApplyResult(); //apply the result
                            }
                            else if (quests[questName].CurrentStep().IsCompleted() && quests[questName].CurrentStep().PreconditionsMet() == false)
                            {
                                quests[questName].CurrentStep().Completed(false);
                            }

                            //find the next satisfied step..
                            if (quests[questName].CurrentStep().IsCompleted() == false && quests[questName].CurrentStep().IsOptional() && exit == false)
                            {
                                Quest.GoFetch temp = (Quest.GoFetch)quests[questName].CurrentStep();
                                if (temp.Equals((Quest.GoFetch)quests[questName].GetNextStep(quests[questName].Steps())))
                                {
                                    quests[questName] = revertToThis;
                                    quests[questName].CurrentStep().CheckObjective(itemsChanged);
                                    exit = true;
                                }
                            }
                            //revert...
                            else if (quests[questName].CurrentStep().IsCompleted() == false && exit == false)
                            {
                                quests[questName] = revertToThis;
                                quests[questName].CurrentStep().CheckObjective(itemsChanged);
                                exit = true;
                            }
                        }
                    } while (exit == false);
                    
                }
                break;

                //for shooting a zombie
            case "ZombieDefeated":
                foreach(string questName in quests.Keys.ToArray())
                {
                    //update runtime variables...
                    if(quests[questName].HasVar("zombiesEncountered"))
                    {
                        quests[questName].SetVar("zombiesEncountered", WorldState.GetZombiesEncountered());
                    }

                    //yep. same code as above again

                    Quest revertToThis = new Quest(quests[questName]);

                    bool checkForObjective = true;
                    bool exit = false;
                    do
                    {

                        if (quests[questName].CurrentStep().IsCompleted()) //current step completed but preconditions of next step possibly not met
                        {
                            checkForObjective = false;
                            exit = true;
                            if (quests[questName].CurrentStep().Equals(quests[questName].GetNextStep(quests[questName].Steps())) == false) checkForObjective = true; //if preconditions now met, move on to next step and check for completion
                        }

                        if (checkForObjective)
                        {
                            quests[questName].CurrentStep().CheckObjective(value);

                            //if this satisfies the step, apply results....
                            if (quests[questName].CurrentStep().IsCompleted())
                            {
                                exit = true;
                                if (questName == "Your Typical Go-Fetch Quest") //yeah this quest was buggy
                                {
                                    GameObject.Find("FPSController").GetComponent<ChatManager>().SetConversation(quests["Your Typical Go-Fetch Quest"].CurrentStep().GetDialogue());
                                    GameObject.Find("FPSController").GetComponent<ChatManager>().StartChat();
                                }
                                quests[questName].CurrentStep().ApplyResult();
                            }
                            //find first satisfied step...
                            else if (quests[questName].CurrentStep().IsOptional())
                            {
                                Quest.GoFetch temp = (Quest.GoFetch)quests[questName].CurrentStep();
                                if (temp.Equals((Quest.GoFetch)quests[questName].GetNextStep(quests[questName].Steps())))
                                {
                                    quests[questName] = revertToThis;
                                    quests[questName].CurrentStep().CheckObjective(value);
                                    exit = true;
                                }
                            }
                            else
                            {
                                //revert...
                                quests[questName] = revertToThis;
                                quests[questName].CurrentStep().CheckObjective(value);
                                exit = true;
                            }
                        }
                    } while (exit == false);

                    //Debug.Log("Now on \"" + quests[questName].CurrentStep().GetDescription() + "\" for \"" + questName + "\" after Query(" + situation + ", " + varName + ", " + value.ToString() + ").");
                }
                break;

            case "chatEnd":
                //all quests end with a conversation. quests are removed before this conversation ends. if there are no quests when a conversation ends - game won!
                if (quests.Count == 0) GameObject.Find("FPSController").GetComponent<Inventory>().GameOver("You completed all the quests!\n\nWell done!", true);
                break;
            default:

                break;
        }

        RemoveQuests(); //remove any completed quests from the list
    }

    //read the quests in from JSON file
    private void FillQuests() {
        
        quests = new Dictionary<string, Quest>();
        
        //turn serialized fields like Arrays into non-serializable fields like Dictionaries
        //from this, form the Dictionary of Quests
        foreach(SerializedQuest squest in JsonUtility.FromJson<SerializedQuestList>(jsonFile.text).quests)
        {
            //work out the variables
            Dictionary<string, System.Object> vars = new Dictionary<string, object>();
            int noOfVariables = squest.variables.names.Length;
            if(noOfVariables > 0)
            {
                for (int i = 0; i < noOfVariables; i++)
                {
                    if (squest.variables.values[i].ToLower() == "true")
                    {
                        vars.Add(squest.variables.names[i], true);
                    } else if (squest.variables.values[i].ToLower() == "false")
                    {
                        vars.Add(squest.variables.names[i], false);
                    } else if (squest.variables.values[i] == "")
                    {
                        vars.Add(squest.variables.names[i], null);
                    } else
                    {
                        try { vars.Add(squest.variables.names[i], Convert.ToInt32(squest.variables.values[i])); }
                        catch (FormatException) { vars.Add(squest.variables.names[i], squest.variables.values[i]); }
                    }
                }
            }
            

            Quest newQuest = new Quest(this, squest.questName, vars); //create the basic quest

            //work out the various aspects of each step of the quest
            foreach(SerializedQuestStep step in squest.questSteps)
            {
                Dictionary<string, System.Object> skipIf = new Dictionary<string, object>();
                Dictionary<string, System.Object> preconditions = new Dictionary<string, object>();
                Dictionary<string, System.Object> result = new Dictionary<string, object>();
                
                if(step.skipIf.names.Length > 0) //turn skipIf to a Dictionary
                {
                    int noOfSkipVariables = step.skipIf.names.Length;
                    for (int i = 0; i < noOfSkipVariables; i++)
                    {
                        if (step.skipIf.values[i].ToLower() == "true")
                        {
                            skipIf.Add(step.skipIf.names[i], true);
                        }
                        else if (step.skipIf.values[i].ToLower() == "false")
                        {
                            skipIf.Add(step.skipIf.names[i], false);
                        }
                        else if (step.skipIf.values[i] == "")
                        {
                            skipIf.Add(step.skipIf.names[i], null);
                        }
                        else
                        {
                            try { skipIf.Add(step.skipIf.names[i], Convert.ToInt32(step.skipIf.values[i])); }
                            catch (FormatException) { skipIf.Add(step.skipIf.names[i], step.skipIf.values[i]); }
                        }
                    }
                }

                if (step.preconditions.names.Length > 0) //turn preconditions to a dictionary
                {
                    int noOfPreconVariables = step.preconditions.names.Length;
                    for (int i = 0; i < noOfPreconVariables; i++)
                    {
                        if (step.preconditions.values[i].ToLower() == "true")
                        {
                            preconditions.Add(step.preconditions.names[i], true);
                        }
                        else if (step.preconditions.values[i].ToLower() == "false")
                        {
                            preconditions.Add(step.preconditions.names[i], false);
                        }
                        else if (step.preconditions.values[i] == "")
                        {
                            preconditions.Add(step.preconditions.names[i], null);
                        }
                        else
                        {
                            try { preconditions.Add(step.preconditions.names[i], Convert.ToInt32(step.preconditions.values[i])); }
                            catch (FormatException) { preconditions.Add(step.preconditions.names[i], step.preconditions.values[i]); }
                        }
                    }
                }

                if(step.result.names.Length > 0) //turn result to a dictionary
                {
                    int noOfResultVariables = step.result.names.Length;
                    for (int i = 0; i < noOfResultVariables; i++)
                    {
                        if (step.result.values[i].ToLower() == "true")
                        {
                            result.Add(step.result.names[i], true);
                        }
                        else if (step.result.values[i].ToLower() == "false")
                        {
                            result.Add(step.result.names[i], false);
                        }
                        else if (step.result.values[i] == "")
                        {
                            result.Add(step.result.names[i], null);
                        }
                        else
                        {
                            try { result.Add(step.result.names[i], Convert.ToInt32(step.result.values[i])); }
                            catch (FormatException) { result.Add(step.result.names[i], step.result.values[i]); }
                        }
                    }
                }

                //get the other variables
                string anyTag = "";
                string anyDescription = "";
                string findWhat = "";

                //ugh... "Your Typical 'Go-Fetch' Quest" required this monstrosity.
                //sets the tag the player has to find to a random game object or set of game objects
                if(step.tag == "Anything")
                {
                    //these three are nice.. just get the player to find a friendly NPC, pick-up, or shoot any zombie
                    int whatKindOfRandom = UnityEngine.Random.Range(0, 100);
                    if(whatKindOfRandom <= 40)
                    {
                        anyTag = "Friendly";
                        anyDescription = step.description + "chat to a friendlyNPC";
                        findWhat = "chat to some random survivor, they might know something";
                    } else if (whatKindOfRandom <= 80)
                    {
                        anyTag = "Pickup";
                        anyDescription = step.description + "pick up an object";
                        findWhat = "find something interesting to pick up, might be useful";
                    } else if (whatKindOfRandom <= 85)
                    {
                        anyTag = "Zombie";
                        anyDescription = step.description + "shoot a zombie";
                        findWhat = "shoot some zombies, end all this";
                    }
                    else
                    {
                        //set the tag to a specific game object, cause that can be done
                        List<GameObject> interactiveObjects = new List<GameObject>();
                        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Friendly")) interactiveObjects.Add(friendly);
                        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Zombie")) interactiveObjects.Add(zombie);
                        foreach (GameObject pickup in GameObject.FindGameObjectsWithTag("Pickup")) interactiveObjects.Add(pickup);

                        //select a random interactive object
                        anyTag = interactiveObjects.ElementAt(UnityEngine.Random.Range(0, interactiveObjects.Count)).name;

                        //now we need to set the developer-level description for that
                        switch (GameObject.Find(anyTag).tag)
                        {
                            case "Friendly":
                                anyDescription = step.description + "chat to the " + anyTag;
                                findWhat = "chat to a " + (anyTag[anyTag.Length - 1] == ')' ? anyTag.Substring(0, anyTag.Length - 4) : anyTag) + ", they might know something";
                                break;
                            case "Pickup":
                                anyDescription = step.description + "pick up the " + anyTag;
                                findWhat = "find something interesting to pick up, might be useful";
                                break;
                            case "Zombie":
                                anyDescription = step.description + "shoot the " + anyTag;
                                findWhat = "shoot some zombies, end all this";
                                break;
                        }

                        if(GameObject.Find(anyTag).tag == "Pickup")
                        {
                            anyTag = GameObject.Find(anyTag).GetComponent<PickupManager>().ItemName();
                        }
                    }

                    //ok now, we need to give the player clues as to what to look for next
                    //set the dialogue for the last quest step to look for the game object we've just determined
                    try
                    {
                        string dialogueString = "";
                        switch (((Quest.GoFetch)newQuest.Steps().Peek()).Tag())
                        {
                            case "Friendly":
                                dialogueString = "\"You there! I'd love you to do something for me.\"\n\"It sure would be great if you could\n" + findWhat + "!\"\n[Click to continue]";
                                break;

                            case "Pickup":
                                dialogueString = "The item has something enscribed on it.\n\"Be sure to " + findWhat + "!\"\n[Click to contiune]";
                                break;

                            case "Zombie":
                                dialogueString = "The zombie seems to be carrying a note...\n\"Be sure to " + findWhat + "!\"\n[Click to contiune]";
                                break;

                            default:
                                try
                                {
                                    switch (GameObject.Find(((Quest.GoFetch)newQuest.Steps().Peek()).Tag()).tag)
                                    {
                                        case "Friendly":
                                            dialogueString = "\"You there! I'd love you to do something for me.\"\n\"It sure would be great if you could\n" + findWhat + "\"\n[Click to continue]";
                                            break;

                                        case "Zombie":
                                            dialogueString = "The zombie seems to be carrying a note...\n\"Be sure to " + findWhat + "!\"\n[Click to contiune]";
                                            break;

                                        default:
                                            dialogueString = "\"You there! I'd love you to do something for me.\"\n\"It sure would be great if you could\n" + findWhat + "\"\n[Click to continue]";
                                            break;
                                    }
                                } catch (NullReferenceException)
                                {
                                    dialogueString = "The item has something enscribed on it.\n\"Be sure to " + findWhat + "!\"\n[Click to contiune]";
                                }
                                break;
                        }

                        newQuest.Steps().Peek().SetDialogue(new Stack<string>(new List<string>() { dialogueString }));
                    } catch (InvalidOperationException) { }
                }

                //okay that's all done... add the step to the current quest!
                newQuest.AddStep(new Quest.GoFetch((step.tag == "Anything" ? anyTag : step.tag), (step.tag == "Anything" ? anyDescription : step.description), step.generalDescription, newQuest, skipIf, preconditions, step.optional, new Stack<string>(step.dialogue), result));
            }

            //because steps are a stack, they'll currently be in reverse order. fix!
            newQuest.ReverseSteps();

            quests.Add(newQuest.GetName(), newQuest); //add the quest to the Dictionary of Quests
        }

        DataLog.Print(quests["Your Typical Go-Fetch Quest"].CurrentStep().GetDescription());
        foreach (Quest.GoFetch step in quests["Your Typical Go-Fetch Quest"].Steps()) DataLog.Print(step.GetDescription());

        GameObject.Find("FPSController").GetComponent<Inventory>().InitializeQuests();
    }

    //set a quest to be removed once all effects applied
    public static void RemoveQuestAtQueryEnd(string toRemove)
    {
        questsToRemove.Add(toRemove);
    }

    //remove all the quests that need removing
    public static void RemoveQuests()
    {
        foreach(string toRemove in questsToRemove)
            quests.Remove(toRemove);

        questsToRemove = new List<string>();
        
    }

    //getter
    public static Dictionary<string, Quest> Quests()
    {
        return quests;
    }

    //getter
    public static Quest GetQuest(string name)
    {
        try
        {
            return quests[name];
        } catch (KeyNotFoundException)
        {
            return null;
        }
    }

    //get the user-friendly description of the current step of "questName"
    public static string GetGeneralDescription(string questName)
    {
        if(GetQuest(questName) == null)
        {
            return "";
        } else
        {
            return GetQuest(questName).CurrentStep().GetGeneralDescription();
        }
    }

    //handles the overall quest structure
    public class Quest
    {
        public string questName;
        public Dictionary<string, System.Object> variables;
        public Stack<QuestStep> questSteps;
        private QuestStep currentStep;
        private QuestManager belongsTo;
        private GameObject thisGameObject;
        private string thisItem;
        private bool started;

        //construct from variables
        public Quest(QuestManager belongsTo, string name, Dictionary<string, System.Object> vars)
        {
            this.belongsTo = belongsTo;
            questName = name;
            variables = vars;
            questSteps = new Stack<QuestStep>();
            thisGameObject = null;
            started = false;
        }

        //construct from an existing quest. Used in Query(...) for reverting a quest back to how it was
        public Quest(Quest toCopy)
        {
            questName = new string(toCopy.GetName().ToCharArray());
            variables = toCopy.Variables();

            questSteps = new Stack<QuestStep>();
            Stack<QuestStep> temp = new Stack<QuestStep>(toCopy.Steps());
            while (temp.Count != 0)
                questSteps.Push(temp.Pop());

            currentStep = toCopy.CurrentStep();
            belongsTo = toCopy.BelongsTo();
            thisGameObject = toCopy.ThisGameObject();
            thisItem = toCopy.ThisItem();

            currentStep.BelongsTo(toCopy);
            foreach (QuestStep step in questSteps)
                step.BelongsTo(toCopy);

            started = toCopy.Started();
        }

        //getters & setters...
        public QuestManager BelongsTo()
        {
            return belongsTo;
        }

        public string GetName()
        {
            return questName;
        }

        public bool Started()
        {
            return started;
        }

        public void Started(bool started)
        {
            this.started = started;
        }

        //add a variable to the runtime list
        public void AddVar(string name, System.Object var)
        {
            variables.Add(name, var);
        }

        //does this quest have the runtime variable called "name"?
        public bool HasVar(string name)
        {
            return variables.ContainsKey(name);
        }

        //get the value of runtime variable called "name"
        public object GetVar(string name)
        {
            return (HasVar(name) ? variables[name] : new object());
        }

        //set the value of the runtime variable called "name" to "value"
        public void SetVar(string varName, System.Object value)
        {
            if(value.GetType() == typeof(string))
            {
                if((string)value == "")
                {
                    variables[varName] = null;
                } else
                {
                    variables[varName] = (CurrentStep().ToVariableIfExists((string)value) ?? value);
                }
            } else
            {
                variables[varName] = value;
            }
        }

        //getters and setters...
        public Dictionary<string, System.Object> Variables()
        {
            return variables;
        }

        public GameObject ThisGameObject()
        {
            return thisGameObject;
        }

        public void ThisGameObject(GameObject obj)
        {
            thisGameObject = obj;
        }

        public string ThisItem()
        {
            return thisItem;
        }

        public void ThisItem(string item)
        {
            thisItem = item;
        }

        public Stack<QuestStep> Steps()
        {
            return questSteps;
        }

        //returns the popped current step
        public QuestStep CurrentStep()
        {
            return currentStep;
        }

        public void CurrentStep(QuestStep step)
        {
            currentStep = step;
        }

        //determines which step to advance to (if any) based on completion, preconditons and skip conditions
        public QuestStep GetNextStep(Stack<QuestStep> steps)
        {
            if (questName == "Your Typical Go-Fetch Quest") //this quest was super buggy... just advance to the next step for it
            {
                questSteps = steps;
                CurrentStep(questSteps.Pop());
                return CurrentStep();
            }
            else
            {
                try
                {
                    //look past the next step if it can be skipped
                    if (steps.Peek().SkipThisStep())
                    {
                        Stack<QuestStep> stepsPopped = steps;
                        stepsPopped.Pop();
                        return GetNextStep(stepsPopped);
                    }
                    //if the next step's preconditions haven't been met, go back to the current step
                    else if ((steps.Peek().PreconditionsMet()) == false)
                    {
                        return CurrentStep();
                    }

                    //found a suitable step. pop it, set it to the new current step
                    questSteps = steps;
                    CurrentStep(questSteps.Pop());
                    return CurrentStep();
                }
                catch (InvalidOperationException)
                {
                    return CurrentStep(); //if we get to the end of the questSteps stack, go back to the current step
                }
            }
        }

        //push a step onto the stack
        public void AddStep(QuestStep step)
        {
            questSteps.Push(step);
        }

        //reverse the stack
        public void ReverseSteps()
        {
            Stack<QuestStep> temp = new Stack<QuestStep>();
            while (questSteps.Count != 0)
                temp.Push(questSteps.Pop());
            questSteps = temp;

            CurrentStep(questSteps.Pop());
        }

        //handles the various steps in a quest
        public abstract class QuestStep
        {
            private string tag;
            private string description;
            private string genDescription;
            private bool completed;
            private Quest belongsTo;
            private Dictionary<string, System.Object> skipIf;
            private Dictionary<string, System.Object> preconditions;
            private bool optional;
            private Stack<string> dialogue;
            private Dictionary<string, System.Object> result; //VariableName --> Value
            private bool resultApplied;

            public abstract void CheckObjective(System.Object checkAgainst);

            public QuestStep() { }

            //constructor...
            public QuestStep(Quest belongsTo, string description, string genDescription, Dictionary<string, System.Object> skipIf, Dictionary<string, System.Object> preconditions, bool optional, Stack<string> dialogue, Dictionary<string, System.Object> result)
            {
                completed = false;
                this.belongsTo = belongsTo;
                this.description = description;
                this.genDescription = genDescription;
                this.skipIf = (skipIf ?? new Dictionary<string, object>());
                this.preconditions = (preconditions ?? new Dictionary<string, object>());
                this.optional = optional;
                this.dialogue = dialogue;
                this.result = result;
                resultApplied = false;
            }

            //getters/setters...
            public void Completed(bool comp)
            {
                completed = comp;
            }

            public bool IsCompleted()
            {
                return completed;
            }

            public bool IsOptional()
            {
                return optional;
            }

            public string GetDescription()
            {
                return description;
            }

            public string GetGeneralDescription()
            {
                return genDescription;
            }

            public Stack<string> GetDialogue()
            {
                return dialogue;
            }

            public void SetDialogue(Stack<string> words)
            {
                dialogue = words;
            }
            
            //return the results for this step
            public Dictionary<string, System.Object> GetResult()
            {
                return result;
            }

            //apply the results to the gameworld & attempt to advance to the next step
            public void ApplyResult()
            {
                if (resultApplied == false)
                {
                    //if this starts the quest, inform the Inventory so it can update its list of quests
                    if(!belongsTo.Started())
                    {
                        GameObject.Find("FPSController").GetComponent<Inventory>().QuestStarted(belongsTo.GetName());
                        belongsTo.Started(true);
                    }

                    //apply the results
                    foreach (string varName in GetResult().Keys.ToArray())
                    {
                        if (GetResult()[varName].Equals("thisGameObject")) //apply results to a game object
                        {
                            switch (varName)
                            {
                                case "destroy": //destroy it
                                    Destroy(belongsTo.ThisGameObject().gameObject);
                                    break;

                                default: //set it to a runtime variable
                                    belongsTo.variables[varName] = belongsTo.ThisGameObject();
                                    break;
                            }
                        } else if (GetResult()[varName].Equals("thisItem")) //apply results to an item game object
                        {
                            switch (varName)
                            {
                                default: //set it to a runtime variable
                                    belongsTo.SetVar(varName, belongsTo.ThisItem());
                                    break;
                            }
                            //pick up or drop an item
                        } else if(varName == "pickup" || varName == "drop")
                        {
                            resultApplied = true;
                            int lengthOfNumber = 0;
                            
                            //work out number of items to pick up/drop
                            foreach (char character in GetResult()[varName].ToString())
                            {
                                if (character == '_') { break; }
                                else lengthOfNumber++;
                            }

                            int noOfItems = Convert.ToInt32(GetResult()[varName].ToString().Substring(0, lengthOfNumber));
                            string itemName = GetResult()[varName].ToString().Substring(lengthOfNumber + 1);

                            string itemNameOriginal = itemName;

                            //pick up "noOfItems" items
                            if (varName == "pickup")
                            {
                                try
                                {
                                    itemName = (ToVariableIfExists(itemName) == null ? itemName : (string)ToVariableIfExists(itemName));
                                } catch (InvalidCastException)
                                {
                                    itemName = itemNameOriginal;
                                }

                                Inventory.PickUp(itemName, noOfItems); //tell inventory
                                Query("ItemsChanged", "Pickup", new Dictionary<string, int>() { { itemName, noOfItems } }); //does this advance a step?
                            } else
                            {
                                //drop the items
                                try
                                {
                                    itemName = (ToVariableIfExists(itemName) == null ? itemName : (string)ToVariableIfExists(itemName));
                                } catch (InvalidCastException)
                                {
                                    itemName = itemNameOriginal;
                                }

                                Inventory.Drop(itemName, noOfItems); //tell inventory
                                Query("ItemsChanged", "Drop", new Dictionary<string, int>() { { itemName, noOfItems } }); //advances a quest?
                            }
                        } else
                        {
                            belongsTo.variables[varName] = GetResult()[varName]; //set to a runtime variable
                        }
                    }

                    resultApplied = true;

                    //print to the log
                    string currentVariables = belongsTo.GetName() + " \"" + description + "\" COMPLETED ::: Variables: ";
                    foreach (KeyValuePair<string, System.Object> var in belongsTo.variables) currentVariables += (var.Key + "->" + (var.Value ?? "null") + "; ");
                    DataLog.Print(currentVariables);

                    //if one of the results was to complete the quest
                    if (belongsTo.GetVar("questFinished").Equals(true))
                    {
                        RemoveQuestAtQueryEnd(belongsTo.GetName()); //remove this quest
                        DataLog.Print("Quest \"" + belongsTo.GetName() + "\" completed!"); //print to the log
                        GameObject.Find("FPSController").GetComponent<Inventory>().QuestFinished(belongsTo.GetName()); //inform the Inventory

                        //tame all the zombies if it's the ancient druid tome what was completed
                        if (belongsTo.GetName() == "The Ancient Druid Tome")
                        {
                            foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Zombie"))
                                zombie.GetComponent<CombatManager>().MakeTame();
                        }
                    }
                    else
                    {
                        //now try and get the next queststep
                        belongsTo.GetNextStep(belongsTo.Steps());
                        DataLog.Print("Now on \"" + belongsTo.GetName() + " \"" + belongsTo.CurrentStep().GetDescription() + "\""); //log it
                    }

                    //update user-friendly description in Inventory
                    GameObject.Find("FPSController").GetComponent<Inventory>().UpdateDescription(belongsTo.GetName(), belongsTo.CurrentStep().GetGeneralDescription());
                }
            }

            //turn a string referring to a particular variable into the value of that variable
            public System.Object ToVariableIfExists(string varName)
            {
                System.Object value = null;
                try
                {
                    varName = (varName.Substring(0, 4) == "NOT_" ? varName.Substring(4) : varName);
                } catch (ArgumentOutOfRangeException) { }
                
                //if the value is a string, might be refering to a variable (e.g. thisVar = thatVar), so double-check
                if (belongsTo.HasVar(varName))
                {
                    //if this Quest has a variable called [varName], return its value, else return [varName] itself
                    value = belongsTo.GetVar(varName);
                }
                else
                {
                    //if WorldState has a variable called [varName], return its value, else return [varName] itself
                    try
                    {
                        value = belongsTo.BelongsTo().worldState.GetType().GetField(varName).GetValue(belongsTo.BelongsTo().worldState);
                    }
                    catch (ArgumentNullException) { }
                    catch (NullReferenceException) { /*Some global variable e.g. chattingTo is not set, can safely ignore*/ }
                }

                return value;
            }

            //check whether or not to skip this questStep based on the step info
            public bool SkipThisStep()
            {
                //if there are no ways to skip the step, just move on
                if (skipIf.Count == 0)
                {
                    return false;
                }
                else return CheckDictionaryAgainstValues(skipIf, true, true);
            }

            //checks that all preconditions for starting this step have been met
            public bool PreconditionsMet()
            {
                if (preconditions.Count == 0)
                {
                    return true;
                }
                else return CheckDictionaryAgainstValues(preconditions, false, false);
            }

            //general method that checks if pairs in "toCheck" are "returnIf" and return "valToReturnIf" if so
            //used by SkipIf() and PreconditonsMet() to check to see if the step should be skipped or if it can be advanced to
            private bool CheckDictionaryAgainstValues(Dictionary<string, System.Object> toCheck, bool returnIf, bool valToReturnIf)
            {
                //go through the keys
                foreach (string variableName in toCheck.Keys.ToArray())
                {
                    //default: check the key equals a value
                    string operation = "equals";
                    int intValue = 0;

                    System.Object checkAgainst = toCheck[variableName];

                    //sometimes you might want to see if a value is NOT something, this allows for that
                    bool invertSearch = false;
                    if (checkAgainst != null && checkAgainst.GetType() == typeof(string))
                    {
                        System.Object toVar = ToVariableIfExists((string)checkAgainst);
                        checkAgainst = (toVar ?? toCheck[variableName]);

                        try
                        {
                            //might want to be checking against a number and use other operators than "=", check for that
                            if (((string)toCheck[variableName])[0] == '<')
                            {
                                operation = "lessThan";
                                intValue = Convert.ToInt32(((string)toCheck[variableName]).Substring(2));
                            }
                            else if (((string)toCheck[variableName])[0] == '>')
                            {
                                operation = "greaterThan";
                                intValue = Convert.ToInt32(((string)toCheck[variableName]).Substring(2));
                            }

                            //invert search? (NOT something)
                            invertSearch = (((string)toCheck[variableName]).Substring(0, 4) == "NOT_");
                            checkAgainst = ((string)toCheck[variableName] == "NOT_null" ? null : checkAgainst);
                        } catch (ArgumentOutOfRangeException) { }
                    }
                    
                    //if we are checking against one of the quest's runtime variables
                    if (belongsTo.HasVar(variableName))
                    {
                        //comparisons to return the correct value
                        switch (operation)
                        {
                            case "equals":
                                try
                                {
                                    if ((belongsTo.GetVar(variableName).Equals(checkAgainst) ^ invertSearch) == returnIf) return valToReturnIf;
                                }
                                catch (NullReferenceException)
                                {
                                    if (((belongsTo.GetVar(variableName) == checkAgainst) ^ invertSearch) == returnIf) return valToReturnIf;
                                }
                                break;
                            case "lessThan":
                                if ((Convert.ToInt32(belongsTo.GetVar(variableName)) < intValue) == returnIf) return valToReturnIf;
                                break;
                            case "greaterThan":
                                if ((Convert.ToInt32(belongsTo.GetVar(variableName)) > intValue) == returnIf) return valToReturnIf;
                                break;
                        }

                    }
                    //if we are checking against a WorldState value
                    else
                    {
                        //get the value of the WorldState value
                        System.Object worldStateVal = belongsTo.BelongsTo().worldState.GetType().GetField(variableName).GetValue(belongsTo.BelongsTo().worldState);
                        
                        //comparisons to return the correct value
                        switch (operation)
                        {
                            case "equals":

                                try
                                {
                                    if ((worldStateVal.Equals(checkAgainst) ^ invertSearch) == returnIf) return valToReturnIf;
                                }
                                catch (NullReferenceException)
                                {
                                    if (((((worldStateVal == checkAgainst)) == true) ^ invertSearch) == returnIf) return valToReturnIf;
                                }
                                break;

                            case "lessThan":
                                if ((((Convert.ToInt32(worldStateVal) < intValue)) == true) == returnIf) return valToReturnIf;
                                break;

                            case "greaterThan":
                                if ((((Convert.ToInt32(worldStateVal) > intValue)) == true) == returnIf) return valToReturnIf;
                                break;
                        }
                    }
                }

                return !valToReturnIf;
            }

            //getter/setter
            public Quest BelongsTo()
            {
                return belongsTo;
            }

            public void BelongsTo(Quest quest)
            {
                belongsTo = quest;
            }

        }

        //originally, there would be multiple types of quest steps.
        //although this is deprecated in my head, it's still used.
        //basically for checking that the player has fetched (interacted with) a particular object or an object with a particular tag
        public class GoFetch : QuestStep
        {
            private string fetchTag;
            private GameObject fetchObject;

            //check whether this object is the object (or has the tag) you're supposed to fetch
            public override void CheckObjective(System.Object checkAgainst)
            {
                //keyValuePairs used for pick-ups
                if (checkAgainst.GetType() == typeof(KeyValuePair<string, int>))
                {
                    if (Tag() == "Pickup")
                    {
                        Completed(true);
                    }
                    else if (Tag() == ((KeyValuePair<string, int>)checkAgainst).Key) Completed(true);
                }
                else
                {
                    //check for a correct tag or game object match
                    try
                    {
                        GameObject checkAgainstObj = (GameObject)checkAgainst;
                        if (fetchObject == null)
                        {
                            if (checkAgainstObj.tag.Equals(Tag()) || checkAgainstObj.name.Equals(Tag()) || checkAgainstObj.Equals(BelongsTo().GetVar(Tag()))) Completed(true);
                        }
                        else
                        {
                            if (checkAgainstObj.Equals(Object())) Completed(true);
                        }
                    }
                    catch (InvalidCastException)
                    {
                        if (((string)checkAgainst) == Tag()) Completed(true);
                    }
                }
            }

            //constructor for tag...
            public GoFetch(string fetch, string description, string genDescription, Quest belongsTo, Dictionary<string, System.Object> skipIf, Dictionary<string, System.Object> preconditions, bool optional, Stack<string> dialogue, Dictionary<string, System.Object> result) : base(belongsTo, description, genDescription, skipIf, preconditions, optional, dialogue, result)
            {
                fetchTag = fetch;
                fetchObject = null;
            }

            //constructor for object...
            public GoFetch(GameObject fetch, string description, string genDescription, Quest belongsTo, Dictionary<string, System.Object> skipIf, Dictionary<string, System.Object> preconditions, bool optional, Stack<string> dialogue, Dictionary<string, System.Object> result) : base(belongsTo, description, genDescription, skipIf, preconditions, optional, dialogue, result)
            {
                fetchObject = fetch;
                fetchTag = "";
            }
            
            //get the tag of the thing to fetch
            public string Tag()
            {
                return fetchTag;
            }

            //return the gameobject to be fetched
            public GameObject Object()
            {
                return fetchObject;
            }
        }
    }
}
