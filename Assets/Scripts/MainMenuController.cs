using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject infoPanel;

    [SerializeField]
    private Canvas loadingCanvas;

    private bool loading;

    [SerializeField]
    private GameObject loadingBar;
    [SerializeField]
    private UnityEngine.UI.Image loadingBarBackground;
    [SerializeField]
    private UnityEngine.UI.Image loadingBarProgress;

    private string controls = "Press \"W\" to move forward, \"S\" to move back, \"A\" to strafe left and \"D\" to strafe right.\nUse the mouse to look around.\nLeft click to shoot zombies or talk to people. Don't get too close to zombies!\nPress \"E\" to pause/resume the game.\nUse the spacebar to jump and hold \"Shift\" while moving to sprint.\n\nYou have 10 minutes - explore the world and interact with everything!";
    private string about = "You wake up in the woods with just your handy pistol for company. The zombie apocalypse has been consuming the world for a month now...\n\nThis game is all about how narratives are experienced! Please engage in as many quests and plotlines as possible and see what happens.\n\nBe aware that the log.txt file will be overwritten each time you click \"Start\".";
    private string credits = "All assets (excluding those in the \"/Scripts\" directory) from the Unity Asset Store or Unity Standard Assets unless mentioned below.\n\nFonts 'A Love of Thunder' by Cumberland Fontworks, 'Feast of Flesh' by Blambot, both sourced from Dafont.com.\nMusic 'When The Mockingbirds Are Singing In The Wildwood' by Frank C Stanley, 'Bring Back My Blushing Rose' by John Steel, 'The Spirit of Russian Love' by Zinaida Trokai, all sourced from FreeMusicArchive.org.\nMain menu background image sourced from hdwallsource.com.";

    void Start()
    {
        loadingCanvas.GetComponentInChildren<UnityEngine.UI.Image>().enabled = false;
        loadingCanvas.GetComponentInChildren<UnityEngine.UI.Text>().enabled = false;
        loadingCanvas.enabled = false;

        loadingBar.SetActive(false);

        loading = false;

        ShowControls();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartGame()
    {
        if (!loading)
        {
            loading = true;
            DataLog.CreateNewLog();
            //StartCoroutine(LoadForestAsync());
            StartCoroutine(ShowLoading());
        }
    }

    private IEnumerator LoadForestAsync()
    {
        loadingBar.SetActive(true);
        AsyncOperation loader = SceneManager.LoadSceneAsync("Forest");

        while(!loader.isDone)
        {
            loadingBar.GetComponent<UnityEngine.UI.Slider>().value = loader.progress;
            yield return null;
        }
    }

    private IEnumerator ShowLoading()
    {
        loadingCanvas.enabled = true;
        loadingCanvas.GetComponentInChildren<UnityEngine.UI.Image>().enabled = true;
        loadingCanvas.GetComponentInChildren<UnityEngine.UI.Text>().enabled = true;

        UnityEngine.UI.Image loadingPanel = loadingCanvas.GetComponentInChildren<UnityEngine.UI.Image>();
        UnityEngine.UI.Text loadingText = loadingCanvas.GetComponentInChildren<UnityEngine.UI.Text>();

        loadingPanel.enabled = true;
        loadingText.enabled = true;

        for (float i = 0f; i < 1f; i += Time.deltaTime / 3.5f)
        {
            loadingText.color = new Color(1f, 1f, 1f, i);
            loadingPanel.color = new Color(0f, 0f, 0f, i);

            //loadingBarBackground.color = new Color(1f, 1f, 1f, i);
            //loadingBarProgress.color = new Color((221 / 255f), (8f / 255f), (8f / 255f), i * (210f / 255f));

            yield return null;
        }

        loadingText.color = Color.white;
        loadingPanel.color = Color.black;

        //loadingBarBackground.color = Color.white;
        //loadingBarProgress.color = new Color((221 / 255f), (8f / 255f), (8f / 255f), (210f / 255f));

        SceneManager.LoadScene("Forest");
    }

    public void ShowControls()
    {
        if (!loading)
        {
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 20;
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().text = controls;
        }
    }

    public void ShowAbout()
    {
        if (!loading)
        {
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 20;
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().text = about;
        }
    }

    public void ShowCredits()
    {
        if (!loading)
        {
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 19;
            infoPanel.GetComponentInChildren<UnityEngine.UI.Text>().text = credits;
        }
    }

    public void ExitGame()
    {
        if (!loading)
        {
            Application.Quit();
        }
    }
}
