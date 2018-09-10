using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{

    NavMeshAgent navMeshAgent;

	// Use this for initialization
	void Start ()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        //navMeshAgent.updateRotation = false;
        navMeshAgent.angularSpeed = 1;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Player clicked..");
            InteractWithWorld();
        }
    }

    void InteractWithWorld()
    {
        var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit interactHit;
        if (Physics.Raycast(interactRay, out interactHit, Mathf.Infinity))
        {
            var objectHit = interactHit.collider.gameObject;
            Debug.Log("Interacted with " + objectHit.name);

            navMeshAgent.destination = interactHit.point;
        }
    }
}
