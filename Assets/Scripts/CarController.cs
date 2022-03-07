using UnityEngine;

public class CarController : MonoBehaviour
{

    public float moveInput, turnInput;
    public float moveSpeed = 0f, turnStrength, addedTurnStrength = 30f;
    public float brakeFactor = 1f, brakeStrength;

    [SerializeField]
    bool grounded = false, printed = false, brakespressed = false;

    // max limits of speed and drift angle and times taken to reach those values
    public float maxSpeed, maxDriftAngle, extremestDriftAngle, timeToMaxSpeed, timeToReachMaxDriftAngle;

    // temp variables to store times of key releases and presses
    [SerializeField]
    float timeSinceSpeedIncrease, timeSinceSpeedDecrease, brakePressTime, brakeReleaseTime;

    // temp variables to store values of lower and upper bounds of speed values for Lerp
    [SerializeField]
    float tempSpeedLowerBound, speedReached;

    // temp variables to store values of lower and upper bounds of angular values for Lerp
    [SerializeField]
    private float tempAngleLowerBound, angleReached;

    // direction +1 or -1, for each axes
    [SerializeField]
    private int vertDirection, horizDirection;

    // reference to the transforms of the wheels 
    public Transform frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;

    public Transform carVisuals, groundRayPoint;

    [SerializeField]
    Quaternion frontLeftWheelOriginalRot, frontRightWheelOriginalRot;

    public Rigidbody rb;

    public float gravityForce, lowerDragValue, greaterDragValue;

    //max angle by which front wheels can rotate
    public float maxWheelRotation, maxWheelRotationNotDrifting, maxWheelRotationWhileDrifting;

    public float rayPointLength;

    public LayerMask whatIsGround;

    private void Awake()
    {
        //storing original rotational values
        frontLeftWheelOriginalRot = frontLeftWheel.transform.localRotation;
        frontRightWheelOriginalRot = frontRightWheel.transform.localRotation;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // vertical input
        moveInput = Input.GetAxis("Vertical");

        // horizontal input
        turnInput = Input.GetAxis("Horizontal");

        // If either of the vertical keys have been triggered:
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            // start time of pressing vertical keys is stored
            timeSinceSpeedIncrease = Time.timeSinceLevelLoad;

            // the lower bound of the first lerp function is defined
            tempSpeedLowerBound = moveSpeed;
        }


        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.S))
        {
            // time of vertical key release is stored
            timeSinceSpeedDecrease = Time.timeSinceLevelLoad;

            // the speed reached is stored in a var
            speedReached = moveSpeed;
        }

        //directions should either be 1 or -1. Never 0

        if (moveInput > 0)
            vertDirection = 1;
        else if (moveInput < 0)
            vertDirection = -1;


        if (!brakespressed)
        {
            if (turnInput > 0)
                horizDirection = 1;
            else if (turnInput < 0)
                horizDirection = -1;
        }


        // calculating speed - acceleration or deceleration - smoothened with Lerp

        if (Mathf.Abs(moveInput) > 0)
        {
            if (!brakespressed)
            // if brakes are not pressed then there will be vertical acceleration
            {
                float timeElapsedSincePressFactor = Mathf.Clamp01((Time.timeSinceLevelLoad - timeSinceSpeedIncrease) / ((1 - (tempSpeedLowerBound / maxSpeed * vertDirection)) * timeToMaxSpeed));
                moveSpeed = Mathf.Lerp(tempSpeedLowerBound, maxSpeed * vertDirection, timeElapsedSincePressFactor);
            }
            else
            // if brakes are pressed and there is a vertical movement input - brakes dominate
            {
                float timeElapsedSinceReleaseFactor = Mathf.Clamp01((Time.timeSinceLevelLoad - brakePressTime) / ((Mathf.Abs(speedReached) / maxSpeed) * timeToMaxSpeed));
                moveSpeed = Mathf.Lerp(speedReached, 0f, timeElapsedSinceReleaseFactor * brakeFactor);
            }
        }
        else  // if there is no vertical input 
        {
            float timeElapsedSinceReleaseFactor = Mathf.Clamp01((Time.timeSinceLevelLoad - timeSinceSpeedDecrease) / ((Mathf.Abs(speedReached) / maxSpeed) * timeToMaxSpeed));
            moveSpeed = Mathf.Lerp(speedReached, 0f, timeElapsedSinceReleaseFactor * brakeFactor);
        }


        //when space is triggered
        //storing values for calculations for drifting 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //time when brake is pressed is stored
            brakePressTime = Time.timeSinceLevelLoad;
            
            //new lower bound of slerp function
            tempAngleLowerBound = (carVisuals.localRotation.eulerAngles.y > 300) ? (carVisuals.localRotation.eulerAngles.y - 360) : carVisuals.localRotation.eulerAngles.y;

            //the car can turn more
            turnStrength += addedTurnStrength;

            //speed reached in trnaslation is stored
            speedReached = moveSpeed;

            //wheel rotates further
            maxWheelRotation = maxWheelRotationWhileDrifting;
        }

        // while Space is pressed
        if (Input.GetKey(KeyCode.Space))
        {
            brakeFactor = brakeStrength; // speed return to zero faster by this factor
            brakespressed = true;
        }

        if (turnInput > 0 && brakespressed)
            brakeFactor = 1.2f;


        /* when Space is released, new values stored for use in the slerp function
         * that reverts the rotation of the vehicle visuals back to zero
         */
        if (Input.GetKeyUp(KeyCode.Space))
        {
            // updating the new lower bound for the movement speed
            speedReached = moveSpeed;

            //storing the direction value so that it does not change when left or right key is released
            horizDirection = Mathf.RoundToInt(turnInput);

            //original retardation factor
            brakeFactor = 1f;

            brakespressed = false;

            timeSinceSpeedIncrease = Time.timeSinceLevelLoad;

            tempSpeedLowerBound = moveSpeed;

            brakeReleaseTime = Time.timeSinceLevelLoad;

            turnStrength -= addedTurnStrength;

            // defining the new lower bound for the the second slerp function
            angleReached = (carVisuals.localRotation.eulerAngles.y > 200) ? (carVisuals.localRotation.eulerAngles.y - 360) : carVisuals.localRotation.eulerAngles.y;

            maxWheelRotation = maxWheelRotationNotDrifting;
        }

        
        
        
        
        // Calculations for drifting   

        maxDriftAngle = (2 * moveSpeed / maxSpeed) * extremestDriftAngle;

        //increase local angle of car visuals - smoothened with Slerp
        if (Mathf.Abs(turnInput) > 0 && Mathf.Abs(moveSpeed) > 0 && grounded)
        {
            if (brakespressed)
            {
                float timeElapsedSinceBrakePressFactor = Mathf.Clamp01((Time.timeSinceLevelLoad - brakePressTime) / ((1 - (tempAngleLowerBound / maxDriftAngle * horizDirection)) * timeToReachMaxDriftAngle));
                carVisuals.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, tempAngleLowerBound, 0f), Quaternion.Euler(0f, horizDirection * maxDriftAngle, 0f), timeElapsedSinceBrakePressFactor);
            }
            else
            {
                float timeElapsedSinceBrakeReleaseFactor = Mathf.Clamp01((Time.timeSinceLevelLoad - brakeReleaseTime) / ((Mathf.Abs(angleReached) / maxDriftAngle) * timeToReachMaxDriftAngle));
                carVisuals.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, angleReached, 0f), Quaternion.identity, timeElapsedSinceBrakeReleaseFactor);
            }
        }

    }

    private void FixedUpdate()
    {
        grounded = false;
        
        rb.drag = greaterDragValue;

        //check if on ground
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out _, rayPointLength, whatIsGround))
            grounded = true;


        //vehicle vertical translation 
        transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime, Space.Self);

        //car can be controlled only when on ground
        if (grounded)
        {
            if (Mathf.Abs(moveSpeed) > 0)
            {
                //vehicle rotation
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * moveSpeed / Mathf.Abs(moveSpeed) * Time.fixedDeltaTime, 0f));


                //rear wheel rotation

                rearLeftWheel.localRotation *= Quaternion.AngleAxis(100f * moveSpeed * Time.fixedDeltaTime, Vector3.forward);
                rearRightWheel.localRotation *= Quaternion.AngleAxis(100f * moveSpeed * Time.fixedDeltaTime, Vector3.forward);


                //front wheel rotation and turning

                frontLeftWheel.localRotation = Quaternion.Euler(frontLeftWheel.localRotation.eulerAngles.x,
                    frontLeftWheelOriginalRot.eulerAngles.y + turnInput * maxWheelRotation,
                    frontLeftWheel.localRotation.eulerAngles.z + 100f * moveSpeed * Time.fixedDeltaTime);

                frontRightWheel.localRotation = Quaternion.Euler(frontRightWheel.localRotation.eulerAngles.x,
                    frontRightWheelOriginalRot.eulerAngles.y + turnInput * maxWheelRotation,
                    frontRightWheel.localRotation.eulerAngles.z + 100f * moveSpeed * Time.fixedDeltaTime);

            }
        }
        else
        {
            rb.AddForce(-Vector3.up * gravityForce * 1000f);
            rb.drag = lowerDragValue;
        }

       
    }
}