using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class CombatManager : MonoBehaviour {

    [SerializeField]
    private AudioClip attackSound;

    [SerializeField]
    private AudioClip shotSound;

    private Animator animator;

    private const int MIN_ATTACK_DAMAGE = 8;
    private const int MAX_ATTACK_DAMAGE = 25;

    private const float MIN_TIME_BETWEEN_ATTACKS = 2f;
    private const float MAX_TIME_BETWEEN_ATTACKS = 3.5f;

    private float timeBetweenAttacks;
    private float timeWaited;
    private bool combatPaused;
    private bool stopAttack;
    private bool attacking;
    private bool tame;

    private float standardY;

    private bool alive;

	// Use this for initialization
	void Start () {
        alive = true;
        combatPaused = false;
        stopAttack = false;
        attacking = false;
        tame = false;
        standardY = gameObject.transform.position.y;
        timeBetweenAttacks = GetAttackWaitTime();
        timeWaited = timeBetweenAttacks;
        animator = gameObject.GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //randomize the time between attacks
    private float GetAttackWaitTime()
    {
        return Random.Range(MIN_TIME_BETWEEN_ATTACKS, MAX_TIME_BETWEEN_ATTACKS);
    }

    //allow the player to attack the player when they enter the trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && alive) stopAttack = false;
    }

    //attack the player if certain conditions met, or increment time waited since last attack if other conditions met
    public void OnTriggerStay(Collider other)
    {
        //if the player is in the trigger, "Druid Tome" hasn't been completed, player is alive, game isn't paused, zombie isn't currently attacking, zombie is alive:
        if(other.gameObject.tag == "Player" && !tame && Inventory.Health() > 0 && !CombatPaused() && !attacking && alive)
        {
            if(timeWaited >= timeBetweenAttacks)
            {
                StartCoroutine(Attack()); //attack if it's time to
            } else
            {
                timeWaited += Time.deltaTime; //increase time waited if not
            }
        }
    }

    //stop the zombie from following the player and waiting to attack if the player exits the trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player") stopAttack = true;
    }

    //rotate towards the player and then attack
    private IEnumerator Attack()
    {
        attacking = true;

        //find angle between zombie and player:
        GameObject fpsController = GameObject.Find("FPSController");

        float oldX = gameObject.transform.eulerAngles.x;
        float oldZ = gameObject.transform.eulerAngles.z;

        float angle = Vector3.Angle(fpsController.transform.position - gameObject.transform.position, gameObject.transform.forward);
        
        //rotate towards the player while a "walk" animation plays:
        while ((angle = Vector3.Angle(fpsController.transform.position - gameObject.transform.position, gameObject.transform.forward)) > 40f && !stopAttack)
        {
            if (!CombatPaused() && !tame)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("walk")) animator.Play("walk", 0);
                float step = 1.2f * Time.deltaTime;
                Vector3 newDir = Vector3.RotateTowards(gameObject.transform.forward, fpsController.transform.position - gameObject.transform.position, step, 0.0F);
                transform.rotation = Quaternion.LookRotation(newDir);
                transform.eulerAngles = new Vector3(oldX, transform.eulerAngles.y, oldZ);
            }

            yield return null;
        }

        //if the player leaves the trigger, cancel the whole thing
        yield return new WaitUntil(() => combatPaused == false);

        //attack the player if all conditions are met
        if (!stopAttack && !tame)
        {
            AudioSource.PlayClipAtPoint(attackSound, gameObject.transform.position); //play attack sound
            animator.Play("attack 0", 0); //play attack animation
            Inventory.Hit(Random.Range(MIN_ATTACK_DAMAGE, MAX_ATTACK_DAMAGE)); //deal a random amount of damage
        }

        //reset various variables
        timeWaited = 0;
        timeBetweenAttacks = GetAttackWaitTime();

        attacking = false;

        //there was a bug where the zombie would start rising into the air, this fixes it
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, standardY, gameObject.transform.position.z);
    }

    //if shot by the player, kill the zombie
    public void Shot()
    {
        WorldState.ZombieEncountered();
        alive = false;
        stopAttack = true;
        AudioSource.PlayClipAtPoint(shotSound, gameObject.transform.position); //play death sound
        StartCoroutine(FallOver()); //fall over
    }

    //play "falling over" animation then destroy the game object
    IEnumerator FallOver()
    {
        animator.Play("back_fall");

        var fromPos = gameObject.transform.position;
        var toPos = fromPos - new Vector3(0, 1.3f);
        for (var t = 0f; t < 1; t += Time.deltaTime / 0.75f)
        {
            gameObject.transform.position = Vector3.Lerp(fromPos, toPos, t);
            yield return null;
        }
        Destroy(gameObject);
    }

    //if the player pauses the game
    public void PauseCombat()
    {
        combatPaused = true;
        animator.enabled = false;
    }

    //if the player resumes the game
    public void ResumeCombat()
    {
        combatPaused = false;
        animator.enabled = true;
    }

    //getter
    private bool CombatPaused()
    {
        return combatPaused;
    }

    //stop the zombie from ever attacking again
    public void MakeTame()
    {
        tame = true;
        stopAttack = true;
    }
}
