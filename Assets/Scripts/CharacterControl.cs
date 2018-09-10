using UnityEngine;

/// <summary>
/// 
/// game mode : character state (moving, casting, idle, ) , character stats, 
///     character input controller and deterministic move player, 
///     networked players agree to play ech others actions with maximum input delay - action animation speed can be adjusted 
///     based on delay from the other player, move validity checked against some state in player,
///     unity structure calls for update of game state each pre-draw,
///     game structure is state of player at given time,
///     unity structure feed input into player, input is filtered and registered and played, input over network is played 
///     on revieve and sent out, inputs from all streams and their filtered result start event in associated game structure player
///     
///     gameMode mode =
///         state
///         time
///         streams
///         
///         update(elapsed)=
///             apply game rules, play out events
///             get inputs from streams 
///             filter player inputs and network inputs
///             transform inputs into commands
///             apply commands
///             maintain visual description
///             
///         startPlayingFrom(replay/game session with network players /just local input - input streams, )
///         
///     basic state - players, player positions, elapsed time, kind of player mode, objects in level
///         
/// unity mode : run a game mode based on settings, send input, recieve visual object lifetime events
/// 
/// unity structure:    animation, position, rotation, creation and lifetime events of objects as specified by game mode events
/// </summary>
/// 


[RequireComponent(typeof (Animator))]
//[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
public class CharacterControl : MonoBehaviour
{/*
    private float animSpeed = 1.0f;
    private float lookSmoother = 3.0f;

    private float forwardSpeed = 1.0f;
    private float backwardSpeed = 1.0f;
    private float rotationSpeed = 12.0f;

    private float speed = 5.0f;
    private float castSpeed =  1.4f;

    private float moveCooldown = 0.25f;
    private float moveDuration = 0.0f;
    private bool moving = false;

    private float rotationTime = 0.0f;

    private Quaternion moveStartRotation = Quaternion.identity;
    private Quaternion moveDestinationRotation = Quaternion.identity;
    private Vector3 moveDestination = Vector3.zero;
    private Vector3 moveStartPosition = Vector3.zero;
    private float moveDistance = 0.0f;
    private float moveStartTime = 0.0f;
    private Vector3 moveDirection = Vector3.zero;

    private CapsuleCollider capsuleCollider;
    private Rigidbody rigidBody;
    private Vector3 velocity;
	
	private Animator animator;
	private AnimatorStateInfo currentBaseState;

	private GameObject cameraObject;

    private Vector3 lastPosition;

    private bool freezeMovement = false;
    private float spellCooldown = 0.0f;

    private int lastAnimation = 0;

    private float timeStart = 0.0f;
		
	static int idleState = Animator.StringToHash("Base Layer.idle");
	static int runState = Animator.StringToHash("Base Layer.run");
	static int spellState = Animator.StringToHash("Base Layer.spell");

    private Vector3 interactPoint = Vector3.zero;
    private bool interact = false;
    private GameObject interactObject;

    private float lastAnimationCallTime = 0.0f;

    private float animationRunDuration = 0.0f;
    private float animationSpellDuration = 0.0f;
    //private float animation

    private float idleStartTime = 0.0f;
    private bool lastMovingState = false;
    private float minimumIdleDuration = 0.0f;

    // TODO
    // move event is not calling because the cycle starts at different position
    void Start ()
	{
		animator = GetComponent<Animator>();

        //capsuleCollider = GetComponent<CapsuleCollider>();
		rigidBody = GetComponent<Rigidbody>();
		cameraObject = GameObject.FindWithTag("MainCamera");

        var allClips = animator.runtimeAnimatorController.animationClips;

        foreach (var clip in allClips)
        {
            var e = new AnimationEvent();
            e.functionName = "PrintEvent";
            e.stringParameter = clip.name + " = " + clip.length.ToString() + "S";

            if (clip.name.StartsWith("spell"))
            {
                animationSpellDuration = clip.length;
                e.time = 0.0f;
            }

            if (clip.name.StartsWith("run"))
            {
                animationRunDuration = clip.length;
                e.time = 0.35f;
            }

            clip.AddEvent(e);


            //Debug.Log("Animation detected = " + clip.name);

        }
    }

    public void PrintEvent(string i)
    {
        var timeNow = Time.time;
        var difference = timeNow - lastAnimationCallTime;

        //Debug.Log("Animation started: " + i + " called at: " + timeNow + " after = " + difference + " movement = " + moveDuration + " freeze = " + spellCooldown);

        lastAnimationCallTime = timeNow;
    }

    bool spellKey = false;
    bool moveKey = false;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        //subsract freeze/etc times
        spellCooldown = Mathf.Max(0.0f, spellCooldown - Time.deltaTime);

        InteractRay();

        spellKey = false;
        moveKey = false;

        if (Input.GetMouseButton(1))
            spellKey = true;

        if (Input.GetMouseButton(0))
            moveKey = true;

        if (spellCooldown <= 0.0f)
        {
            if (interact)
            {
                if (spellKey)
                {
                    moveStartRotation = transform.rotation;
                    moveDestinationRotation = Quaternion.LookRotation(moveDirection);
                    moveStartPosition = transform.position;
                    moveDestination = interactPoint;
                    moveStartTime = Time.time;

                    animator.CrossFadeInFixedTime("spell", 0.25f);
                    //animator.PlayInFixedTime("spell", -1, 0.0f);

                    timeStart = Time.realtimeSinceStartup;
                    spellCooldown = 1.0f / castSpeed; // animationSpellDuration / castSpeed;

                    rotationTime = 0.0f;
                    moving = false;
                }
                else if (moveKey)
                {
                    // if perform move success..
                    // if spellcasting queue move
                    // if move distance < minimum move radius do nothing
                    // for spells if already casting, queue a spell
                    moveStartRotation = transform.rotation;
                    moveDestinationRotation = Quaternion.LookRotation(moveDirection);
                    moveStartPosition = transform.position;
                    moveDestination = interactPoint;
                    moveStartTime = Time.time;
                    moveDistance = Vector3.Distance(moveStartPosition, moveDestination);

                    //moveDuration = 0.0f;
                    rotationTime = 0.0f;
                    moving = true;

                    velocity = moveDirection * forwardSpeed * speed;
                }
            }
        }

        var t = Time.deltaTime;

        if (spellCooldown <= 0.0f)
        {
            if (!moving)
            {
                rotationTime = 0.0f;
                moveDuration = 0.0f;

                moveStartRotation = transform.rotation;
                moveDestinationRotation = Quaternion.LookRotation(moveDirection);
            }
            else
            {
                moveDuration += t;

                var distanceTraveled = Vector3.Distance(transform.position, moveStartPosition);


                if (distanceTraveled >= moveDistance - 0.1f)
                {
                    //transform.position = moveDestination;



                    velocity = Vector3.zero;
                    moveDuration = 0.0f;
                    moving = false;
                }

                transform.localPosition += velocity * t;
            }
        }

        bool idle = (!moving) && (spellCooldown <= 0.0f);

        rotationTime += idle ? t * 0.2f : t;
        transform.rotation = Quaternion.Lerp(moveStartRotation, moveDestinationRotation, rotationTime * rotationSpeed);

        //apply animator parameters
        animator.speed = animSpeed;
        animator.SetFloat("CastSpeed", (animationSpellDuration / 1.0f) * castSpeed);
        animator.SetFloat("Speed", speed * (animationRunDuration / 4.0f));
        animator.SetBool("Rest", !moving);
    }

    void FixedUpdate ()
    {


    }

    private void InteractRay()
    {
        var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit interactHit;
        var mask = LayerMask.GetMask("Ground");
        interact = false;
        
        if (Physics.Raycast(interactRay, out interactHit, Mathf.Infinity, mask))
        {
            interactObject = interactHit.collider.gameObject;
            //Debug.Log("Interacted with " + objectHit.name);

            interactPoint = interactHit.point;
            

            moveDirection = Vector3.Normalize(interactPoint - transform.position);
            moveDirection.y = 0.0f;
            moveDirection.Normalize();

            interact = true;
        }
    }*/
}
