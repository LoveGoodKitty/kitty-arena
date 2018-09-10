using GameClassLibrary;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    private float zoom = 1.0f;
    private float zoomStep = 0.125f;

    private Vector3 lookTarget = Vector3.zero;
    private GameObject target = null;
    public Vector3 TargetPosition;

	void Start()
	{
        target = GameObject.FindWithTag("Player");
    }

    public void SetTarget(GameObject target)
    {
        this.target = target;
    }

    private void Update()
    {
        Vector3 lookAt = TargetPosition;
        if (target != null)
            lookAt = target.transform.position;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0.0f)
            zoom += zoomStep;
        else if (scroll > 0.0f)
            zoom -= zoomStep;

        var positionNow = transform.position;
        var targetPosition = (lookAt + new Vector3(0.0f, 1.0f, 0.0f)) + (new Vector3(0.0f, 9.0f, -6.0f) * zoom);

        // smooth camera causes problems with player movement commands making character go back and forth... 
        // adjust target position for camera lag?
        var smoothCamera = false;
        if (smoothCamera)
        {
            transform.position = Vector3.Lerp(positionNow, targetPosition, Time.deltaTime * GameStatic.CameraFollowSpeed);
            lookTarget = Vector3.Lerp(lookTarget, lookAt, Time.deltaTime * GameStatic.CameraFollowSpeed);
            transform.LookAt(lookTarget);
        }
        else
        {
            transform.position = targetPosition;
            lookTarget = lookAt;
            transform.LookAt(lookTarget);
        }

        /*
        var cursor = GameStatic.CursorTarget;
        var onTopOfPlayer = target.transform.position + new Vector3(0.0f, 2.0f, 0.0f);
        var direction = Vector3.Normalize(cursor - onTopOfPlayer);
        var cameraPosition = onTopOfPlayer - (direction * zoom);

        transform.position = Vector3.Lerp(positionNow, cameraPosition, Time.deltaTime * GameStatic.CameraFollowSpeed);
        lookTarget = Vector3.Lerp(lookTarget, cursor, Time.deltaTime * GameStatic.CameraFollowSpeed);

        //transform.position = cameraPosition;
        transform.LookAt(lookTarget); */

    }

    void FixedUpdate()
	{

    }

}
