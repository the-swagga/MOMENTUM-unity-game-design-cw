using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private Transform look;
    [SerializeField] private Camera playerCamera;

    private float inputX;
    private float inputY;
    private Vector3 moveDirection;

    [Header("Movement Variables")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float slopeAccelConst;
    [SerializeField] private float groundDrag;
    [SerializeField] private float slopeDrag;
    [SerializeField] private float airDrag;
    [SerializeField] private float jumpStrength;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airControl;
    [SerializeField] private float launchPadStrength;
    private Vector3 horizontalVelocity = Vector3.zero;
    
    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask groundLayer;
    private bool canJump = true;
    private bool isGrounded;

    private RaycastHit slopeHit;
    private Vector3 maxAirVelocity;

    [Header("Weapon-Based Propulsion Variables")]
    [SerializeField] private GameObject prop;
    [SerializeField] private float propForce;
    [SerializeField] private float maxVertSpeedProp;
    [SerializeField] private float propFireRate;
    [SerializeField] private int propAmmo;
    private float nextProp = 0.0f;

    [Header("Wall Run Variables")]
    [SerializeField] private float wallRunDecel;
    [SerializeField] private float wallRunCooldown;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance;
    private Vector3 wallNormal = Vector3.zero;
    private bool canWallRun = true;
    private bool isWallRunning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);
        if (isGrounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0.0f;

        MoveInput();
        CheckWallRun();
    }
    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MoveInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space) && canJump && (isGrounded || isWallRunning)) {
            canJump = false;
            Jump();
            Invoke(nameof(ResetCanJump), jumpCooldown);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && PropIsActive() && Time.time >= nextProp && propAmmo > 0)
        {
            nextProp = Time.time + (1.0f / propFireRate);
            propAmmo--;
            if (propAmmo <= 0)
                prop.SetActive(false);

            Vector3 propDirection = playerCamera.transform.forward;
            Prop(propDirection);
        }
    }

    private void MovePlayer()
    {
        moveDirection = look.forward * inputY + look.right * inputX;

        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5F + 0.3F, groundLayer))
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);

            if (isGrounded && IsSliding())
            {
                if (slopeAngle > 0.0f)
                {
                    rb.drag = slopeDrag;
                    float slopeMult = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * slopeAccelConst;
                    rb.AddForce(slopeDirection * moveSpeed * 10.0f * slopeMult, ForceMode.Force);
                } else
                {
                    rb.drag = groundDrag;
                }
                
            }                
        }

        if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10.0f, ForceMode.Force);
        }
        else if (!isGrounded && !isWallRunning)
        {
            horizontalVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);

            if (horizontalVelocity.x > 0.0f)
                horizontalVelocity.x = Mathf.Max(horizontalVelocity.x - airDrag * Time.fixedDeltaTime, 0.0f);
            else if (horizontalVelocity.x < 0.0f)
                horizontalVelocity.x = Mathf.Min(horizontalVelocity.x + airDrag * Time.fixedDeltaTime, 0.0f);

            if (horizontalVelocity.z > 0.0f)
                horizontalVelocity.z = Mathf.Max(horizontalVelocity.z - airDrag * Time.fixedDeltaTime, 0.0f);
            else if (horizontalVelocity.z < 0.0f)
                horizontalVelocity.z = Mathf.Min(horizontalVelocity.z + airDrag * Time.fixedDeltaTime, 0.0f);


            Vector3 airVelocity = horizontalVelocity + moveDirection.normalized * airControl * Time.fixedDeltaTime;

            if (airVelocity.magnitude > maxAirVelocity.magnitude)
                airVelocity = airVelocity.normalized * maxAirVelocity.magnitude;

            rb.velocity = new Vector3(airVelocity.x, rb.velocity.y, airVelocity.z);
        } else if (!isGrounded && isWallRunning)
        {
            WallRun();
        }
    }

    private void Jump()
    {
        Vector3 preJumpVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);

        if (preJumpVelocity.magnitude < 0.1f)
        {
            preJumpVelocity = new Vector3(0.1f, 0.0f, 0.1f);
        }

        if (preJumpVelocity.magnitude < moveSpeed)
            maxAirVelocity = preJumpVelocity.normalized * moveSpeed;
        else
            maxAirVelocity = preJumpVelocity;

        rb.velocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);

        if (isWallRunning)
        {
            StartCoroutine(WallRunCooldown());
        }
    }

    private void ResetCanJump()
    {
        canJump = true;
    }

    private bool IsSliding()
    {
        return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.F));
    }

    private void Prop(Vector3 direction)
    {
        Vector3 currentVelocity = rb.velocity;
        Vector3 newVelocity = currentVelocity + (-direction.normalized * propForce);

        if (newVelocity.y > maxVertSpeedProp)
            newVelocity = new Vector3(rb.velocity.x, maxVertSpeedProp, rb.velocity.z);

        maxAirVelocity = newVelocity;
        rb.velocity = newVelocity;
    }

    public int GetPropAmmo()
    {
        return propAmmo;
    }

    public bool PropIsActive()
    {
        return prop.activeSelf;
    }

    private void CheckWallRun()
    {
        if (!canWallRun) return;

        if (isGrounded)
        {
            isWallRunning = false;
            rb.useGravity = true;
            return;
        }

        if (Physics.Raycast(transform.position, moveDirection.normalized, out RaycastHit wallHit, wallCheckDistance, wallLayer))
        {
            isWallRunning = true;
            wallNormal = wallHit.normal;
        } else
        {
            isWallRunning = false;
            rb.useGravity = true;
        }
    }

    private void WallRun()
    {
        rb.useGravity = false;
        horizontalVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, horizontalVelocity) < 0)
            wallForward *= -1;

        horizontalVelocity = wallForward * horizontalVelocity.magnitude;
        rb.velocity = horizontalVelocity;
        float preWallRunSpeed = horizontalVelocity.magnitude;

        if ((Mathf.Abs(inputX) < 0.1f && Mathf.Abs(inputY) < 0.1f) || preWallRunSpeed < 0.01f)
        {
            rb.useGravity = true;
            return;
        }

        float newSpeed = Mathf.Max(0.0f, preWallRunSpeed - (wallRunDecel * Time.fixedDeltaTime));
        rb.velocity = horizontalVelocity.normalized * newSpeed;
        maxAirVelocity = rb.velocity;
    }

    private IEnumerator WallRunCooldown()
    {
        canWallRun = false;
        isWallRunning = false;
        rb.useGravity = true;

        yield return new WaitForSeconds(wallRunCooldown);

        canWallRun = true;
    }

    public float GetXSpeed()
    {
        horizontalVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        float outSpeed = horizontalVelocity.magnitude;

        return outSpeed;
    }

    public void LaunchPad()
    {
        Vector3 preLaunchVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);

        if (preLaunchVelocity.magnitude < 0.1f)
        {
            preLaunchVelocity = new Vector3(0.1f, 0.0f, 0.1f);
        }

        if (preLaunchVelocity.magnitude < moveSpeed)
            maxAirVelocity = preLaunchVelocity.normalized * moveSpeed;
        else
            maxAirVelocity = preLaunchVelocity;

        rb.velocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        rb.AddForce(Vector3.up * launchPadStrength, ForceMode.Impulse);
    }
}

