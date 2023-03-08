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
    private bool trial_change;
    private List <Vector3> r_positions = new List <Vector3>();
    private List <string> ids = new List <string>();
    private string id;
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
        else if (manager.Replay)
        {
            line_renderer = traj_toolbox.lineRenderer;
            line_renderer2 = gameObject.GetComponent<LineRenderer>();
            
            for(int i=0; i < body.rec_positions.Count; i++)
            {
                var pos =  body.rec_positions[i];
                r_positions.Add(new Vector3 (pos[0], pos[1], pos[2]));
            }
            ids = new List <string>(body.ids);
            id = ids[0];

            ReplayReset();            
            
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
            else if (manager.AddStepVariation)
            {
                if (check_path && nav_agent.hasPath)
                {
                    Vector3[] path = nav_agent.path.corners;

                    nav_agent.path.ClearCorners();
                    path = traj_toolbox.AddPathVariation(path).ToArray();
                    traj_toolbox.Path = new List <Vector3>(path);
                    SetDestinationQeue(path);

                    check_path = false;
                    VisualizePath(path);
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
                    Debug.Log($"Heading to target {selected_target}");
                }
                VisualizePath(nav_agent.path.corners);
                
            }
            VisualizeCalPath(body.positions);

        }
        else if (manager.Replay)
        {
            
            if (body.collided)
            {
                body.collided = false;
                ReplayReset();
            }
            VisualizeReplayPath(path);
            VisualizeCalPath(body.positions);
        }
    }

    void ReplayReset()
    {
        path = new List <Vector3>();
        for (int i = 0; i < r_positions.Count; i++)
        {
            if (ids[i] == id)
                path.Add(r_positions[i]);
            else 
            {
                id = ids[i];
                break;
            }
        }
        r_positions.RemoveRange(0, path.Count);
        ids.RemoveRange(0, path.Count);
        path.RemoveAt(path.Count - 1);
    }

    public void SetDestinationQeue(IEnumerable<Vector3> path)
    {
        if (addStepVariation && !check_path)
        {
            // path = traj_toolbox.SimplifyPath(path);
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
            nav_agent.SetDestination(current_target.position);
            check_path = true;
            body.collided = false;
            Debug.Log($"Heading to target {selected_target}");
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

    private void VisualizeReplayPath(List<Vector3> waypoints)
    {
        if (line_renderer == null) 
            line_renderer = GetComponent<LineRenderer>();
        
        List<Vector3> path = new List<Vector3>(waypoints);

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