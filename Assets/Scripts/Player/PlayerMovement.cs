using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashForce = 10f;
    public float dashCooldown = 1f;
    public float dashDuration = 0.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isDashing = false;
    private float lastDashTime = -999f;
    private string currentAnimation = "";

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleDashInput();
        UpdateAnimation();
    }

    private void HandleMovement()
    {
        if (isDashing) return;

        Vector2 input = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        rb.linearVelocity = input * moveSpeed;
    }

    private void HandleDashInput()
    {
        if (isDashing || Time.time < lastDashTime + dashCooldown) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            Vector2 inputDir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            if (inputDir.sqrMagnitude > 0.1f)
            {
                StartCoroutine(PerformDash(inputDir));
            }
        }
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        isDashing = true;
        lastDashTime = Time.time;
        
        rb.linearVelocity = direction * dashForce;
        yield return new WaitForSeconds(dashDuration);
        
        if (new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isDashing = false;
    }

    private void UpdateAnimation()
    {
        if (isDashing) return;
        
        Vector2 movement = rb.linearVelocity.normalized;
        string newAnim = "idle";

        if (movement.magnitude > 0.1f)
        {
            newAnim = Mathf.Abs(movement.x) > Mathf.Abs(movement.y)
                ? movement.x > 0 ? "walk_right" : "walk_left"
                : movement.y > 0 ? "walk_up" : "walk_down";
        }
        else if (!string.IsNullOrEmpty(currentAnimation))
        {
            newAnim = currentAnimation.Replace("walk_", "idle_");
        }

        if (newAnim != currentAnimation)
        {
            currentAnimation = newAnim;
            animator.CrossFade(currentAnimation, 0.1f);
        }
    }
}