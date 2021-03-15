using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid based movement tutorial
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private Vector3 nextMovePos;
    public float moveSpeed = 5;

    public LayerMask collisionMask;

    private void Start()
    {
        nextMovePos = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Movement();
    }

    private void Movement()
    {
        RotatePlayer();

        if(Vector3.Distance(transform.position, nextMovePos) <= .05f)
        {
            // Forward
            if(Input.GetKey(KeyCode.W) && transform.position == nextMovePos) 
            { 
                if(CanMoveToPos(nextMovePos + transform.forward))
                {
                    nextMovePos += transform.forward;
                }
            }

            // Backwards
            if(Input.GetKey(KeyCode.S) && transform.position == nextMovePos) 
            {
                if(CanMoveToPos(nextMovePos + transform.forward * -1f))
                {
                    nextMovePos += transform.forward * -1f;
                }
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, nextMovePos, moveSpeed * Time.deltaTime);
    }

    private bool CanMoveToPos(Vector3 movePosToCheck)
    {
        Collider[] colliders = Physics.OverlapBox(movePosToCheck, new Vector3(0.25f, 0.25f, 0.25f), Quaternion.identity, collisionMask);

        if(colliders.Length == 0) { return true; }

        return false;
    }

    private void RotatePlayer()
    {
        // Left
        if(Input.GetKeyDown(KeyCode.A)) { transform.Rotate(new Vector3(0, -90, 0)); }

        // Right 
        if(Input.GetKeyDown(KeyCode.D)) { transform.Rotate(new Vector3(0, 90, 0)); }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(nextMovePos + transform.forward, new Vector3(0.25f, 0.25f, 0.25f));
    }
}
