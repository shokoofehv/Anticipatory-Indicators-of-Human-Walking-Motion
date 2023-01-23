using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathManager : MonoBehaviour
{

    public Transform goal_obj;
    // public ClickedPathCreator clicked_path_creator;
    public TrajectoryToolbox traj_toolbox;

    public bool addStepVariation = true;

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

    void Start()
    {
        nav_agent = GetComponent<NavMeshAgent>();
        nav_agent = GameObject.Find("User Body").GetComponent<NavMeshAgent>();
        // if (clicked_path_creator && clicked_path_creator.drawing)
        //     clicked_path_creator.OnNewPathCreated += SetDestinationQeue;
        // else if (clicked_path_creator == null)
        //     Debug.LogWarning($"clicked_path_creator at {gameObject.name} is null!");

        // if (goal_obj == null)
        //     goal_obj = GameObject.Find("Goal").transform;
        
        // if (line_renderer == null)
        line_renderer = GameObject.Find("Trajectory Toolbox").GetComponent<LineRenderer>();
    }

    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            if (goal_obj != null)
            {
                nav_agent.SetDestination(goal_obj.position);
                CheckPath = true;
                
            }
        }*/

        //handle direct goal setting to the nav_agent
        if (check_path && nav_agent.hasPath)
        {
            Vector3[] path = nav_agent.path.corners;

            if (addStepVariation) // ToDo refer to the same parametrezation setting everywhere
            {
                
                Debug.Log("Adding step variation on PATH CHECK");
                path = traj_toolbox.AddPathVariation(path).ToArray();
                Debug.Log($"addStepVariation = {addStepVariation} setting qeue");
                SetDestinationQeue(path);
            }

            check_path = false;
            VisualizePath(path);
        }
        // else if (nav_agent.hasPath)
        // {   
        //     Vector3[] path = nav_agent.path.corners;
        //     nav_agent.path.ClearCorners();
        //     traj_toolbox.GenerateTrajectory(Trajectory_type.Arch, nav_agent.transform.position, nav_agent.destination, false);
        //     SetDestinationQeue(traj_toolbox.Path);
        //     VisualizePath(traj_toolbox.Path);
        // }
        
        else 
        {
            VisualizePath(nav_agent.path.corners);
        }   

        //ToDo ensure that there's no conflict between the qeue and nav_mesh goal
        //if there's a qeue - update 
        UpdateQueuedPathing();
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
            if (!addStepVariation)
                path = traj_toolbox.SimplifyPath(path);
            path_queue = new Queue<Vector3>(path);
        }
        VisualizePath(path);
        traj_toolbox.Path = new List<Vector3>(path);
    }

    private void UpdateQueuedPathing()
    {
        //if there's no qeue or nav_agent has other path - don't do anything
        if (path_queue.Count == 0)
            return;
        //if the destination is practically reached - set next point
        if (nav_agent.hasPath == false || nav_agent.remainingDistance < 0.1f /*traj_toolbox.StepLength / 2*/)
            nav_agent.SetDestination(path_queue.Dequeue());

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
