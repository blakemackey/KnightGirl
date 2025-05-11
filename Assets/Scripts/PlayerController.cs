using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class PlayerController : MonoBehaviour
{
    //Movement
    [Header("Horizontal Movement Settings: ")] 
    [SerializeField] private float walkSpeed = 1;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 45;
    private float jumpBufferCounter = 0;
    [SerializeField] private float jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    //References
    PlayerStateList pState;
    Rigidbody2D rb;
    private float xAxis;
    Animator anim;

    public static PlayerController Instance;

    private void Awake()
    {
        //Destroy player objects that arent the one being controlled
        if(Instance != null && Instance != this) 
        {
            Destroy(gameObject);
        }

        else 
        {
            Instance = this;
        }
    }

    void Start() 
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        pState = GetComponent<PlayerStateList>();
    }

    void Update() 
    {
        GetInputs();
        UpdateJumpVariables();
        Flip();
        Move();
        Jump();
    }

    void GetInputs() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    //Flip sprite when changing directions
    void Flip() 
    {
        if(xAxis < 0) 
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }

        else if(xAxis > 0) 
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }
    }

    void Move() 
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
        anim.SetBool("Walking", rb.linearVelocity.x != 0 && Grounded());
    }

    //Set up raycast for hit detection on ground
    public bool Grounded() 
    {
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)) 
            {
                return true;
            }

            else 
            {
                return false;
            }
    }

    //Add jump function
    void Jump() 
    {
        if(Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0) 
        {
            pState.isJumping = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if(!pState.isJumping) 
        {
            //if (Input.GetButtonDown("Jump") && Grounded())
            if(jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                pState.isJumping = true;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
            }
            else if(!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")) 
            {
                pState.isJumping = true;
                airJumpCounter++;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    //Reset jumping bool to recycle buffer
    void UpdateJumpVariables() 
    {
        if(Grounded()) 
        {
            pState.isJumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else 
        {
            //Time.delta time = time between each frame
            //Reset coyote time
            coyoteTimeCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }
}
