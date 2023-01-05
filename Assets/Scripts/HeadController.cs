using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HeadController : MonoBehaviour
{
    public float rotation_speed = 25.0f; 
    public float speed = 1.42f;
    private Rigidbody rb;
    // Vector3 movement;

    float yaw = 0.0f;
    Vector3 direction_prev = new Vector3();
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    Vector3 OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x, 0, z);
        movement = Vector3.ClampMagnitude(movement, 1);
        Vector3 tempVect = speed * movement * Time.deltaTime;
        rb.MovePosition(transform.position + tempVect);
        return movement;
    }

    Vector3 OnHeading(Vector3 movement)
    {   if (movement == new Vector3 (0, 0, 0))
            return new Vector3 (-1, -1, -1);
        if (movement.x == 1)
            return new Vector3 (0, 0, 0);
        if (movement.x == -1) 
            return new Vector3 (0, 180, 0);
        if (movement.z == 1)
            return new Vector3 (0, 90, 0);
        if (movement.z == -1) 
            return new Vector3 (0, 270, 0);
        else 
        return new Vector3 (0, Mathf.Rad2Deg * Mathf.Atan(Mathf.Sin(movement.z)/Mathf.Cos(movement.x)), 0);
    }
    void OnRotate(Vector3 direction)
    {   
        if (direction != new Vector3 (-1, -1, -1))
        {
            transform.eulerAngles = direction;
            direction_prev = direction;
        }
        else 
            transform.eulerAngles = direction_prev;
            
        yaw = rotation_speed * Input.GetAxis("Mouse X");
        // transform.eulerAngles = new Vector3(0.0f, yaw + direction.y, 0.0f);
        transform.Rotate(0.0f, yaw, 0.0f);
        // Debug.Log("angles: " + transform.eulerAngles);
    }

    public Vector3 GetAngles()
    {
        return transform.eulerAngles;
    }
    // Update is called once per frame
    void Update()
    {
        Vector3 movement = OnMove();
        Vector3 direction = OnHeading(movement);
        OnRotate(direction);
        // Debug.Log("movement: " + movement.ToString());
        // Vector3 tempVect = speed * movement * Time.deltaTime;
        // rb.MovePosition(transform.position + tempVect);

        // Debug.Log("Speed: " + velocity.ToString("F2"));
        // Debug.Log("oldPosition: " + oldPosition.ToString("F2"));

    }
}
