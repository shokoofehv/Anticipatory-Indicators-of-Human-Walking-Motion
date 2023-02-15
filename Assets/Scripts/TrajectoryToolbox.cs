using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
// using SaveLoadData;

public enum Trajectory_type
{
    Arch,
    ZigZag
}

public class TrajectoryToolbox : MonoBehaviour
{
    public Transform finish_goal;

    private List<Vector3> path = new List<Vector3>();
    public List<Vector3> Path 
    { 
        get { return path; }
        set
        {
            Debug.Log("New path is set");
            path = value; 
        } 
    }

    public float step_precision = 1f; //for simplification for the path
    private float step_length = 0.45f; //for variation

    [SerializeField]
    public float StepLength
    {
        set
        {
            Debug.Log("Step length changed from " + step_length + " to " + value);
            step_length = value;
        }
        get 
        {          
            return step_length; 
        }
    }


    public GameObject user;
    private NavMeshAgent nav_agent;

    private LineRenderer lineRenderer;

    private void Start()
    {

        nav_agent = user.transform.GetComponent<NavMeshAgent>();
        if (nav_agent == null)
            Debug.LogWarning($"Toolbox's user {user.name} has no nav_agent");

        lineRenderer = gameObject.GetComponent<LineRenderer>();

        //path = GenerateCircle(user.transform.position, user.transform.right, radius);
        //path = ExtractArchFromCircle(path, user.transform.position, angle);
        //path = GenerateZigzag(user.transform.position, user.transform.forward, 10f, 4f, 90f);
        // path = GetConnectingArch(user.transform.position, finish_goal.position, 60f);
        //path = CorrectTrajectoryValidity(path);
        VisualizeTrajectory(path);

    }


    public void GenerateTrajectory(Trajectory_type trajectory_type, Vector3 start_position, Vector3 finish_position, bool useStepVariation)
    {
        List<Vector3> trajectory;

        switch (trajectory_type)
        {
            case Trajectory_type.Arch:
                trajectory = GetConnectingArch(start_position, finish_position);
                break;
            case Trajectory_type.ZigZag:
                trajectory = GenerateZigzag(start_position, finish_position);
                break;

            default:
                trajectory = new List<Vector3>();
                break;
        }

        trajectory = CorrectTrajectoryValidity(trajectory);
        Debug.LogWarning($"TestGenerator: trajectory count = {trajectory.Count}");
        if (useStepVariation && trajectory.Count > 0)
            trajectory = AddPathVariation(trajectory);

        //return trajectory;
        Path = trajectory;
        VisualizeTrajectory(Path);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="finish"></param>
    /// <param name="triangle_angle"> range 0-70 degrees (more than 70 is not practical), defines the radius, the smaller the angle = smaller radius, sharper arch</param>
    /// <returns></returns>
    public List<Vector3> GetConnectingArch(Vector3 start, Vector3 finish, float triangle_angle = 60f)
    {
        triangle_angle = triangle_angle < 0f ? 0f : triangle_angle;

        triangle_angle = triangle_angle > 70f ? 70f : triangle_angle;

        List<Vector3> new_path = new List<Vector3>();
        Vector3 circle_center;
        float radius =  (Vector3.Magnitude(finish - start)/2f) / Mathf.Cos(Mathf.Deg2Rad*triangle_angle);
        
        bool clockwise = Vector3.Dot(start.normalized, finish.normalized) > 0;
       
        Vector3 center_direction = Quaternion.Euler(0f, (clockwise ? 1 : (-1)) * triangle_angle, 0f) * (finish - start).normalized ;


        new_path = GenerateCircle(start, center_direction.normalized, radius, out circle_center);

        Vector3 center_to_start = (start - circle_center).normalized;
        Vector3 center_to_finish = (finish - circle_center).normalized;
        
        float ang = Vector3.Angle(center_to_start, center_to_finish);
        //Debug.Log($"triangle_angle = {triangle_angle}, ang = {ang}, dot product = {Vector3.Dot(center_to_start, center_to_finish)}, clockwise = {clockwise}");
        float d = Vector3.Dot(center_to_start, center_to_finish);
        if ((triangle_angle > 45f ? d > 0 : d < 0) ^ clockwise) //counterclockwise rotated and needs to be clockwise or vise versa, the smallest possible arch
            ang *= -1;

        //Debug.Log($"Now angle = {ang}");
        new_path = ExtractArchFromCircle(new_path, start, ang);

        Debug.Log($"Now points = {new_path.Count}");

        return new_path;
    }

    public List<Vector3> GenerateCircle(Vector3 position, Vector3 arch_center_direction_norm, float radius, out Vector3 circle_center)
    {
        List<Vector3> new_path = new List<Vector3>();
        new_path.Clear();

        List<Vector3> path_plus = new List<Vector3>();
        List<Vector3> path_minus = new List<Vector3>();

        circle_center = new Vector3(position.x + arch_center_direction_norm.x * radius, 0f, position.z + arch_center_direction_norm.z * radius);
        float factor = 1 / (radius / step_precision);
        Debug.Log($"Circle center point = {circle_center} radius = {radius} factor = {factor}");

        for (float x = circle_center.x - radius; x < circle_center.x + radius+ factor* step_precision; x += factor * step_precision)
        {
            float v = radius * radius - (x - circle_center.x) * (x - circle_center.x);
            float z_base = Mathf.Sqrt(Mathf.Abs(v));
            
            path_plus.Add(new Vector3(x, 0f, circle_center.z + z_base));
            path_minus.Add(new Vector3(x, 0f, circle_center.z - z_base));
        }

        path_minus.Reverse();

        new_path.AddRange(path_plus);
        new_path.AddRange(path_minus);

        new_path = SimplifyPath(new_path);

        Debug.Log("Circle point count = " + new_path.Count);

        return new_path;
    }

    public List<Vector3> SimplifyPath(IEnumerable<Vector3> path, float steps_num_simplification = 1f)
    {
        Debug.Log($"Simplifying path: steps_num_simplification = {steps_num_simplification}");

        List<Vector3> new_path = new List<Vector3>(path);
        for (int i = 1; i < new_path.Count; i++)
        {
            if (Vector3.Magnitude(new_path[i] - new_path[i - 1]) < step_length* steps_num_simplification) // or step_length  0.5 * step_precision
            {
                new_path.RemoveAt(i);
                i--;
            }

            //ToDo: maintain the step magnitude to step_length
        }
        return new_path;
    }


    public List<Vector3> ExtractArchFromCircle(List<Vector3> circle_points, Vector3 from_pos, float angle)
    {
        if (angle > 360 || angle < - 360)
        {
            Debug.LogError("Angle should be withing (-360, 360) range of Euler angles");
        }

        List<Vector3> arch_path = new List<Vector3>();

        Vector3 center = (circle_points[0] + circle_points[circle_points.Count / 2]) / 2;
        Vector3 closest_from_point = GetClosestPointFromList(circle_points, from_pos);     
        int from_index = circle_points.IndexOf(closest_from_point);
        Vector3 direction = closest_from_point - center;
        Vector3 to_point = center + Quaternion.Euler(0f, angle, 0f)*direction;
        Vector3 closest_to_point = GetClosestPointFromList(circle_points, to_point);
        int to_index = circle_points.IndexOf(closest_to_point);

        //Debug.Log("Closest to_point = " + to_point.ToString() + " index = " + to_index);

        if (angle > 0) //clock-wise rotation
        { 
            if (from_index < to_index)
            {
                int cnt = to_index - from_index+1;
                arch_path.AddRange(circle_points.GetRange(from_index, cnt));
            }
            else //(from_index > to_index)
            {
                int cnt = circle_points.Count - from_index;
                arch_path.AddRange(circle_points.GetRange(from_index, cnt));
                arch_path.AddRange(circle_points.GetRange(0, to_index));
            }
        }
        else //counterclockwise
        {
            if (from_index < to_index)
            {
                int cnt = from_index + 1;
                List<Vector3> tmp = circle_points.GetRange(0, cnt);
                tmp.Reverse();
                arch_path.AddRange(tmp);
                tmp.Clear();

                cnt = circle_points.Count - to_index - 1;
                tmp = circle_points.GetRange(to_index, cnt);
                tmp.Reverse();
                arch_path.AddRange(tmp);
               
            }
            else //(from_index > to_index)
            {
                int cnt = from_index - to_index +1;
                arch_path.AddRange(circle_points.GetRange(to_index, cnt));
                arch_path.Reverse();
            }
        }
        //arch_path.AddRange(circle_points.Get(from_index, to_index));

        return arch_path;
    }

    public Vector3 GetClosestPointFromList(List<Vector3> points_list, Vector3 point)
    {
        float min_dist = Mathf.Infinity;
        Vector3 closest_point = Vector3.zero;
        for (int i = 0; i < points_list.Count; i++)
        {
            float dist = Vector3.Distance(point, points_list[i]);
            if (dist < min_dist)
            {
                min_dist = dist;
                closest_point = points_list[i];
            }
        }
        return closest_point;
    }

    public List<Vector3> GenerateZigzag(Vector3 from_position, Vector3 to_position, float width = 2f, float angle = 90f, bool start_to_right = true)
    {
        List<Vector3> new_path = new List<Vector3>();

        

        float hyp_step = width / Mathf.Cos(angle/2 * Mathf.Deg2Rad); //path segment length
        float dir_progress_step = hyp_step * Mathf.Sin((90 - angle / 2)*Mathf.Deg2Rad); //movement in chosen direction
        
        Vector3 main_direction = to_position - from_position;
        Vector3 main_dir_norm = main_direction.normalized;

        float length = main_direction.magnitude;

        float segments_needed = length / dir_progress_step;
        new_path.Add(from_position);

        Vector3 main_dir_ortognorm = Quaternion.Euler(0f, 90f, 0f) * main_dir_norm;

        for (int i = 0; i < Mathf.Round(segments_needed); i++)
        {
            //ToDo consider bool start_from_left
            var v = main_dir_norm * dir_progress_step + (i % 2 == 0 ? 1 : -1) * main_dir_ortognorm * width;
            Vector3 new_point;
            if (i+1 < segments_needed + 1)
                new_point = new_path[i] + v;
            else
                new_point = new_path[i] + v*(segments_needed - Mathf.Floor(segments_needed));

            new_path.Add(new_point);
        }

        new_path.Add(to_position);

        return new_path;
    }

    public void VisualizeTrajectory(List<Vector3> path_points)
    {
        if (lineRenderer == null)
            lineRenderer = gameObject.GetComponent<LineRenderer>();

        lineRenderer.positionCount = path_points.Count;
        lineRenderer.SetPositions(path_points.ToArray());
    }

    public List<Vector3> AddPathVariation(IEnumerable<Vector3> waypoints, float variation_factor_dir = 0.95f)
    {
        if (nav_agent && nav_agent.hasPath)
            nav_agent.ResetPath();

        if (waypoints == null)
            Debug.LogError("waypoints are NULL");

        List<Vector3> new_path = new List<Vector3>(waypoints);
        
        if (transform.position == new_path[0])
            Debug.Log("They match!");
        Debug.Log("Before AddPathVariation Waypoints count  = " + new_path.Count);
        int path_cnt = new_path.Count - 1;

        int i = 0, cnt = 1;
        while (i < path_cnt)
        {
            float factor = 1f;
            Vector3 direction = new_path[i + 1] - new_path[i];
            if (direction.magnitude < step_length)
                factor = direction.magnitude / step_length;

            int steps_needed = Mathf.FloorToInt(direction.magnitude / (variation_factor_dir*step_length));
            steps_needed = steps_needed == 0 ? 1 : steps_needed;

            Vector3 dir_normalized = direction.normalized;
            Vector3 dir_orto_norm = Quaternion.Euler(0f, 90f, 0f) * dir_normalized; ;

            int k = i + steps_needed;
            Vector3 cur_pnt = new_path[i];
            for (int j = i + 1; j <= k; j++)
            {
                Vector3 v = cur_pnt + variation_factor_dir*dir_normalized * step_length + (j % 2 == 0 ? 1 : -1) * factor * 0.65f * step_length * dir_orto_norm;
                v.y = v.y < 0f ? (-1) * v.y : v.y;
                cur_pnt = v;
                new_path.Insert(j, v);
            }

            cnt++;

            path_cnt += steps_needed;
            i = k + 1;

        }
        Debug.Log("After AddPathVariation NEW Waypoints count = " + new_path.Count);

        //VisualizePath(new_waypoints);

        return new_path;
    }


    // Checks if the location belongs to the NavMesh, or if there's a point in the distance range from source that belongs to NavMesh
    public bool CheckDestination(Vector3 targetDestination, float range = 1f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetDestination, out hit, range, NavMesh.AllAreas))
            return true;
        return false;
    }

    public Vector3 GetValidDestination(Vector3 targetDestination, float range_step = 0.5f, float range = 1f)
    {
        NavMeshHit hit;
        while (!NavMesh.SamplePosition(targetDestination, out hit, range, NavMesh.AllAreas))
        {
            range += range_step;
        } 
        return hit.position;
    }

    private Vector3 ReplaceWithValidDestination(Vector3 targetDestination, float range = 0.5f, float range_step = 0.5f)
    {
        NavMeshHit hit;
        while (!NavMesh.SamplePosition(targetDestination, out hit, range, NavMesh.AllAreas))
        {
            range += range_step;
        }

        return hit.position;
    }


    public List<Vector3> CorrectTrajectoryValidity(IEnumerable<Vector3> waypoints)
    {
        List<Vector3> new_path = new List<Vector3>(waypoints);
        for (int i = 0; i < new_path.Count; i++)
            new_path[i] = ReplaceWithValidDestination(new_path[i]);

        return new_path;
    }



}
