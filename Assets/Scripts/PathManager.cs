using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathManager : MonoBehaviour
{

    public Transform goal_obj;
    public Manager manager;
    public TrajectoryToolbox traj_toolbox;
    public BodyController body; 
    private Transform current_target;

    public bool addStepVariation = false;

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
    private Queue<Vector3> path_queue = new Queue<Vector3>();
    private List <Vector3> path = new List <Vector3>();

    void Start()
    {
        nav_agent = body.agent;
        if (nav_agent == null)
            Debug.Log("Couldn't find the nav mesh agent.");
        line_renderer = GameObject.Find("Trajectory Toolbox").GetComponent<LineRenderer>();

        int selected_target = body.PickRandom();
        current_target = body.targets[selected_target].transform;
        
    }

    void Update()
    {
        
        if (manager.AgentMode)
        {
            if (manager.ArchTrajectory)
            {        
                if (check_path && nav_agent.hasPath)
                {   
                    // Vector3[] path = nav_agent.path.corners;
    
    
                    nav_agent.path.ClearCorners();
                    // path = traj_toolbox.GetConnectingArch(body.position, current_target.position, 60f);
                    traj_toolbox.GenerateTrajectory(Trajectory_type.Arch, body.transform.position, current_target.position, false);
                    check_path = false;
    
                    SetDestinationQeue(traj_toolbox.Path);
                    VisualizePath(traj_toolbox.Path);
                    Debug.Log("current_target.position " + current_target.position + " traj Path " + traj_toolbox.Path.Count + " check_path " + check_path);
                }
                else if (!nav_agent.hasPath)
                {
                    nav_agent.destination = current_target.position; 
                }
    
                UpdateQueuedPathing();
    
            }
            else 
            {
                if (body.reset)
                {
                    int selected_target = body.PickRandom();
                    current_target = body.targets[selected_target].transform;
                    nav_agent.destination = current_target.position; 
                }
                VisualizePath(nav_agent.path.corners);
            }
        }
    }

    public void SetDestinationQeue(IEnumerable<Vector3> path)
    {
        if (addStepVariation && !check_path)
        {
            Debug.Log("Adding step variation on QEUE SETTING");
            path = traj_toolbox.AddPathVariation(path);
            path_queue = new Queue<Vector3>(path);
        }
        else
        {
            // if (!addStepVariation)
            //     path = traj_toolbox.SimplifyPath(path);
            path_queue = new Queue<Vector3>(path);
        }
        VisualizePath(path);
        traj_toolbox.Path = new List<Vector3>(path);
    }

    private void UpdateQueuedPathing()
    {
        //if there's no qeue or nav_agent has other path - don't do anything
        if (path_queue.Count == 0)
        {
            int selected_target = body.PickRandom();
            current_target = body.targets[selected_target].transform;
            check_path = true;
            return;
        }
        //if the destination is practically reached - set next point
        // if (nav_agent.hasPath == true || nav_agent.remainingDistance < 0.1f /*traj_toolbox.StepLength / 2*/)
        // {
        //     Debug.Log("path_queue.Count " + path_queue.Count);
        //     nav_agent.SetDestination(path_queue.Dequeue());
        // }
        nav_agent.SetDestination(path_queue.Dequeue());
        Debug.Log("path_queue.Count " + path_queue.Count + " nav_agent.hasPath " + nav_agent.hasPath);

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

    public void SetDestination(Vector3 point)
    {
        //Debug.Log($"Path Manager: {gameObject.name} location {transform.position} Destination set to {point}");
        nav_agent.SetDestination(point);
        CheckPath = true;
    }
    

}
