using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;


// public class  
public class BodyController : MonoBehaviour
{
    public float speed = 240.0f;
    private Rigidbody rb;
    Vector3 movement;

    List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  
    List <float> rotations = new List <float>();  

    public HeadRotation head;
    public Calculations cal;
    public Recordings rec;
    void Start()
    {

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        cal = new Calculations();
        cal.Train();

        rec = new Recordings();
        
    }

    void Update()
    {   
        var curr_pos = transform.position;
        movement = OnMove();
        Vector3 tempVect = speed * movement * Time.deltaTime;
        rb.MovePosition(curr_pos + tempVect);
        Vector3 velocity = VelocityCal();
        float yaw = head.head_orientation;

        cal.CalculateOnRun(positions, velocities, rotations);

        UpdatePositionList(curr_pos, velocity, yaw);
        rec.SavetoCSV(transform.position, velocity, yaw);

    }
    

    Vector3 OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x, 0, z);
        return Vector3.ClampMagnitude(movement, 1);
    }

    void UpdatePositionList(Vector3 new_position, Vector3 new_velocity, float yaw)
    {
        positions.Add(new_position);
        velocities.Add(new_velocity);
        rotations.Add(yaw);
    }

    // void SavetoCSV(Vector3 new_position, Vector3 new_velocity, float yaw 
    //                 //, List<float> probs
    //                 )
    // {   
    //     string delimiter = ","; 

    //     float[] output = {
    //         new_position.x,
    //         new_position.y,
    //         new_position.z,
    //         yaw
    //         // probs[0],
    //         // probs[1],
    //         // probs[2],
    //         // probs[3],
    //         // probs[4],
    //     }; 

    //     string res = String.Join(delimiter, output);
        
    //     if(!File.Exists(file_path))
    //         File.WriteAllText(file_path, res + Environment.NewLine); 
    //     else
    //         File.AppendAllText(file_path, res + Environment.NewLine);
    // }

    Vector3 VelocityCal()
    {   // 4th order Taylor expansion for the 1st derivative
        int i = positions.Count - 1;  //last index 
        // float e = (float) Math.E;
        if (positions.Count >= 9)
        {
            float velocity_x = (3*positions[i-4].x-16*positions[i-3].x+36*positions[i-2].x-48*positions[i-1].x+25*positions[i+0].x)/(12);
            float velocity_z = (3*positions[i-4].z-16*positions[i-3].z+36*positions[i-2].z-48*positions[i-1].z+25*positions[i+0].z)/(12);
            return new Vector3 (velocity_x, 0, velocity_z);
        }
        else {
            return new Vector3 (0, 0, 0);
        }
    }

}





 