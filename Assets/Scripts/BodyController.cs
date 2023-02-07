using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;
using Random=UnityEngine.Random;
using UnityEngine.AI;

// public class  
public class BodyController : MonoBehaviour
{
    public bool agent_mode = true;
    private bool random_initial_position_flag = true;

    public float speed = 1e6f; 
    public Vector3 initial_position;
    private Rigidbody rb;
    Vector3 movement;
    int last_target_id;
    private Quaternion last_rotation;

    List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  
    List <float> rotations = new List <float>();  
    List <float> body_rotations = new List <float>();  
    List <List <float>> probabilities = new List <List <float>>();  

    List <float[]> rec_positions = new List <float[]>(); 
    List <float> rec_rotations = new List <float>(); 
    List <float> rec_brotations = new List <float>(); 
    List <float> rec_targets = new List <float>(); 

    public Manager manager;
    public HeadRotation head;
    public Calculations cal;
    public Recordings rec;
    public NavMeshAgent agent;
    public TrajectoryToolbox traj_toolbox;
    public Evaluation evaluation;
    public GameObject[] targets;

    public List<float> probability;
    public int n_targets;
    public float head_rotation_rate; 
    public bool reset;

    void Start()
    {
        initial_position = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // rb.freezeRotation = true;

        rec = new Recordings(manager.data_collection);
        
        cal = new Calculations(manager.BodyTorso, random_initial_position_flag); //manager.data_collection
        cal.Train();
        n_targets = cal.n_targets; 

        targets = new GameObject[n_targets];
        for ( int i = 0; i < n_targets; i++)
        {
            targets[i] = GameObject.Find("Target " + i);
            Debug.Log($"Target {i} is found.");
        }

        if (agent_mode)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            if (agent != null)
                Debug.Log("In Agent Mode");
        }

        if (manager.Replay) ReadFile();

        head_rotation_rate = manager.HeadRotationRate;
    }

    void Update()
    {   
        if (manager.Replay)
        {
            Replay();
        }
        else 
        {
            var curr_pos = transform.position;
            movement = OnMove();
            Vector3 tempVect = speed * movement * Time.deltaTime; 
            rb.MovePosition(curr_pos + tempVect);

            Vector3 velocity = VelocityCal();

            float yaw = head.head_orientation;

            BodyRotate();
            float body_rotation = transform.rotation.eulerAngles.y;

            // probability = new List<float>();
            probability = cal.CalculateOnRun(positions, velocities, rotations, body_rotations);
            
            UpdatePositionList(curr_pos, velocity, yaw, body_rotation, probability);

            if (Input.GetKeyDown(KeyCode.R)) 
                Reset();
        }
        
    }
    
    void Replay()
    {
        var pos = rec_positions.PopAt(0);
        var curr_pos = new Vector3 (pos[0], pos[1], pos[2]);
        transform.position = curr_pos;

        var rot = rec_rotations.PopAt(0);
        var curr_rot = Quaternion.Euler(0, rot, 0);
        transform.rotation = curr_rot;

        var brot = rec_brotations.PopAt(0);
        var curr_brot = Quaternion.Euler(0, brot, 0);
        head.transform.rotation = curr_brot;
        
        var target = (int) rec_targets.PopAt(0);

        Vector3 velocity = VelocityCal();

        probability = cal.CalculateOnRun(positions, velocities, rotations, body_rotations);
        UpdatePositionList(curr_pos, velocity, rot, brot, probability);

        evaluation.Eval(curr_pos, rot, brot, probability, target);
    }

    void ReadFile()
    {
        rec.CSVParser(ref rec_positions, ref rec_rotations, ref rec_brotations, ref rec_targets);
    }

    int PickRandom()
    {
        int selected = Random.Range(0, n_targets); 
        return selected;
    }

    void Reset()
    {
        if (positions.Count > 10)
        {
            rec.SavetoCSV(positions, rotations, body_rotations, probabilities, last_target_id, manager.Replay);
            evaluation.GetResults(positions, rotations, body_rotations, probabilities, last_target_id);
        }

        if (!manager.Replay)
        {
            transform.position = initial_position;
            transform.rotation = Quaternion.Euler(0, Random.Range(-180f, 180f), 0);
        }    

        int selected_target = PickRandom();
        if(agent_mode)
            agent.destination = targets[selected_target].transform.position; 

        positions = new List <Vector3>();  
        velocities = new List <Vector3>();  
        rotations = new List <float>();  
        body_rotations = new List <float>();  
        probabilities = new List <List <float>>(); 
        Debug.Log("New path started.");
        
        reset = true;
        StartCoroutine(SetFalse());
    }

    IEnumerator SetFalse()
    {
        yield return new WaitForSeconds(0.005f); 
        reset = false;
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

    void BodyRotate()
    {   
        Quaternion targetRotation;
        if (movement == Vector3.zero) // handle the standing and not moving
            targetRotation = last_rotation;

        else 
            targetRotation = Quaternion.LookRotation(movement);

        targetRotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    360 * Time.fixedDeltaTime);
        rb.MoveRotation(targetRotation);
    }

    Vector3 OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x, 0, z);
        return Vector3.ClampMagnitude(movement, 1);
    }

    void UpdatePositionList(Vector3 new_position, Vector3 new_velocity, float yaw, float body_rotation, List <float> probability)
    {
        positions.Add(new_position);
        velocities.Add(new_velocity);
        rotations.Add(yaw);
        body_rotations.Add(body_rotation);
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

static class ListExtension
{
    public static T PopAt<T>(this List<T> list, int index)
    {
        T r = list[index];
        list.RemoveAt(index);
        return r;
    }
}





 