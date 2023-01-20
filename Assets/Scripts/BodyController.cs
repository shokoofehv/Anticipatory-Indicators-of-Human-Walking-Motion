using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;
using Random=UnityEngine.Random;

// public class  
public class BodyController : MonoBehaviour
{
    bool agent_flag = true;

    public float speed = 1e5f;
    public Vector3 initial_position;
    private Rigidbody rb;
    Vector3 movement;
    int last_target_id;
    public Transform goal;

    List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  
    List <float> rotations = new List <float>();  
    List <List <float>> probabilities = new List <List <float>>();  

    public HeadRotation head;
    public Calculations cal;
    public Recordings rec;
    public UnityEngine.AI.NavMeshAgent agent;

    public GameObject[] targets;

    int n_targets;

    void Start()
    {
        initial_position = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        cal = new Calculations();
        cal.Train();
        n_targets = cal.n_targets; 

        rec = new Recordings();
        
        if (agent_flag)
        {
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            agent.enabled = true;
            agent.destination = goal.position; 
        }

        targets = new GameObject[n_targets];
        for ( int i = 0; i < n_targets; i++)
            targets[i] = GameObject.Find("Target " + i);
        
    }

    void Update()
    {   
        var curr_pos = transform.position;
        movement = OnMove();
        Vector3 tempVect = speed * movement * Time.deltaTime; // 
        rb.MovePosition(curr_pos + tempVect);
        Vector3 velocity = VelocityCal();
        float yaw = head.head_orientation;

        var probability = cal.CalculateOnRun(positions, velocities, rotations);

        UpdatePositionList(curr_pos, velocity, yaw, probability);
        

        if (Input.GetKeyDown(KeyCode.R)) 
        {
            Reset();
        }
        
    }
    
    int PickRandomTarget()
    {
        int selected = Random.Range(0, n_targets); 
        return selected;
    }

    void Reset()
    {
        transform.position = initial_position;
        rec.SavetoCSV(positions, rotations, probabilities, last_target_id);

        int selected_target = PickRandomTarget();
        if(agent_flag)
            agent.destination = targets[selected_target].transform.position; 

        positions = new List <Vector3>();  
        velocities = new List <Vector3>();  
        rotations = new List <float>();  
        probabilities = new List <List <float>>(); 
        Debug.Log("New path started.");

    }

    void OnCollisionEnter(Collision collision)
    {   
        string str = "Collision detected. ";
        for (int i = 0; i < n_targets; i++)
            if (collision.gameObject.name == ("Target " + i))
            {
                last_target_id = i;

                str += "You hit Target " + i;
                Debug.Log(str);
                break;
            }
        Reset();
    }

    Vector3 OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x, 0, z);
        return Vector3.ClampMagnitude(movement, 1);
    }

    void UpdatePositionList(Vector3 new_position, Vector3 new_velocity, float yaw, List <float> probability)
    {
        positions.Add(new_position);
        velocities.Add(new_velocity);
        rotations.Add(yaw);
        probabilities.Add(probability);
    }

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





 