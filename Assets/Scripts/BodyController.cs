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
using System.Globalization;


// public class  
public class BodyController : MonoBehaviour
{
    public bool agent_mode = true;
    // private bool random_initial_position_flag = true;

    private float speed = 2f; 
    private Vector3 initial_position;
    private Rigidbody rb;
    Vector3 movement;
    int last_target_id;
    private Quaternion last_rotation;

    public List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  
    List <float> rotations = new List <float>();  
    List <float> body_rotations = new List <float>();  
    List <List <float>> probabilities = new List <List <float>>();  
    List <string> timestamps = new List <string>();  

    public List <float[]> rec_positions = new List <float[]>(); 
    List <float> rec_rotations = new List <float>(); 
    List <float> rec_brotations = new List <float>(); 
    List <float> rec_targets = new List <float>(); 
    public List <string> ids = new List <string> ();

    public Manager manager;
    public HeadRotation head;
    public Calculations cal;
    public Recordings rec;
    public NavMeshAgent agent;
    public Evaluation evaluation;
    public PathManager path_manager;
    public GameObject[] targets;

    public List<float> probability;
    private Vector3 curr_pos;
    private Vector3 velocity;
    private float yaw;
    private float body_rotation;
    
    public int n_targets;
    public float head_rotation_rate; 
    public bool reset;
    public bool collided;

    // float _interval = 0.03f;
    // float _time;

    void Start()
    {
        // _time = 0f;

        initial_position = transform.position;
        // transform.rotation = Quaternion.Euler(0, Random.Range(-180f, 180f), 0);
        head_rotation_rate = manager.HeadRotationRate;
        
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // rb.freezeRotation = true;

        rec = new Recordings(manager.data_collection, manager.dataset, manager.Replay);
        
        cal = new Calculations(manager.BodyTorso, manager.dataset, manager.ScalingProbability); //manager.data_collection
        
        DateTime before = DateTime.Now;
        cal.Train();
        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log("Training duration in milliseconds: " + duration.Milliseconds);

        n_targets = cal.n_targets; 

        targets = new GameObject[n_targets];
        for ( int i = 0; i < n_targets; i++)
        {
            targets[i] = GameObject.Find("Target " + i);
            Debug.Log($"Target {i} is found.");
        }

        // if (agent_mode)
        if (manager.AgentMode)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            if (agent != null)
                Debug.Log("In Agent Mode");
        }
        
        if (manager.Replay) 
        {
            ReadFile();
            // _interval *= 1;
        }
        else 
        {
            GetCurrentData();
            
        }
        StartCoroutine(UpdatePositionList());
        
    }

    void Update()
    {   
        if (manager.Replay)
        {
            Replay();
        }
        else 
        {
            GetCurrentData();
            
            if (Input.GetKeyDown(KeyCode.R)) 
                Reset();
        }
        
    }
    
    void GetCurrentData()
    {
        curr_pos = transform.position;
        OnMove();

        velocity = VelocityCal();

        yaw = head.head_orientation;
        // BodyRotate();
        body_rotation = transform.eulerAngles.y;
        // probability = new List<float>();
        probability = cal.CalculateOnRun(positions, velocities, rotations, body_rotations);
    }
 

    void Replay()
    {
        transform.position = curr_pos;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        head.transform.rotation = Quaternion.Euler(0, body_rotation, 0);
        velocity = VelocityCal();
        probability = cal.CalculateOnRun(positions, velocities, rotations, body_rotations);
        
    }

    IEnumerator UpdatePositionList()
    {
        while(true)
        {
            // Debug.Log($" - {DateTime.Now:HH mm ss fff}");
            if (manager.Replay)
            {
                var pos = rec_positions.PopAt(0);
                curr_pos = new Vector3 (pos[0], pos[1], pos[2]);
        
                yaw = rec_rotations.PopAt(0);

                body_rotation = rec_brotations.PopAt(0);
            }

            var timestamp = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"); 
            positions.Add(curr_pos);
            velocities.Add(velocity);
            rotations.Add(yaw);
            body_rotations.Add(body_rotation);
            probabilities.Add(probability);
            timestamps.Add(timestamp);
            yield return new WaitForSeconds(0.03f);
        }
    }

    void Reset()
    {
        // _time = 0f; 
        if (positions.Count > 10)
        {
            rec.SavetoCSV(timestamps, positions, rotations, body_rotations, probabilities, last_target_id, manager.Replay);
            evaluation.GetResults(positions, rotations, body_rotations, probabilities, last_target_id);
        }

        if (!manager.Replay)
        {
            if (!agent_mode)
                transform.position = initial_position;
            else 
            {   
                // Vector2 rand = Random.insideUnitCircle * 3;
                // transform.position = new Vector3(rand.x, 0, rand.y);
                transform.position = initial_position;

                Vector3 targetDir = path_manager.current_target.position - transform.position;
                float angle = Vector3.Angle(targetDir, transform.forward);
                angle += transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, angle, 0); 
            }
        }    

        positions = new List <Vector3>();  
        velocities = new List <Vector3>();  
        rotations = new List <float>();  
        body_rotations = new List <float>();  
        probabilities = new List <List <float>>(); 
        timestamps = new List <string>();
        Debug.Log("New path started.");
        
        // reset = false;
        // StartCoroutine(SetFalse());
    }

    void OnCollisionEnter(Collision collision)
    {   
        if (collision.gameObject.name != "Ground")
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

            collided = true;
            reset = true;
        }
    }
 

    void OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
       
        transform.RotateAround(transform.position, Vector3.up, x * 5); 
        transform.position += transform.forward * z * Time.deltaTime * speed; 
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

    void ReadFile()
    {
        rec.CSVParser(ref rec_positions, ref rec_rotations, ref rec_brotations, ref rec_targets, ref ids);
    }

    public int PickRandom()
    {
        int selected = Random.Range(0, n_targets); 
        return selected;
    }

    IEnumerator SetFalse()
    {
        yield return new WaitForSeconds(0.001f); 
        reset = false;
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





 