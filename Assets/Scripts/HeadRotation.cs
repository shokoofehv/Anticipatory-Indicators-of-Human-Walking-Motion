using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadRotation : MonoBehaviour
{   
    public float head_orientation;
    public BodyController body;

    Quaternion start_rotation;
    Quaternion end_rotation;
    float rotation_progress = -1;
    // float waiting_time = 0;
    // bool inverse_rotate = false;

    void Start()
    {
        
    }

    void Update()
    {
        float rotY = Input.GetAxis("Mouse X");
        transform.Rotate(0.0f, rotY, 0.0f);

        RandomHeadRotation();

        if (transform.eulerAngles.y > 180)
            head_orientation = transform.eulerAngles.y - 360;
        
        else
            head_orientation = transform.eulerAngles.y;
    }

    void RandomHeadRotation()
    {
        if (rotation_progress < 1 && rotation_progress >= 0)
        {
            float speed = Random.Range(0.1f, body.head_rotation_rate); 
            rotation_progress += Time.deltaTime * speed;
            transform.rotation = Quaternion.Lerp(start_rotation, end_rotation, rotation_progress);
        }

        else
        {
            rotation_progress = 0;
            start_rotation = body.transform.rotation;
            float rand_deg = Random.Range(-60.0f, 60.0f);
            end_rotation = Quaternion.Euler(0, start_rotation.eulerAngles.y + rand_deg, 0);
        }

    }

    public float GetRotation()
    {
        return transform.eulerAngles.y;
    }

    
}
