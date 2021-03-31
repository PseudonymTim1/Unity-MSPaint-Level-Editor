using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple FPS player controller 
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private const float GRAVITY = -15f;
    private const float JUMP_HEIGHT = 0.5f;
    private CharacterController charController;

    public float moveSpeed = 12f;
    private Vector2 moveInput;
    private Vector2 mouseInput;

    private Vector3 plyrVelocity;
    private Transform playerCamTransform;
    private float rotX;
    public float mouseSens = 4f;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        charController = GetComponent<CharacterController>();
        playerCamTransform = Camera.main.transform;
    }

    private void Update()
    {
        PlayerInput();
        Movement();
    }

    private void LateUpdate()
    {
        CamAndBodyRotate();
    }

    private void CamAndBodyRotate()
    {
        // Body/cam rotate
        transform.Rotate(Vector3.up * mouseInput.x);
        playerCamTransform.localRotation = Quaternion.Euler(rotX, 0f, 0f);

        rotX -= mouseInput.y;
        rotX = Mathf.Clamp(rotX, -90f, 90f);
    }

    private void PlayerInput()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSens;

        // Jump
        if(Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            plyrVelocity.y = Mathf.Sqrt(JUMP_HEIGHT * -2f * GRAVITY);
        }
    }

    private void Movement()
    {
        // Reset velocity
        if(IsGrounded() && plyrVelocity.y < 0) { plyrVelocity.y = -2f; }

        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;

        charController.Move(moveDir * moveSpeed * Time.deltaTime);

        // Apply gravity
        plyrVelocity.y += GRAVITY * Time.deltaTime;
        charController.Move(plyrVelocity * Time.deltaTime);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, charController.bounds.extents.y + 0.5f);
    }
}
