using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    //Movement
    [Header("Horizontal Movement Settings: ")] 
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]

    [Header("Jump Settings:")]
    [SerializeField] private float jumpForce = 45;
    private float jumpBufferCounter = 0;
    [SerializeField] private float jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;
    [Space(5)]


    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]

    [Header("Dash Settings:")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    [Space(5)]

    //Create different attack areas
    [Header("Attack Settings:")]
    [SerializeField] private Transform SideAttackTransform;
    [SerializeField] private Vector2 SideAttackArea;
    [SerializeField] private Transform UpAttackTransform;
    [SerializeField] private Vector2 UpAttackArea;
    [SerializeField] private Transform DownAttackTransform;
    [SerializeField] private Vector2 DownAttackArea;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private float damage;
    [SerializeField] private GameObject slashEffect;

    [SerializeField] private float timeBetweenAttack;
    private float timeSinceAttack;
    [Space(5)]



    //References
    PlayerStateList pState;
    Rigidbody2D rb;

    private bool attack = false;
    private float xAxis;
    private float yAxis;

    private float gravity;
    Animator anim;
    private bool canDash = true;
    //Check if dash has been used in 

    private bool hasDashed;

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
        //Create gravity scale
        gravity = rb.gravityScale;
    }

    //Create attack areas
    private void OnDrawGizmos()
    {
        //Make wirecube red for debugging
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    void Update() 
    {
        GetInputs();
        UpdateJumpVariables();

        //Freeze movement options if dashing
        if(pState.isDashing) return;

        Flip();
        Move();
        Jump();
        StartDash();
        Attack();
    }

    void GetInputs() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
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

    //Call dash routine
    void StartDash() 
    {
        if(Input.GetButtonDown("Dash") && canDash && !hasDashed) 
        {
            StartCoroutine(Dash());
            hasDashed = true;
        }

        //Refresh dash on the ground
        if(Grounded()) 
        {
            hasDashed = false;
        }
    }

    IEnumerator Dash() 
    {
        //Stop function from running again while player dashes, and perform dash
        canDash = false;
        pState.isDashing = true;
        anim.SetTrigger("Dashing");

        //Stop falling during dash
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if(Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;

        //End dash and set cooldown until player can dash again
        pState.isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack() 
    {
        timeSinceAttack += Time.deltaTime;
        //Prevent spamming attacks
        if(attack && timeSinceAttack >= timeBetweenAttack) 
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            //call hit function based on where player is attacking
            if (yAxis == 0 || yAxis < 0 && Grounded())
            {
                Hit(SideAttackTransform, SideAttackArea);
                Instantiate(slashEffect, SideAttackTransform);
            }

            else if (yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea);
                SlashEffectAtAngle(slashEffect, 90, UpAttackTransform);
            }

            else if (yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea);
                SlashEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
        List<Enemy> hitEnemies = new List<Enemy>();

        if (objectsToHit.Length > 0)
        {
            Debug.Log("Hit");
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy e = objectsToHit[i].GetComponent<Enemy>();
            if (e && !hitEnemies.Contains(e))
            {
                e.EnemyHit(damage);
                hitEnemies.Add(e);
            }
        }
    }

    private void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    //Set up raycast for hit detection on ground
    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
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
