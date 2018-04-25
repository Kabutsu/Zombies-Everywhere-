using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.SceneManagement;

/* Inventory handles the avatar's side of the IWQ model
 * as well as the player's interactions with the game world,
 * pausing/resuming the game, and UI elements
 */

public class Inventory : MonoBehaviour {

    private static GameObject chattingTo;

    private static Dictionary<string, int> carrying;

    private static int health;
    private static bool gameOver;
    private static bool allQuestsCompleted;

    private int lastHealth;

    private static bool gamePaused;

    private float timePassed;

    private const int TALKING_DISTANCE = 3;
    private const int SHOOTING_DISTANCE = 30;

    [SerializeField]
    private GameObject pausePanel;
    [SerializeField]
    private UnityEngine.UI.Text menuText;

    [SerializeField]
    private UnityEngine.UI.Image crosshairs;
    [SerializeField]
    private GameObject gun;
    [SerializeField]
    private AudioClip gunShot;

    [SerializeField]
    private GameObject healthBar;

    [SerializeField]
    private UnityEngine.UI.Text timer;
    [SerializeField]
    private UnityEngine.UI.Text timeRemainingLabel;

    [SerializeField]
    private UnityEngine.UI.Text questUpdates;

    [SerializeField]
    private GameObject damageIndicator;
    UnityEngine.UI.Image damagePanel;

    private Dictionary<string, string> quests;

    [SerializeField]
    private AudioClip questUpdateSound;
    [SerializeField]
    private AudioClip loseSound;
    [SerializeField]
    private AudioClip winSound;

    [SerializeField]
    private Canvas gameOverCanvas;

    private int layerMask;

    // Use this for initialization
    void Start () {
        foreach (UnityEngine.UI.Image img in gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Image>()) img.enabled = false;
        foreach (UnityEngine.UI.Text txt in gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Text>()) txt.enabled = false;

        health = 100;
        lastHealth = 100;
        gamePaused = false;
        chattingTo = null;
        timePassed = 0;

        carrying = new Dictionary<string, int>();
        quests = new Dictionary<string, string>();
        
        pausePanel.SetActive(false);

        damagePanel = damageIndicator.GetComponent<UnityEngine.UI.Image>();
        damagePanel.enabled = false;

        questUpdates.text = "";
        
        gameOver = false;
        allQuestsCompleted = false;

        layerMask = 1 << 8;

        StartCoroutine(FadeOutTimeLabel());
    }

    //load the quests from QuestManager so they can show up in the pause menu
    public void InitializeQuests()
    {
        foreach(string questName in QuestManager.Quests().Keys)
        {
            quests.Add(questName, QuestManager.GetGeneralDescription(questName));
        }
    }

    //update the text shown with a quest in the "Quests" section of the pause menu
    public void UpdateDescription(string questName, string description)
    {
        quests[questName] = description;
    }
	
	// Update is called once per frame
	void Update () {
        //check for decreased health (i.e. a zombie attacking the player)
        if (Health() != LastHealth())
        {
            if(Health() < LastHealth()) StartCoroutine(ShowDamage());
            LastHealth(Health());
            healthBar.GetComponent<UnityEngine.UI.Slider>().value = (Health() < 0 ? 0 : Health());
            if (Health() <= 0 && !gameOver) GameOver("You died!", false); //game over if the player ends up on 0 or less health
        }

        //keep the timer ticking along if the game is running
        if (!GamePaused() && !gameOver)
        {
            timePassed += Time.deltaTime;

            int minutesLeft = 9 - Mathf.FloorToInt(timePassed / 60f);
            int secondsLeft = 59 - Mathf.RoundToInt(timePassed) % 60;

            //display the time left in top corner
            timer.text = minutesLeft.ToString() + ":" + (secondsLeft < 10 ? (secondsLeft == 0 ? "00" : "0" + secondsLeft.ToString()) : secondsLeft.ToString());

            if (timePassed >= 600) GameOver("You ran out of time!", false);
        }

        //pause/resume the game
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!GamePaused())
            {
                OpenPauseMenu();
            }
            else
            {
                ClosePauseMenu();
            }
        }

        //if the game is running, check to see if the player can/does interact with anything
        if (!GamePaused()) CheckForInteractions();

        //play a sound if the player shoots the gun
        if (Input.GetKeyDown(KeyCode.Mouse0) && crosshairs.enabled)
            GetComponent<AudioSource>().PlayOneShot(gunShot);
    }

    //pause the game and show the menu
    private void OpenPauseMenu()
    {
        PauseGame();
        pausePanel.SetActive(true);
        timeRemainingLabel.color = new Color(1f, 1f, 1f, 0.75f);
        ShowQuests();
    }

    //resume the game and hide the menu
    public void ClosePauseMenu()
    {
        pausePanel.SetActive(false);
        ResumeGame();
        StartCoroutine(FadeOutTimeLabel());
    }

    //fade out the text next to the time remaining so it doesn't distract the player
    private IEnumerator FadeOutTimeLabel()
    {
        yield return new WaitForSeconds(6.5f);

        for (float i = 1f; i > 0f; i -= Time.deltaTime / 3.5f)
        {
            timeRemainingLabel.color = new Color(1f, 1f, 1f, i * 0.75f);
            yield return null;
        }
        timeRemainingLabel.color = new Color(1f, 1f, 1f, 0f);
    }

    //pause all game objects and show the cursor
    private void PauseGame()
    {
        gamePaused = true;

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Friendly"))
            friendly.GetComponent<ChatManager>().PauseChat();

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Pickup"))
            friendly.GetComponent<ChatManager>().PauseChat();

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Idol"))
            friendly.GetComponent<ChatManager>().PauseChat();

        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Zombie"))
            zombie.GetComponent<CombatManager>().PauseCombat();

        crosshairs.enabled = false;

        GameObject.Find("FPSController").GetComponent<FirstPersonController>().enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    //resume all game objects and hide the cursor
    public void ResumeGame()
    {
        gamePaused = false;

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Friendly"))
            friendly.GetComponent<ChatManager>().ResumeChat();

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Pickup"))
            friendly.GetComponent<ChatManager>().ResumeChat();

        foreach (GameObject friendly in GameObject.FindGameObjectsWithTag("Idol"))
            friendly.GetComponent<ChatManager>().ResumeChat();

        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Zombie"))
            zombie.GetComponent<CombatManager>().ResumeCombat();

        if (chattingTo == null)
        {
            crosshairs.enabled = true;
            GameObject.Find("FPSController").GetComponent<FirstPersonController>().enabled = true;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        pausePanel.SetActive(false);
    }

    //show all the items in the inventory
    public void OpenBackpack()
    {
        menuText.fontSize = 24;
        string backpackItems = "";
        foreach (KeyValuePair<string, int> item in Backpack()) backpackItems += (MoreReadable(item.Key) + ": " + item.Value.ToString() + "\n");
        menuText.text = "<b>Inventory:</b>\n\n" + (backpackItems == "" ? "Your backpack is empty." : backpackItems);
    }

    //show the text describing the controls
    public void ShowControls()
    {
        menuText.fontSize = 24;
        menuText.text = "Press \"W\" to move forward, \"S\" to move back, \"A\" to strafe left and \"D\" to strafe right.\n\nUse the mouse to look around.\n\nLeft click to shoot zombies or talk to people. Don't get too close to zombies or they'll attack!\n\nPress \"E\" to pause/resume the game.\n\nUse the spacebar to jump and hold \"Shift\" while moving to sprint.\n\nYou have 10 minutes - explore the world and interact with everything!";
    }

    //show the current status of all quests
    public void ShowQuests()
    {
        menuText.fontSize = 19;
        string questText = "";
        foreach(string questName in quests.Keys)
        {
            questText += (questName + ":\n    - ");
            QuestManager.Quest currentQuest = QuestManager.GetQuest(questName);
            if(currentQuest == null)
            {
                questText += "COMPLETED\n";
            } else if (currentQuest.Started())
            {
                questText += "STARTED\n";
            } else
            {
                questText += "NOT STARTED\n";
            }
            questText += ("    - " + quests[questName] + "\n\n");
        }
        menuText.text = questText;
    }

    //show the "about" text
    public void ShowAbout()
    {
        menuText.fontSize = 24;
        menuText.text = "You wake up in the woods with just your handy pistol for company. The zombie apocalypse has been consuming the world for a month now...\n\nThis game is all about how narratives are experienced! Please engage in as many quests and plotlines as possible and see what happens!\n\n\nThe quests:\n\n\"Hunt Them Down\" is about clearing out zombies from every pile of radioactive material.\n\"The Family Heirloom\" is about finding a character's cherished items from one of the buildings.\n\"The Worst Trade Deal...\" involves you working out what to trade with one of the survivors.\n\"The Ancient Druid Tome\" finds you using magic to put an end to the apocalypse.\n\"Your Typical 'Go-Fetch' Quest\" has you chasing objective after objective, in true 'Go-Fecth' fashion.";
    }

    //takes a variable name e.g. "varName" and returns a more readable version e.g. "Var Name"
    private string MoreReadable(string varName)
    {
        string readableVarName = varName;
        
        int pos = 0;
        foreach(char character in varName) {
            if (Char.IsUpper(character))
            {
                readableVarName = readableVarName.Substring(0, pos) + " " + readableVarName.Substring(pos);
                pos++;
            }
            pos++;
        }
        
        return readableVarName.Substring(0, 1).ToUpper() + readableVarName.Substring(1);
    }
    
    //checks to see if any zombies can be shot, else goes on to check for conversations
    private void CheckForInteractions()
    {
        RaycastHit somethingInFront;

        //interact with something close
        if (Physics.Raycast(transform.position, transform.forward, out somethingInFront, TALKING_DISTANCE, layerMask))
        {
            if (!WorldState.CanChat())
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) && somethingInFront.collider.gameObject.tag == "Zombie")
                {
                    somethingInFront.collider.gameObject.GetComponent<CombatManager>().Shot(); //shoot a zombie if one in crosshairs and mouse clicked
                }
                else if (Input.GetKeyDown(KeyCode.Mouse0) && somethingInFront.collider.gameObject.name == "Radio")
                {
                    somethingInFront.collider.gameObject.GetComponent<RadioController>().Louder(); //make the radio louder if it's in the crosshairs and mouse clicked
                }
                else if (somethingInFront.collider.gameObject.tag == "Friendly" || somethingInFront.collider.gameObject.tag == "Idol")
                {
                    //make everything ready for chatting if the object is a friendly NPC or magic idol
                    crosshairs.enabled = false;

                    chattingTo = somethingInFront.collider.gameObject;
                    WorldState.CanChat(true);
                    chattingTo.GetComponent<ChatManager>().GiveChatOption();
                }
                else if (somethingInFront.collider.gameObject.tag == "Pickup")
                {
                    //make everything ready for picking up if the object is a pick-up
                    crosshairs.enabled = false;
                    chattingTo = somethingInFront.collider.gameObject;

                    if (chattingTo.GetComponent<PickupManager>().CanPickUp())
                    {
                        WorldState.CanChat(true);
                        GameObject pickupObject = somethingInFront.collider.gameObject;
                        pickupObject.GetComponent<ChatManager>().GiveChatOption();
                        pickupObject.GetComponent<ChatManager>().SetConversation(pickupObject.GetComponent<PickupManager>().PickupMessage());
                    }
                }
                else crosshairs.enabled = true; //on any other collision, make sure the crosshairs are displayed
            }

            //interact with something far away
        }
        else if (Physics.Raycast(transform.position, gameObject.transform.GetChild(0).transform.forward, out somethingInFront, SHOOTING_DISTANCE, layerMask)
          && (somethingInFront.collider.gameObject.tag == "Friendly" || somethingInFront.collider.gameObject.tag == "Zombie" || somethingInFront.collider.gameObject.name == "Radio"))
        {
            if (!WorldState.CanChat())
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) && somethingInFront.collider.gameObject.tag == "Zombie")
                {
                    somethingInFront.collider.gameObject.GetComponent<CombatManager>().Shot(); //shoot zombie if it's in the crosshairs and mouse clicked
                }
                else if (Input.GetKeyDown(KeyCode.Mouse0) && somethingInFront.collider.gameObject.name == "Radio")
                {
                    somethingInFront.collider.gameObject.GetComponent<RadioController>().Louder(); //make radio louder if it's in the crosshairs and mouse clicked
                }
                else if (somethingInFront.collider.gameObject.tag == "Friendly")
                {
                    //make sure player can't shoot friendly NPCs
                    crosshairs.enabled = false;
                }
                else crosshairs.enabled = true; //make sure crosshairs are enabled for all other collisions
            }
        }
        else
        {
            //get rid of the chatting option if no NPC, zombie, idol, or pick-up is hit by the raycast
            WorldState.CanChat(false);
            if (!(chattingTo == null))
            {
                try
                {
                    chattingTo.GetComponent<ChatManager>().TakeChatOption();
                    chattingTo = null;
                }
                catch (NullReferenceException) { /*do nothing */ }
                crosshairs.enabled = true;
            }

            if (!WorldState.CanChat() && !pausePanel.activeInHierarchy) crosshairs.enabled = true; //make sure crosshairs are enabled

        }
    }

    public void ShowCrosshairs()
    {
        crosshairs.enabled = true;
    }

    //getter/setter
    public static GameObject ChattingTo() { return chattingTo; }
    public static void ChattingTo(GameObject setTo) { chattingTo = setTo; }

    //add a picked up item to the backpack
    public static void PickUp(string item, int number)
    {
        if (Carrying(item))
        {
            carrying[item] = carrying[item] + number;
        } else
        {
            carrying.Add(item, number);
        }
    }

    //drop an item from the backpack
    public static void Drop(string item, int number)
    {
        if(Carrying(item))
        {
            carrying[item] = carrying[item] - number;
            if (carrying[item] <= 0) carrying.Remove(item);
        }
    }

    //return how many of an item the player is carrying
    public static int CarryingHowMany(string item)
    {
        return (Carrying(item) ? carrying[item] : 0);
    }

    //is the player carrying "item"?
    private static bool Carrying(string item)
    {
        return carrying.ContainsKey(item);
    }

    //return the whole backpack
    private static Dictionary<string, int> Backpack()
    {
        return carrying;
    }

    //reduce health by "damage"
    public static void Hit(int damage)
    {
        health -= damage;
    }

    //increase health by "byAmount" up to 100
    public static void Heal(int byAmount)
    {
        health += byAmount;
        if (Health() > 100) health = 100;
    }

    //getters...
    public static int Health()
    {
        return health;
    }

    private int LastHealth()
    {
        return lastHealth;
    }

    //setter
    private void LastHealth(int val)
    {
        lastHealth = val;
    }

    //show a red hitbox if the player is attacked, then fade it away
    private IEnumerator ShowDamage()
    {
        damagePanel.enabled = true;

        damagePanel.color = new Color((229f / 255f), (25f / 255f), (25f / 255f), 0.55f);
        yield return new WaitForSeconds(0.15f);

        for(float i = 1f; i > 0f; i -= Time.deltaTime)
        {
            damagePanel.color = new Color((229f / 255f), (25f / 255f), (25f / 255f), (i > 0.55f ? 0.55f : i));
            yield return null;
        }

        damagePanel.enabled = false;
    }

    //play the game over animation
    public void GameOver(string reason, bool win)
    {
        gameOver = true;
        allQuestsCompleted = win;
        PauseGame();

        //play win/lose sound
        if (allQuestsCompleted)
        {
            AudioSource.PlayClipAtPoint(winSound, transform.localPosition);
        }
        else AudioSource.PlayClipAtPoint(loseSound, transform.localPosition);

        //enable/disable UI elements
        timeRemainingLabel.enabled = false;

        pausePanel.SetActive(false);
        crosshairs.enabled = false;

        gameOverCanvas.enabled = true;

        //display game over text
        if(!allQuestsCompleted)
        {
            int questsCompleted = 0;
            foreach (string questName in quests.Keys)
                if (QuestManager.GetQuest(questName) == null) questsCompleted++;

            reason += "\n\n\n\nYou completed " + questsCompleted + "/5 quests!";
        }

        gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Text>()[1].text = reason;
        if (allQuestsCompleted) gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Text>()[0].text = "You Win!";

        //disable the player
        GameObject.Find("FPSController").GetComponent<FirstPersonController>().enabled = false;

        //print this to the log
        DataLog.Print("\"" + reason + "\" ::: Time left:= " + timer.text);

        //then fade out to the main menu
        StartCoroutine(FadeToMenu(3f, 7f));
    }

    //fade the screen to black over "inSeconds" seconds after waiting for "waitForSeconds" seconds then load the main menu scene
    private IEnumerator FadeToMenu(float inSeconds, float waitForSeconds)
    {
        //get & enable UI elements
        UnityEngine.UI.Image fadePanel = gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Image>()[0];
        UnityEngine.UI.Text gameOverText = gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Text>()[0];
        UnityEngine.UI.Text reasonText = gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Text>()[1];
        UnityEngine.UI.Image opaquePanel = gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Image>()[1];

        fadePanel.enabled = true;
        gameOverText.enabled = true;
        reasonText.enabled = true;
        
        //change colour of game over text depending on win/lose
        Vector3 gameOverColour = (allQuestsCompleted ? new Vector3(131f, 248f, 109f) : new Vector3(200f, 40f, 0f));
        float gameOverAlpha = (allQuestsCompleted ? 200f : 240f);

        //fade in the game over text
        for (float i = 0f; i < 1f; i += Time.deltaTime)
        {
            fadePanel.color = new Color((33f/255f), (33f / 255f), (33f / 255f), i * (170f/255f));
            gameOverText.color = new Color((gameOverColour.x/255f), (gameOverColour.y/255f), (gameOverColour.y/255f), i * (gameOverAlpha/255f));
            reasonText.color = new Color(1f, 1f, 1f, i);
            yield return null;
        }

        fadePanel.color = new Color((33f / 255f), (33f / 255f), (33f / 255f), (170f/255f));
        gameOverText.color = new Color(gameOverColour.x/255f, gameOverColour.y/255f, gameOverColour.z/255f, gameOverAlpha/255f);
        reasonText.color = Color.white;
        
        //wait for player to read the info
        yield return new WaitForSeconds(waitForSeconds);

        opaquePanel.enabled = true;
        
        //fade to black
        for (float i = 0f; i < 1f; i += Time.deltaTime)
        {
            opaquePanel.color = new Color(0f, 0f, 0f, i);
            yield return null;
        }
        fadePanel.color = new Color(0f, 0f, 0f, 1f);

        LoadMainMenu();
    }

    //load the main menu scene
    public void LoadMainMenu()
    {
        DataLog.Print("EXITED GAME at " + timer.text); //write to the log
        SceneManager.LoadScene("main");
    }

    //getter
    public static bool GamePaused()
    {
        return gamePaused;
    }

    //update "Quests" info...
    public void QuestStarted(string questName)
    {
        questUpdates.text += "Quest \"" + questName + "\" started\n";
        StartCoroutine(FadeQuestUpdates());
    }

    public void QuestFinished(string questName)
    {
        questUpdates.text += "Quest \"" + questName + "\" completed!\n";
        StartCoroutine(FadeQuestUpdates());
    }

    //tell the player that a quest has been started/completed, then fade out
    private IEnumerator FadeQuestUpdates()
    {
        questUpdates.color = new Color((131f / 255f), (248f / 255f), (109f / 255f), (200f / 255f));

        string originalText = questUpdates.text; //they may complete many quests in quick succession; this will be used later
        AudioSource.PlayClipAtPoint(questUpdateSound, transform.localPosition); //play a sound to notify the player
        yield return new WaitForSeconds(5f); //wait for player to read the update

        //if they complete many quests in quick sucession, we don't want one fading out then another. Just wait for the latest coroutine to reach here before fading
        if(originalText == questUpdates.text)
        {
            //fade the text out
            for (float i = 1f; i > 0f; i -= Time.deltaTime)
            {
                if (originalText != questUpdates.text) break;
                questUpdates.color = new Color((131f/255f), (248f/255f), (109f/255f), i * (200f/255f));
                yield return null;
            }

            //hide the text
            if (originalText == questUpdates.text)
            {
                questUpdates.color = new Color((131f / 255f), (248f / 255f), (109f / 255f), 0f);
                questUpdates.text = "";
            }
        }
    }
}
