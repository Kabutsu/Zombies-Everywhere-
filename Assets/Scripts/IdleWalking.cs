using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleWalking : MonoBehaviour {

    [SerializeField] public float radius = 10.0f;
    [SerializeField] private float m_StickToGroundForce;

    private bool rising = false;
    private float speed = 0;
    private CharacterController m_CharacterController;

    // Use this for initialization
    void Start () {
        m_CharacterController = GetComponent<CharacterController>();
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 desiredMove = transform.position;

        RaycastHit hitInfo;
        Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                           m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        GetNewSpeed();

        desiredMove.x = desiredMove.x * speed;
        desiredMove.z = desiredMove.z * speed;
        desiredMove.y = -m_StickToGroundForce;

        transform.position = desiredMove;
    }

    private void GetNewSpeed() {
        if(rising && speed < radius)
        {
            speed += 0.5f;
        } else if(rising && speed >= radius)
        {
            rising = false;
            speed -= 0.5f;
        } else if(!rising && speed > -radius)
        {
            speed -= 0.5f;
        } else if(!rising && speed <= -radius)
        {
            rising = true;
            speed += 0.5f;
        }
    }
}
