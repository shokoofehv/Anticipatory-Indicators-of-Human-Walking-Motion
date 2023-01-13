using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float GetRotation()
    {
        return transform.transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        float rotY = Input.GetAxis("Mouse X");
        transform.Rotate(0.0f, rotY, 0.0f);
    }
    
}
