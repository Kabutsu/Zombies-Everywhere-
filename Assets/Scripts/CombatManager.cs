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

    private float GetAttackWaitTime()
    {
        return Random.Range(MIN_TIME_BETWEEN_ATTACKS, MAX_TIME_BETWEEN_ATTACKS);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && alive) stopAttack = false;
    }

    public void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Player" && !tame && Inventory.Health() > 0 && !CombatPaused() && !attacking && alive)
        {
            if(timeWaited >= timeBetweenAttacks)
            {
                StartCoroutine(Attack());
            } else
            {
                timeWaited += Time.deltaTime;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player") stopAttack = true;
    }

    private IEnumerator Attack()
    {
        attacking = true;

        GameObject fpsController = GameObject.Find("FPSController");

        float oldX = gameObject.transform.eulerAngles.x;
        float oldZ = gameObject.transform.eulerAngles.z;

        float angle = Vector3.Angle(fpsController.transform.position - gameObject.transform.position, gameObject.transform.forward);
        
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

        yield return new WaitUntil(() => combatPaused == false);

        if (!stopAttack && !tame)
        {
            AudioSource.PlayClipAtPoint(attackSound, gameObject.transform.position);
            animator.Play("attack 0", 0);
            Inventory.Hit(Random.Range(MIN_ATTACK_DAMAGE, MAX_ATTACK_DAMAGE));
        }

        timeWaited = 0;
        timeBetweenAttacks = GetAttackWaitTime();

        attacking = false;

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, standardY, gameObject.transform.position.z);
    }

    public void Shot()
    {
        WorldState.ZombieEncountered();
        alive = false;
        stopAttack = true;
        AudioSource.PlayClipAtPoint(shotSound, gameObject.transform.position);
        StartCoroutine(FallOver());
    }

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

    public void PauseCombat()
    {
        combatPaused = true;
        animator.enabled = false;
    }

    public void ResumeCombat()
    {
        combatPaused = false;
        animator.enabled = true;
    }

    private bool CombatPaused()
    {
        return combatPaused;
    }

    public void MakeTame()
    {
        tame = true;
        stopAttack = true;
    }
}
