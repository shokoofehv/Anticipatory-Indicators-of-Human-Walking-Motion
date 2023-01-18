using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadRotation : MonoBehaviour
{   
    public float head_orientation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float GetRotation()
    {
        return transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        float rotY = Input.GetAxis("Mouse X");
        transform.Rotate(0.0f, rotY, 0.0f);
        // transform.localRotation = Quaternion.Euler(0.0f, rotY, 0.0f);

        if (transform.eulerAngles.y > 180)
            head_orientation = transform.eulerAngles.y - 360;
        
        else
            head_orientation = transform.eulerAngles.y;
    }
    
}
