using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random=UnityEngine.Random;
using System.Linq;

public class PathManager : MonoBehaviour
{

    public Transform goal_obj;
    public Manager manager;
    public TrajectoryToolbox traj_toolbox;
    public BodyController body; 
    public Transform current_target;

    public bool addStepVariation;

    private NavMeshAgent nav_agent;

    private bool check_path = true;
    public bool CheckPath
    {
        set 
        { 
            check_path = value;
            path_queue.Clear();
        }
    }
    private LineRenderer line_renderer;
    private LineRenderer line_renderer2;
    private Queue<Vector3> path_queue = new Queue<Vector3>();
    private List <Vector3> path = new List <Vector3>();
    private bool random_dest_change;

    void Start()
    {
        if (manager.AgentMode)
        {
            nav_agent = body.agent;
            if (nav_agent == null)
                Debug.Log("Couldn't find the nav mesh agent.");
            // line_renderer = GameObject.Find("Trajectory Toolbox").GetComponent<LineRenderer>();
            line_renderer = traj_toolbox.lineRenderer;
            line_renderer2 = gameObject.GetComponent<LineRenderer>();
            int selected_target = body.PickRandom();
            current_target = body.targets[selected_target].transform;
            nav_agent.destination = current_target.position;
        }
    }

    void Update()
    {
        
        if (manager.AgentMode)
        {
            if (manager.ArchTrajectory)
            {        
                if (check_path && nav_agent.hasPath)
                {   
                    nav_agent.path.ClearCorners();
                    traj_toolbox.GenerateTrajectory(Trajectory_type.Arch, body.transform.position, current_target.position, false);
                    check_path = false;
    
                    SetDestinationQeue(traj_toolbox.Path);
                    VisualizePath(traj_toolbox.Path);
                }
    
                UpdateQueuedPathing();
    
            }
            else 
            {
                if (body.collided)
                {
                    int selected_target = body.PickRandom();
                    current_target = body.targets[selected_target].transform;
                    nav_agent.destination = current_target.position; 
                    body.collided = false;
                    
                }
                VisualizePath(nav_agent.path.corners);
                
            }
            VisualizeCalPath(body.positions);

        }
    }

    public void SetDestinationQeue(IEnumerable<Vector3> path)
    {
        if (addStepVariation && !check_path)
        {
            Debug.Log("Adding step variation on QEUE SETTING");
            path = traj_toolbox.SimplifyPath(path);
            path = traj_toolbox.AddPathVariation(path);
            path_queue = new Queue<Vector3>(path);
        }
        else
        {
            if (!addStepVariation)
                path = traj_toolbox.SimplifyPath(path);
            path_queue = new Queue<Vector3>(path);
        }
        VisualizePath(path);
        VisualizeCalPath(new List<Vector3>(path));

        traj_toolbox.Path = new List<Vector3>(path);

        // nav_agent.SetDestination(path_queue.Dequeue());

    }

    private void UpdateQueuedPathing()
    {
        if (body.collided)
        {
            int selected_target = body.PickRandom();
            current_target = body.targets[selected_target].transform;
            check_path = true;
            body.collided = false;
            return;
        }

        if (path_queue.Count == 0)
        {
            nav_agent.SetDestination(current_target.position);
            return;
        }
            
        if (nav_agent.hasPath == false || nav_agent.remainingDistance < 0.05f /*traj_toolbox.StepLength / 2*/)
        {
            nav_agent.SetDestination(path_queue.Dequeue());
        }


    }

    private void VisualizePath(IEnumerable<Vector3> waypoints)
    {
        if (line_renderer == null) //ToDo: fix the running order, line renderer might be null when method is called
            line_renderer = GetComponent<LineRenderer>();

        List<Vector3> path = new List<Vector3>(waypoints);
        // Debug.Log($"PathManager ({gameObject.name}): waypoints.Count = {path.Count}, line_renderer exists {line_renderer != null}");
        line_renderer.positionCount = path.Count;
        line_renderer.SetPositions(path.ToArray());
    }

    private void VisualizeCalPath(List <Vector3> path)
    {
        int id = Math.Max(0, path.Count - 50);
        var v_path = path.GetRange(id, path.Count - id);
        line_renderer2.positionCount = v_path.Count;
        line_renderer2.SetPositions(v_path.ToArray());
    }

    public void SetDestination(Vector3 point)
    {
        //Debug.Log($"Path Manager: {gameObject.name} location {transform.position} Destination set to {point}");
        nav_agent.SetDestination(point);
        CheckPath = true;
    }
    

}
