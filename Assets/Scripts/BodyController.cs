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
    public float speed = 40.42f;
    private Rigidbody rb;
    Vector3 movement;

    List <Vector3> positions = new List <Vector3>();  
    List <Vector3> velocities = new List <Vector3>();  
    List <float> rotations = new List <float>();  

    string file_path = @"Assets/Log/positions.csv";
    string dataset_path = @"Assets/Datasets/test.csv";
    
    
    List <List <float[]>> rec_positions = new List <List <float[]>>();
    List <List <float>> rec_rotations = new List <List <float>>();
    List <List <float>> targets = new List <List <float>>();

    List <List <float[]>> resample_positions = new List <List <float[]>>();
    List <List <float>> resample_rotations = new List <List <float>>();
    List <List <float>> resample_targets = new List <List <float>>();
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

    List<List<float[]>> mu = new List<List<float[]>>();
    List<List<float[,]>> variance = new List<List<float[,]>>();
    List<List<float[,]>> i_variance = new List<List<float[,]>>();
    List<List<float>> det = new List<List<float>>();
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
        FindVariance();
        CalculateDet();
        CalculateInverse();

  
        // if(File.Exists(file_path))
        //     File.Delete(file_path);
        
        
    }

    float CalculateDeltaVar(int id, int k)
    {
        int N=5;
        float[] arr = new float[]{
                        positions[k].x,
                        positions[k].z,
                        rotations[k],
                        velocities[k].x,
                        velocities[k].z};
        float [] new_arr = new float[N];
        for (int i=0; i<N; i++)
        {
            float temp = 0.0f;
            for(int j=0; j<N; j++)
            {
                temp += (arr[j]-mu[id][k][j]) * i_variance[id][k][j, i]; 
            }
            new_arr[i] = temp;
        }
        float res = 0.0f;
        for (int i=0; i<N; i++)
            res += new_arr[i] * (arr[i]-mu[id][k][i]);
        return res;
    }

    List <float> CalculateProb()
    {
        int N_f = 5;
        List <float> target_pro = new List<float>();
        
        for (int i=0; i<variance.Count; i++)
        {
            float temp = 0.0f;
            for(int j=0; j<positions.Count; j++)
            {
                temp += (float) (-1.0 * Math.Log10(Math.Pow(2 * Math.PI, N_f/2) * Math.Pow(det[i][j], 0.5)) 
                        - 0.5 * (CalculateDeltaVar(i, j)));
            }
            target_pro.Add(temp);
        }
        return target_pro;
    }

    void CalculateInverse()
    {
        int N = 5;
        for (int i=0; i<variance.Count; i++)
        {
            List<float[,]> i_var_temp = new List<float[,]>();
            for (int j=0; j<variance[i].Count; j++)
            {
                float [,] _i_var = new float[N,N];
                MInverse inv = new MInverse();
                _i_var = inv.Inverse(variance[i][j]);
                i_var_temp.Add(_i_var);
            }
            i_variance.Add(i_var_temp);
        }   
    }

    void CalculateDet()
    {
        for(int i=0; i<variance.Count; i++)
        {
            List<float> det_i = new List<float>();
            for(int j=0; j<variance[i].Count; j++)
            {
                float [,] m = variance[i][j];
                GFG tt = new GFG();
                float _det = tt.DeterminantOfMatrix(m, 5);
                if (Double.IsNaN(_det))
                    det_i.Add(0);
                else 
                    det_i.Add(_det);
            }
            det.Add(det_i);
        }
    }

    float[,] MultMatrix(float[] mat)
    {   
        float[,] res = new float[mat.Length, mat.Length];
        for(int i=0; i<mat.Length; i++)
        {
            for(int j=0; j<mat.Length; j++)
            {
                res[i, j] = mat[i] * mat[j];
            }
        }
        return res;
    }

    void SumMatrix(ref float[,] sum, float[,] mat, int n_dem)
    {
        float[,] res = sum;
        for(int i=0; i<mat.GetLength(0); i++)
        {
            for(int j=0; j<mat.GetLength(1); j++)
            {
                res[i, j] += mat[i, j] / (n_dem - 1);
            }
        }
        sum = res;

    }

    void FindVariance()
    {
        for(int i=0; i<mu.Count; i++)
        {
            List<int> indexes = new List<int>();
            // find the trials with the same targets
            for(int j=0; j<aligned_t.Count; j++)
                if(i==aligned_t[j].Last())
                    indexes.Add(j);
            
            if (indexes.Count <= 1)
            {
                throw new Exception("Class " + i.ToString() + " doesn't have enough records.");
                // continue;
            }

            List<float[,]> variance_i = new List<float[,]>();
            
            for(int j=0; j<mu[i].Count; j++)
            {   
                float [,] sum_res = new float [mu[i][j].Length, mu[i][j].Length];
                for (int k=0; k<indexes.Count; k++)
                {
                    float[] arr = new float[]{
                        aligned_x[k][j],
                        aligned_z[k][j],
                        aligned_rot[k][j],
                        aligned_vx[k][j],
                        aligned_vz[k][j]
                    };
                    float[] point_diff = new float[arr.Length];
                    float[,] mult_res = new float[arr.Length, arr.Length]; 
                    for(int t=0; t<arr.Length; t++)
                        point_diff[t] = arr[t] - mu[i][j][t];
                    mult_res = MultMatrix(point_diff);
                    SumMatrix(ref sum_res, mult_res, indexes.Count);
                }
                variance_i.Add(sum_res);
            }
            variance.Add(variance_i); 
        }
    }

    void FindMu()
    {   
        List<float[]> mu_i = new List<float[]>();
        for(int i=0; i<n_targets; i++)
        {
            List<int> indexes = new List<int>();

            for(int j=0; j<aligned_t.Count; j++)
                if(i==aligned_t[j].Last())
                    indexes.Add(j);
            if (!indexes.Any())
                continue;
            float x = 0.0f;
            float z = 0.0f;
            float yaw = 0.0f;
            float v_x = 0.0f;
            float v_z = 0.0f;

            mu_i = new List<float[]>();
            for (int k=0; k<aligned_t[0].Count; k++)
            {
                foreach (int id in indexes)
                {
                    int n_dem = indexes.Count;
                    x += aligned_x[id][k] / n_dem;
                    z += aligned_z[id][k] / n_dem;
                    yaw += aligned_rot[id][k] / n_dem;
                    v_x += aligned_vx[id][k] / n_dem;
                    v_z += aligned_vz[id][k] / n_dem;
                }
                mu_i.Add(new float[]{x, z, yaw, v_x, v_z});
            }
            mu.Add(mu_i);
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

    void SameLength()
    {
        List<List<List<float>>> vect = new List<List<List<float>>>(); 
        vect.Add(aligned_x); 
        vect.Add(aligned_z);
        vect.Add(aligned_vx);
        vect.Add(aligned_vz);
        vect.Add(aligned_t);
        vect.Add(aligned_rot); 

        for(int v=0; v < vect.Count; v++)
        {
            int min = Int32.MaxValue;
            int min_i = 0;
            for(int i=0; i < vect[v].Count; i++)
            {
                if (vect[v][i].Count < min)
                {
                    min = vect[v][i].Count;
                    min_i = i;
                }
            }
            for (int i=0; i<vect[v].Count; i++)
            {
                if (i==min_i)
                    continue;
                List<float> temp = new List<float>();
                temp.AddRange(vect[v][i].Where((s, i) => i < vect[v][min_i].Count));
                vect[v][i] = temp;
            }
        }     
    }
    void Align()
    {
        int max_len = 0;
        int max_ind = 0;
        for(int i=0; i<resample_targets.Count; i++)
            if (resample_targets[i].Count > max_len)
            {
                max_len = resample_targets[i].Count;
                max_ind = i;
            }

        for(int i=0; i<resample_targets.Count; i++)
        {
            if (i==max_ind)
            {
                aligned_x.Add(x_vect[i]);
                aligned_z.Add(z_vect[i]);
                aligned_vx.Add(vx_vect[i]);
                aligned_vz.Add(vz_vect[i]);
                aligned_rot.Add(resample_rotations[i]);
                aligned_t.Add(resample_targets[i]);
                continue;
            }
                
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
            
            float[] arr = new float[aligned_x[i].Count];
            for (int j = 0; j < aligned_x[i].Count; j++) {
                arr[j] = resample_targets[i].Last();
            }
            aligned_t.Add(new List<float>(arr));

        }
        
        SameLength();
                
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

        for (int i=0; i<resample_positions.Count; i++)
        {
            float[] arr = new float[resample_positions[i].Count];
            for (int j = 0; j < resample_positions[i].Count; j++) {
                arr[j] = targets[i].Last();
            }
            resample_targets.Add(new List<float>(arr));
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
    float OnHeading(Vector3 movement)
    {   
        float temp;
        if (movement.z == 1)
            temp = 90;
        else if (movement.z == -1) 
            temp = 270;
        else 
            temp = Mathf.Rad2Deg * Mathf.Atan(Mathf.Sin(movement.z)/Mathf.Cos(movement.x));
        return temp;
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

    void SavetoCSV(Vector3 new_position, Vector3 new_velocity, float yaw, List<float> probs)
    {   
        string delimiter = ","; 

        // Vector3 head_orientation = head.GetAngles();
        float[] output = {
            new_position.x,
            new_position.y,
            new_position.z,
            yaw,
            probs[0],
            probs[1],
            probs[2],
            probs[3],
            probs[4],
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
        var yaw = OnHeading(movement);
        var probs = CalculateProb();

        UpdatePositionList(curr_pos, velocity, yaw);
        

        int max_id = 0;
        float max_v = 0;
        for (int i = 0; i< probs.Count; i++)
        {
            if (probs[i] > max_v)
            {
                max_v = probs[i];
                max_id = i;
            }
        }   
        SavetoCSV(curr_pos, velocity, yaw, probs);
        Debug.Log("You are heading to target N.: " + max_id.ToString());
        

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
        int i = x.Length;
        int j = y.Length;
        
        List<int[]> path = new List<int[]>();
        
        while(i != 0 && j != 0)
        {   
            path.Add(new int[]{i-1, j-1});
            double left = double.PositiveInfinity;
            double up = double.PositiveInfinity;
            double diag = double.PositiveInfinity;
            if (j != 1)
                left = f[i, j-1];

            if (i != 1)
                up = f[i-1, j];

            if (i != 1 && j != 1)
                diag = f[i-1, j-1];
            
            if (diag <= left && diag <= up)
            {
                i -= 1;
                j -= 1;
            }
            else if (left < up)
                j -= 1;
            else i -= 1;

        }
        return path;
    }
    public (float[] x, float[] y) AlignedArrays()
    {
        List<int[]> path = CalculatePath();
        path.Reverse();

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
            new_x[i] = x[_x[i]];
    
        for(int i=0; i<_y.Length; i++)
            new_y[i] = y[_y[i]];

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

class GFG {
 
    public float DeterminantOfMatrix(float[, ] mat, int n)
    {
        float num1, num2, det = 1.0f, total = 1.0f; // Initialize result
        int index;
 
        // temporary array for storing row
        float[] temp = new float[n + 1];
 
        // loop for traversing the diagonal elements
        for (int i = 0; i < n; i++)
        {
            index = i; // initialize the index
 
            // finding the index which has non zero value
            while(index < n && mat[index, i] == 0)
            {
                index++;
            }
            if (index == n) // if there is non zero element
            {
                // the determinant of matrix as zero
                continue;
            }
            if (index != i)
            {
                // loop for swapping the diagonal element row
                // and index row
                for (int j = 0; j < n; j++)
                {
                    swap(mat, index, j, i, j);
                }
                // determinant sign changes when we shift
                // rows go through determinant properties
                det = (float)(det * Math.Pow(-1, index - i));
            }
 
            // storing the values of diagonal row elements
            for (int j = 0; j < n; j++)
            {
                temp[j] = mat[i, j];
            }
 
            // traversing every row below the diagonal
            // element
            for (int j = i + 1; j < n; j++)
            {
                num1 = temp[i]; // value of diagonal element
                num2 = mat[j,
                           i]; // value of next row element
 
                // traversing every column of row
                // and multiplying to every row
                for (int k = 0; k < n; k++)
                {
 
                    // multiplying to make the diagonal
                    // element and next row element equal
                    mat[j, k] = (num1 * mat[j, k])
                                - (num2 * temp[k]);
                }
                total = total * num1; // Det(kA)=kDet(A);
            }
        }
 
        // multiplying the diagonal elements to get
        // determinant
        for (int i = 0; i < n; i++)
        {
            det = det * mat[i, i];
        }
        return (det / total); // Det(kA)/k=Det(A);
    }
 
    public float[, ] swap(float[, ] arr, int i1, int j1, int i2,
                        int j2)
    {
        float temp = arr[i1, j1];
        arr[i1, j1] = arr[i2, j2];
        arr[i2, j2] = temp;
        return arr;
    }
}

class MInverse
{
	int N = 5;

    // Function to get cofactor of A[p,q] in [,]temp. n is current
    // dimension of [,]A
    void getCofactor(float [,] A, float [,] temp, int p, int q, int n)
    {
    	int i = 0, j = 0;

    	// Looping for each element of the matrix
    	for (int row = 0; row < n; row++)
    	{
    		for (int col = 0; col < n; col++)
    		{
    			// Copying into temporary matrix only those element
    			// which are not in given row and column
    			if (row != p && col != q)
    			{
    				temp[i, j++] = A[row, col];

    				// Row is filled, so increase row index and
    				// reset col index
    				if (j == n - 1)
    				{
    					j = 0;
    					i++;
    				}
    			}
    		}
    	}
    }

    /* Recursive function for finding determinant of matrix.
    n is current dimension of [,]A. */
    float determinant(float [,] A, int n)
    {
    	float D = 0; // Initialize result

    	// Base case : if matrix contains single element
    	if (n == 1)
    		return A[0, 0];

    	float [,] temp = new float [N, N]; // To store cofactors

    	int sign = 1; // To store sign multiplier

    	// Iterate for each element of first row
    	for (int f = 0; f < n; f++)
    	{
    		// Getting Cofactor of A[0,f]
    		getCofactor(A, temp, 0, f, n);
    		D += sign * A[0, f] * determinant(temp, n - 1);

    		// terms are to be added with alternate sign
    		sign = -sign;
    	}
    	return D;
    }

    // Function to get adjoint of A[N,N] in adj[N,N].
    void adjoint(float [,] A, float [,] adj)
    {
    	if (N == 1)
    	{
    		adj[0, 0] = 1;
    		return;
    	}

    	// temp is used to store cofactors of [,]A
    	int sign = 1;
    	float [,] temp = new float[N, N];

    	for (int i = 0; i < N; i++)
    	{
    		for (int j = 0; j < N; j++)
    		{
    			// Get cofactor of A[i,j]
    			getCofactor(A, temp, i, j, N);

    			// sign of adj[j,i] positive if sum of row
    			// and column indexes is even.
    			sign = ((i + j) % 2 == 0)? 1: -1;

    			// Interchanging rows and columns to get the
    			// transpose of the cofactor matrix
    			adj[j, i] = (sign) * (determinant(temp, N - 1));
    		}
    	}
    }

    // Function to calculate and store inverse, returns false if
    // matrix is singular
    public float [,] Inverse(float [,] A)
    {
        float [,]inverse = new float[N, N];
    	// Find determinant of [,]A
    	float det = determinant(A, N);
    	// if (det == 0)
    	// {
    	// 	// Console.Write("Singular matrix, can't find its inverse");
    	// 	// return false;
    	// }

    	// Find adjoint
    	float[,] adj = new float[N, N];
    	adjoint(A, adj);

    	// Find Inverse using formula "inverse(A) = adj(A)/det(A)"
    	for (int i = 0; i < N; i++)
    		for (int j = 0; j < N; j++)
            {
                if (det == 0)
                {
                    inverse[i, j] = float.MaxValue;
                    continue;
                }

    			inverse[i, j] = adj[i, j]/(float)det;
            }

    	return inverse;
    }

}


 