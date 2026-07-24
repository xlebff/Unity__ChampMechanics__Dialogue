using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private Vector2 movementInput;

    // Unity 6 calls this automatically when using "Send Messages" 
    // The method name MUST match your Action name (e.g., Action "Move" = "OnMove")
    private void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    private void Update()
    {
        // Calculate movement vector for 3D space (X and Z ground plane)
        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

        // Move the transform frame-rate independently
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }
}