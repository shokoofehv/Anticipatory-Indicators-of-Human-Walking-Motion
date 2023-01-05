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
    public float speed = 10.42f;
    private Rigidbody rb;
    Vector3 movement;

    List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  

    string file_path = @"Assets/Log/positions.csv";
    string dataset_path = @"Assets/Datasets/test.csv";
    
    
    List <List <float[]>> rec_positions = new List <List <float[]>>();
    List <List <float>> rec_rotations = new List <List <float>>();
    List <List <float[]>> resample_positions = new List <List <float[]>>();
    List <List <float>> resample_rotations = new List <List <float>>();
    List <List <float>> targets = new List <List <float>>();
    List <List <float[]>> velocity = new List <List <float[]>>();

    List<List<float>> x_vect = new List<List<float>>();
    List<List<float>> z_vect = new List<List<float>>();
    List<List<float>> vx_vect = new List<List<float>>();
    List<List<float>> vz_vect = new List<List<float>>();

    List<List<float>> aligned_x = new List<List<float>>();
    List<List<float>> aligned_z = new List<List<float>>();
    List<List<float>> aligned_vx = new List<List<float>>();
    List<List<float>> aligned_vz = new List<List<float>>();
    List<List<float>> aligned_t = new List<List<float>>();
    List<List<float>> aligned_rot = new List<List<float>>();

    List<float[]> mu = new List<float[]>();

    int frame_rate = 90;
    int resample_freq = 100;
    int n_targets = 5;
    void Start()
    {

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        CSVParser(out rec_positions, out rec_rotations, out targets);
        Resample(); 
        Velocity();
        Vectorize();
        Align();
        FindMu();
        // Debug.Log("target, k, x, z, yaw, v_x, v_z");
        Debug.Log(aligned_x[0].Count); 
        Debug.Log(aligned_z[0].Count); 
        Debug.Log(aligned_x[1].Count); 
        Debug.Log(aligned_z[1].Count); 
        Debug.Log(aligned_x.Count); 
        Debug.Log(aligned_z.Count); 
        // foreach(var x in mu)
        //     Debug.Log(x);  

        if(File.Exists(file_path))
            File.Delete(file_path);
        
        
    }
    void FindMu()
    {           
        for(int i=0; i<n_targets; i++)
        {
            List<int> indexes = new List<int>();

            for(int j=0; j<aligned_t.Count; j++)
                if(i==aligned_t[j].Last())
                    indexes.Add(j);

            float x = 0.0f;
            float z = 0.0f;
            float yaw = 0.0f;
            float v_x = 0.0f;
            float v_z = 0.0f;

            for (int k=0; k<targets[0].Count; k++){
                foreach (int id in indexes)
                {
                    int n_dem = indexes.Count;
                    x += aligned_x[id][k] / n_dem;
                    z += aligned_z[id][k] / n_dem;
                    yaw += aligned_rot[id][k] / n_dem;
                    v_x += aligned_vx[id][k] / n_dem;
                    v_z += aligned_vz[id][k] / n_dem;
                }
                mu.Add(new float[]{i, k, x, z, yaw, v_x, v_z});
            }
            
        }
    }
    void Vectorize()
    {   
        for (int i=0; i<resample_positions.Count; i++)
        {   
            List<float> _x = new List<float>();
            List<float> _z = new List<float>();
            List<float> _v_x = new List<float>();
            List<float> _v_z = new List<float>();
            for (int j=0; j<resample_positions[i].Count; j++)
            {
                _x.Add(resample_positions[i][j][0]);
                _z.Add(resample_positions[i][j][1]);
                _v_x.Add(velocity[i][j][0]);
                _v_z.Add(velocity[i][j][1]);
            }
            x_vect.Add(_x);
            z_vect.Add(_z);
            vx_vect.Add(_v_x);
            vz_vect.Add(_v_z);
        }

    }
    void Align()
    {
        int max_len = 0;
        int max_ind = 0;
        for(int i=0; i<targets.Count; i++)
            if (targets[i].Count > max_len)
            {
                max_len = targets[i].Count;
                max_ind = i;
            }

        for(int i=0; i<targets.Count; i++)
        {
            if (i==max_ind)
                continue;
            SimpleDTW dtw = new SimpleDTW(x_vect[i].ConvertAll(x=>(double) x).ToArray(), 
                                        x_vect[max_ind].ConvertAll(x=>(double) x).ToArray());
            var aligned = dtw.AlignedArrays();
            aligned_x.Add(new List<float>(aligned.x));

            dtw = new SimpleDTW(z_vect[i].ConvertAll(x=>(double) x).ToArray(), 
                                z_vect[max_ind].ConvertAll(x=>(double) x).ToArray());
            aligned = dtw.AlignedArrays();
            aligned_z.Add(new List<float>(aligned.x));

            dtw = new SimpleDTW(vx_vect[i].ConvertAll(x=>(double) x).ToArray(), 
                                vx_vect[max_ind].ConvertAll(x=>(double) x).ToArray());
            aligned = dtw.AlignedArrays();
            aligned_vx.Add(new List<float>(aligned.x));

            dtw = new SimpleDTW(vz_vect[i].ConvertAll(x=>(double) x).ToArray(), 
                                vz_vect[max_ind].ConvertAll(x=>(double) x).ToArray());
            aligned = dtw.AlignedArrays();
            aligned_vz.Add(new List<float>(aligned.x));

            dtw = new SimpleDTW(resample_rotations[i].ConvertAll(x=>(double) x).ToArray(), 
                                resample_rotations[max_ind].ConvertAll(x=>(double) x).ToArray());
            aligned = dtw.AlignedArrays();
            aligned_rot.Add(new List<float>(aligned.x));
            
            float[] arr = new float[aligned.x.Length];
            for (int j = 0; j < aligned.x.Length; j++) {
                arr[j] = targets[i].Last();
            }
            aligned_t.Add(new List<float>(arr));

        }
        
        
                
    }
    void Interpolate(float[] destination, int destFrom, int destTo, float valueFrom, float valueTo)
    {
        int destLength = destTo - destFrom;
        float valueLength = valueTo - valueFrom;
        for (int i = 0; i <= destLength; i++)
            destination[destFrom + i] = valueFrom + (valueLength * i)/destLength;
    }

    List<float> Resampling(List <float> r)
    {   
        float[] source = r.ToArray();
        int dest_size = (int) Math.Round((double) source.Length * resample_freq / frame_rate);
        float[] destination = new float[dest_size];
        destination[0] = source[0];
        int jPrevious = 0;
        for (int i = 1; i < source.Length; i++)
        {
            int j = i * (destination.Length - 1)/(source.Length - 1);
            Interpolate(destination, jPrevious, j, source[i - 1], source[i]);

            jPrevious = j;
        }
        return new List<float>(destination);

    }

    void Resample()
    {
        foreach (var r in rec_rotations){
            List<float> resampled = Resampling(r);
            resample_rotations.Add(resampled);
        }
        foreach (var r in rec_positions){
            List <float> x_axis = new List <float>();
            List <float> z_axis = new List <float>();
            foreach (var p in r){
                x_axis.Add(p[0]);
                z_axis.Add(p[2]);
            }
            List<float> resampled1 = Resampling(x_axis);
            List<float> resampled2 = Resampling(z_axis);
            List<float[]> resampled = new List <float[]>();
            for(int i=0; i < resampled1.Count; i++)
                resampled.Add(new float[] {resampled1[i], resampled2[i]});
            resample_positions.Add(resampled);
        }

    }

    void Velocity()
    {   
        float velocity_x;
        float velocity_z;
        foreach(var p in resample_positions)
        {
            List<float[]> vel_tmp = new List<float[]>();
            for(int i=0; i<p.Count; i++)
            {
                if(i>=5)
                {
                    velocity_x = (3*p[i-4][0]-16*p[i-3][0]+36*p[i-2][0]-48*p[i-1][0]+25*p[i+0][0])/(12);
                    velocity_z = (3*p[i-4][1]-16*p[i-3][1]+36*p[i-2][1]-48*p[i-1][1]+25*p[i+0][1])/(12);

                }
                else 
                {
                    velocity_x = 0.0f;
                    velocity_z = 0.0f;
                }
                vel_tmp.Add(new float[] {velocity_x, velocity_z});
            }
            velocity.Add(vel_tmp);
        }
    }

    Vector3 OnMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x, 0, z);
        return Vector3.ClampMagnitude(movement, 1);
    }

    void UpdatePositionList(Vector3 new_position, Vector3 new_velocity)
    {
        positions.Add(new_position);
        velocities.Add(new_velocity);
    }

    void SavetoCSV(Vector3 new_position, Vector3 new_velocity)
    {   
        string delimiter = ","; 

        // Vector3 head_orientation = head.GetAngles();
        float[] output = {
            new_position.x,
            new_position.y,
            new_position.z,
            new_velocity.x,
            new_velocity.y,
            new_velocity.z,
            // head_orientation.x,
            // head_orientation.y,
            // head_orientation.z
        }; 

        string res = String.Join(delimiter, output);
        
        if(!File.Exists(file_path))
            File.WriteAllText(file_path, res + Environment.NewLine); 
        else
            File.AppendAllText(file_path, res + Environment.NewLine);
    }


    Vector3 VelocityCal()
    {   // 4th order Taylor expansion for the 1st derivative
        int i = positions.Count - 1;  //last index 
        // float e = (float) Math.E;
        if (positions.Count >= 9)
        {
            // float velocity_x = (float) ((4.685468029591042*e+81*positions[i-8].x - 4.2838564842079276*e+82*positions[i-7].x + 1.7492413977236302*e+83*positions[i-6].x 
            //             - 4.1981793545536496*e+83*positions[i-5].x + 6.559655241526099*e+83*positions[i-4].x - 6.996965591015617*e+83*positions[i-3].x 
            //             + 5.247724193324426*e+83*positions[i-2].x - 2.9986995390864937*e+83*positions[i-1].x + 1.0187546202093276*e+83*positions[i+0].x)
            //             /(3.748374423901926*e+82));
            float velocity_x = (3*positions[i-4].x-16*positions[i-3].x+36*positions[i-2].x-48*positions[i-1].x+25*positions[i+0].x)/(12);
            // float velocity_z = (float) ((4.685468029591042*e+81*positions[i-8].z - 4.2838564842079276*e+82*positions[i-7].z + 1.7492413977236302*e+83*positions[i-6].z
            //             - 4.1981793545536496*e+83*positions[i-5].z + 6.559655241526099*e+83*positions[i-4].z - 6.996965591015617*e+83*positions[i-3].z
            //             + 5.247724193324426*e+83*positions[i-2].z - 2.9986995390864937*e+83*positions[i-1].z + 1.0187546202093276*e+83*positions[i+0].z)
            //             /(3.748374423901926*e+82));
            float velocity_z = (3*positions[i-4].z-16*positions[i-3].z+36*positions[i-2].z-48*positions[i-1].z+25*positions[i+0].z)/(12);
            return new Vector3 (velocity_x, 0, velocity_z);
        }
        else {
            return new Vector3 (0, 0, 0);
        }
    }

    void CSVParser(out List <List <float[]>> positions, out List <List<float>> rotations, out List <List<float>> targets)
    {
        using(var reader = new StreamReader(dataset_path))
        {
            List <List <float[]>> position = new List <List<float[]>>();
            List <List <float>> rotation = new List <List<float>>();
            List <List <float>> target = new List <List<float>>();
            List <float[]> position_tmp = new List<float[]>();
            List <float> rotation_tmp = new List<float>();
            List <float> target_tmp = new List<float>();

            var id_prev = "0";
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var id = values[0];
                if (id_prev != id)
                {   
                    position.Add(position_tmp);
                    rotation.Add(rotation_tmp);
                    target.Add(target_tmp);
                    position_tmp = new List<float[]>();
                    rotation_tmp = new List<float>();
                    target_tmp = new List<float>();
                }
                id_prev = id;

                float[] new_values = new float[values.Length-1];
                float out_val = 0.0f;
                for (int i = 1; i < values.Length; i++)
                {
                    if (float.TryParse(values[i], out out_val))
                        new_values[i-1] = float.Parse(values[i]);
                }
                
                position_tmp.Add(new float[] {new_values[0], new_values[1], new_values[2]});
                rotation_tmp.Add(new_values[3]);
                target_tmp.Add(new_values[4]);
            }
            position.Add(position_tmp);
            rotation.Add(rotation_tmp);
            target.Add(target_tmp);
            positions = position;
            rotations = rotation;
            targets = target;
        }
    }

    void Update()
    {   
        var curr_pos = transform.position;
        movement = OnMove();
        Vector3 tempVect = speed * movement * Time.deltaTime;
        rb.MovePosition(curr_pos + tempVect);
        Vector3 velocity = VelocityCal();

        
        UpdatePositionList(curr_pos, velocity);
        SavetoCSV(curr_pos, velocity);
     
        

    }
}

class SimpleDTW
{
    double[] x;
    double[] y;
    double[,] distance;
    double[,] f;
    ArrayList pathX;
    ArrayList pathY;
    ArrayList distanceList;
    double sum;
    public SimpleDTW(double[] _x, double[] _y)
    {
        x = _x;
        y = _y;
        distance = new double[x.Length, y.Length];
        f = new double[x.Length+1, y.Length+1];
        for (int i = 0; i < x.Length; ++i){
            for (int j = 0; j < y.Length; ++j){
                distance[i, j] = Math.Abs(x[i] - y[j]);
            }
        }
        for (int i = 0; i <= x.Length; ++i)
        {
            for (int j = 0; j <= y.Length; ++j)
            {
                f[i, j] = -1.0;
            }
        }
        for (int i = 1; i <= x.Length; ++i) {
            f[i,0] = double.PositiveInfinity;
        }
        for (int j = 1; j <= y.Length; ++j) {
            f[0, j] = double.PositiveInfinity;
        }
        f[0, 0] = 0.0;
        sum = 0.0;
        pathX = new ArrayList();
        pathY = new ArrayList();
        distanceList = new ArrayList();
    }
    public ArrayList getPathX(){
        return pathX;
    }
    public ArrayList getPathY() {
        return pathY;
    }
    public double getSum(){
        return sum;
    }
    public double[,] getFMatrix() {
        return f;
    }
    public ArrayList getDistanceList() {
        return distanceList;
    }
    public void computeDTW() {
        sum = computeFBackward(x.Length, y.Length);
        // Debug.Log("DTW is: " + sum);
        //sum = computeFForward();
    }
    public double computeFForward() {
        for (int i = 1; i <= x.Length; ++i) {
            for (int j = 1; j <= y.Length; ++j) {
                if (f[i - 1, j] <= f[i - 1, j - 1] && f[i - 1, j] <= f[i, j - 1]) {
                    f[i, j] = distance[i - 1, j - 1] + f[i - 1, j];
                }
                else if (f[i, j - 1] <= f[i - 1, j - 1] && f[i, j - 1] <= f[i - 1, j]) {
                    f[i, j] = distance[i - 1, j - 1] + f[i, j - 1 ];                    
                }
                else if (f[i - 1, j-1 ] <= f[i , j - 1] && f[i - 1, j - 1] <= f[i-1, j ]) {
                    f[i, j] = distance[i - 1, j - 1] + f[i - 1, j - 1 ];
                }
            }
        }
        return f[x.Length, y.Length];
    }
    public double computeFBackward(int i, int j)
    {
        if (!(f[i, j] < 0.0) ){
            return f[i, j];
        }
        else {
            if (computeFBackward(i - 1, j) <= computeFBackward(i, j - 1) && computeFBackward(i - 1, j) <= computeFBackward(i - 1, j - 1)
                && computeFBackward(i - 1, j) < double.PositiveInfinity)
            {
                f[i, j] = distance[i - 1, j - 1] + computeFBackward(i - 1, j);
            }
            else if (computeFBackward(i, j - 1) <= computeFBackward(i - 1, j) && computeFBackward(i, j - 1) <= computeFBackward(i - 1, j - 1)
                && computeFBackward(i, j - 1) < double.PositiveInfinity)
            {
                f[i, j] = distance[i - 1, j - 1] + computeFBackward(i, j - 1);    
            }
            else if (computeFBackward(i - 1, j - 1) <= computeFBackward(i - 1, j) && computeFBackward(i - 1, j - 1) <= computeFBackward(i, j - 1)
                && computeFBackward(i - 1, j - 1) < double.PositiveInfinity)
            {
                f[i, j] = distance[i - 1, j - 1] + computeFBackward(i - 1, j - 1);
            }
        }
        return f[i, j];
    }

    public List<int[]> CalculatePath()
    {
        computeDTW();
        double[,] f = getFMatrix();
        int i = 1;
        int j = 1;
        
        List<int[]> path = new List<int[]>();
        
        while(i != x.Length+1 && j != y.Length+1)
        {   
            path.Add(new int[]{i-1, j-1});
            double right = double.PositiveInfinity;
            double down = double.PositiveInfinity;
            double diag = double.PositiveInfinity;
            if (j != y.Length)
                right = f[i, j+1];

            if (i != x.Length)
                down = f[i+1, j];

            if (i != x.Length && j != y.Length)
                diag = f[i+1, j+1];
            
            if (diag <= right && diag <= down)
            {
                i += 1;
                j += 1;
            }
            else if (right < down)
                j += 1;
            else i += 1;

        }
        return path;
    }
    public (float[] x, float[] y) AlignedArrays()
    {
        List<int[]> path = CalculatePath();
        int[] _x = new int[path.Count];
        int[] _y = new int[path.Count];
        double[] new_x = new double[path.Count];
        double[] new_y = new double[path.Count];
        for(int i=0; i<path.Count; i++)
        {
            _x[i] = path[i][0];
            _y[i] = path[i][1];
        }
        for(int i=0; i<_x.Length; i++)
        {
            new_x[i] = x[_x[i]];
        }
        for(int i=0; i<_y.Length; i++)
        {
            new_y[i] = y[_y[i]];
        }

        float[] floatArray1 = new float[new_x.Length];
        float[] floatArray2 = new float[new_y.Length];
        for (int i = 0 ; i < new_x.Length; i++)
        {
            floatArray1[i] = (float) new_x[i];
            floatArray2[i] = (float) new_y[i];
        }
        return (floatArray1, floatArray2);
    }
}
