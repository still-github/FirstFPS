using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float crouchSpeed;
    public float slideSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;


    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]

    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    
    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;


    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState{

        walking,
        air,
        crouching,
        sliding

    }

    public bool sliding;

    private void Start(){
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    private void MyInput(){
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jumpKey) && (grounded || OnSlope())&& readyToJump){
            Jump();
            readyToJump = false;

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if(Input.GetKeyDown(crouchKey)){

            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        if(Input.GetKeyUp(crouchKey)){

            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);

        }
    }

    private void Update(){
        
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        
        MyInput();
        SpeedControl();
        StateHandler();
        if(grounded){
            rb.drag = groundDrag;
        }else{
            rb.drag = 0;
        }
    }

    private void FixedUpdate(){
        MovePlayer();

    }


    private void StateHandler(){
        
        if(sliding){
            state = MovementState.sliding;

            if(OnSlope() && rb.velocity.y < 0.1f){
                desiredMoveSpeed = slideSpeed;

            }else{
                desiredMoveSpeed = walkSpeed + 0.2f;
            }

        }
        
        if(grounded){
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

        if(!grounded){
            state = MovementState.air;
        }
        
        if(Input.GetKey(crouchKey)){

            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        
        if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 7f && moveSpeed != 0){
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }else{

            moveSpeed = desiredMoveSpeed;

        }
        lastDesiredMoveSpeed = desiredMoveSpeed;
    }


    private IEnumerator SmoothlyLerpMoveSpeed(){
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference){
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if(OnSlope()){
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }else{
                time += Time.deltaTime;
            }
            yield return null;

        }

        moveSpeed = desiredMoveSpeed;
    }


    private void MovePlayer(){
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        if(OnSlope() && !exitingSlope){
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);
        }
        if(grounded){
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }else if (!grounded){
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    
    }

    private void SpeedControl(){
        
        if(OnSlope() && !exitingSlope){

            if(rb.velocity.magnitude > moveSpeed){
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }


        }else{
        
            Vector3 flatVel = new Vector3(rb.velocity.x,0f,rb.velocity.z);

            if(flatVel.magnitude > moveSpeed){
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y,limitedVel.z);
            }
        }
    }

    private void Jump(){
        exitingSlope = true;
        
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump(){

        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope(){

        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f)){ 
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;

    }else{
        return false;
    }

    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction){

        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;

    }





}
