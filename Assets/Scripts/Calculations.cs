using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;

public class Calculations
{
    string dataset_path = @"Assets/Datasets/test.csv";

    List <List <float[]>> rec_positions = new List <List <float[]>>(); //variable for the recordings from the csv 
                                                                       //containing each trial -> each time step -> (x, y, z)
    List <List <float>> rec_rotations = new List <List <float>>(); //variable for the recordings from the csv 
                                                                   //containing each trial -> each time step -> yaw
    List <List <float>> targets = new List <List <float>>(); //variable for the recordings from the csv 
                                                             //containing each trial -> each time step -> target
    
    List <List <float[]>> resample_positions = new List <List <float[]>>(); //variable after resampling the recordings 
                                                                            //containing each trial -> each time step -> (x,z)
    List <List <float>> resample_rotations = new List <List <float>>(); //variable after resampling the recordings 
                                                                        //containing each trial -> each time step -> yaw
    List <List <float>> resample_targets = new List <List <float>>(); //variable after resampling the recordings 
                                                                      //containing each trial -> each time step -> target
    List <List <float[]>> velocity = new List <List <float[]>>(); //calculating the velocity from resampled positions 

    List<List<float>> x_vect = new List<List<float>>(); //unattach x value from resample positions
    List<List<float>> z_vect = new List<List<float>>(); //unattach z value from resample positions
    List<List<float>> vx_vect = new List<List<float>>(); //unattach x value from velocity
    List<List<float>> vz_vect = new List<List<float>>(); //unattach z value from velocity

    List<List<float>> aligned_x = new List<List<float>>(); //aligned array of x_vect after DTW
    List<List<float>> aligned_z = new List<List<float>>(); //aligned array of z_vect after DTW
    List<List<float>> aligned_vx = new List<List<float>>(); //aligned array of vx_vect after DTW
    List<List<float>> aligned_vz = new List<List<float>>(); //aligned array of vz_vect after DTW
    List<List<float>> aligned_t = new List<List<float>>(); //aligned array of resample_targets after DTW
    List<List<float>> aligned_rot = new List<List<float>>(); //aligned array of resample_rotations after DTW

    List<List<float[]>> mean = new List<List<float[]>>(); // mean of multivariate Gaussian distribution for each feature  
                                                          // at each time step in each trial
    List<List<float[,]>> variance = new List<List<float[,]>>(); // variance matrix of multivariate Gaussian distribution   
                                                                // for each feature at each time step in each trial
                                                                // with the shape of [n_features, n_features]
    List<List<float[,]>> inverse_variance = new List<List<float[,]>>(); // inverse of variance
    List<List<float>> determinant = new List<List<float>>(); // determinant of variance matrix

    int frame_rate = 90;
    int resample_freq = 100;
    int n_targets = 5;
    int n_features = 5; 

    void CSVParser()
    {   // reading from path csv and adding the data to rec_positions, rec_rotations and targets
        // each line contains id, x, y, z, yaw, target
        using(var reader = new StreamReader(dataset_path))
        {
            List <float[]> position_tmp = new List<float[]>();
            List <float> rotation_tmp = new List<float>();
            List <float> target_tmp = new List<float>();

            var id_prev = "0";
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine(); // read each line of file
                var values = line.Split(',');
                var id = values[0]; // take the first element as id of tial
                if (id_prev != id) // to seperate each trial
                {   
                    rec_positions.Add(position_tmp); // add list of the (x,y,z) for each time step to rec_positions after each trial
                    rec_rotations.Add(rotation_tmp); // add list of the yaw for each time step to rec_positions after each trial
                    targets.Add(target_tmp); // add list of the targets for each time step to rec_positions after each trial
                    position_tmp = new List<float[]>(); // clear the temporary list of positions for each time step 
                    rotation_tmp = new List<float>(); // clear the temporary list of rotations for each time step
                    target_tmp = new List<float>(); // clear the temporary list of targets for each time step
                }
                id_prev = id;

                float[] new_values = new float[values.Length-1]; // set a new array dropping the id
                float out_val = 0.0f;
                for (int i = 1; i < values.Length; i++) // convert each element of each line to float
                {
                    if (float.TryParse(values[i], out out_val)) 
                        new_values[i-1] = float.Parse(values[i]);
                }
                
                position_tmp.Add(new float[] {new_values[0], new_values[1], new_values[2]}); // add (x,y,z) of each time step to temporary file
                rotation_tmp.Add(new_values[3]); // add yaw of each time step to temporary file
                target_tmp.Add(new_values[4]); // add target of each time step to temporary file
            }
            rec_positions.Add(position_tmp); // add the temp list at the end of the file
            rec_rotations.Add(rotation_tmp); // add the temp list at the end of the file
            targets.Add(target_tmp); // add the temp list at the end of the file
        }
    }

    void CSVParserControl() 
    {   // control the output of CSVParser
        Debug.Log("Number of trials: " + targets.Count);

        List<int> unique_targets = new List<int>(); //finding unique targets (in this case: 0 to 4)
        for(int i=0; i<targets.Count; i++)
            unique_targets.Add((int) (targets[i].Last()));

        var q = from x in unique_targets //finding the count of each distinct target
                group x by x into g
                let count = g.Count()
                orderby count descending
                select new { Value = g.Key, Count = count };

        foreach (var x in q) 
        {   
            Debug.Log("Distinct number for target : " + x.Value + " is: " + x.Count);
        }
    }

    void Interpolate(float[] destination, int destFrom, int destTo, float valueFrom, float valueTo)
    {
        int destLength = destTo - destFrom;
        float valueLength = valueTo - valueFrom;
        // a linear interpolation to cover the extra indexes in the destination array
        for (int i = 0; i <= destLength; i++)
            destination[destFrom + i] = valueFrom + (valueLength * i)/destLength;
    }

    List<float> Resampling(List <float> r)
    {   
        float[] source = r.ToArray();
        // find the resample array size 
        int dest_size = (int) Math.Round((double) source.Length * resample_freq / frame_rate);
        float[] destination = new float[dest_size];
        destination[0] = source[0];
        int jPrevious = 0;

        // interpolate the in-between indexes  
        for (int i = 1; i < source.Length; i++)
        {
            int j = i * (destination.Length - 1)/(source.Length - 1);
            Interpolate(destination, jPrevious, j, source[i - 1], source[i]);

            jPrevious = j;
        }
        return new List<float>(destination);

    }

    void Resample()
    {   // resample the data at the resampling freq 
        // (it was one of the steps in the main paper, not sure if it is necessary in our case)

        // add to resample_rotations after resampling each trial
        foreach (var r in rec_rotations){
            List<float> resampled = Resampling(r);
            resample_rotations.Add(resampled);
        }

        
        foreach (var r in rec_positions){
            // distinct x and z of each trial into distinct arrays
            List <float> x_axis = new List <float>();
            List <float> z_axis = new List <float>();
            foreach (var p in r){
                x_axis.Add(p[0]);
                z_axis.Add(p[2]);
            }
            // resample x and z of each trial
            List<float> resampled1 = Resampling(x_axis);
            List<float> resampled2 = Resampling(z_axis);

            // add to resample_positions after resampling each trial as a new array
            List<float[]> resampled = new List <float[]>();
            for(int i=0; i < resampled1.Count; i++)
                resampled.Add(new float[] {resampled1[i], resampled2[i]});
            resample_positions.Add(resampled);
        }

        // extending the targets array to the new value
        // it doesn't need resampling
        for (int i=0; i<resample_positions.Count; i++)
        {
            float[] arr = new float[resample_positions[i].Count];
            for (int j = 0; j < resample_positions[i].Count; j++) {
                arr[j] = targets[i].Last();
            }
            resample_targets.Add(new List<float>(arr));
        }
    }

    void ResampleControl()
    {   // control the output of Resample
        for (int i = 0; i < resample_positions.Count; i++)
        {   
            if ((resample_positions[i].Count == resample_rotations[i].Count) && 
                (resample_positions[i].Count == resample_targets[i].Count))
                continue;
            else 
                throw new Exception("The resampling went wrong at the trial id: " + i);
        }
        Debug.Log("Resampling is done for " + resample_positions.Count + " trials.");
        Debug.Log("First trial resampling info as an example: \n" + 
                  "The original length: " + rec_positions[0].Count + "----" +
                  "The resampled length: " + resample_positions[0].Count + "----" +
                  "The presumed length: " + (int) (rec_positions[0].Count * resample_freq / frame_rate));
    }

    void Velocity()
    {   // 4th order Taylor expansion for the 1st derivative 
        float velocity_x;
        float velocity_z;
        // for each trial
        foreach(var p in resample_positions)
        {
            List<float[]> vel_tmp = new List<float[]>();
            // for each time-step 
            for(int i=0; i<p.Count; i++)
            {   // calculate velocity for x and z 
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
            // returning list of x-velocity and z-velocity for each time-step for each trial 
            velocity.Add(vel_tmp);
        }
    }

    void VelocityControl()
    {
        var p = resample_positions[0];
        var i = 5;
        var velocity_x = (3*p[i-4][0]-16*p[i-3][0]+36*p[i-2][0]-48*p[i-1][0]+25*p[i+0][0])/(12);
        Debug.Log("Velocities are calculated.");
        if (velocity_x != velocity[0][5][0])
            Debug.Log("Something is wrong with velocities.");
    }

    void Vectorize()
    {   // convert position and velocity float[,] to two seperate lists

        // for each trial
        for (int i=0; i<resample_positions.Count; i++)
        {   // declare temp variables
            List<float> _x = new List<float>();
            List<float> _z = new List<float>();
            List<float> _v_x = new List<float>();
            List<float> _v_z = new List<float>();

            // in each time step
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

    void VectorizeControl()
    {
        for ( int i = 0; i < resample_positions.Count; i++)
            for (int j = 0; j < resample_positions[i].Count; j++) 
                if (resample_positions[i][j][0] != x_vect[i][j] || 
                    resample_positions[i][j][1] != z_vect[i][j] ||
                    velocity[i][j][0] != vx_vect[i][j] ||
                    velocity[i][j][1] != vz_vect[i][j])
                    throw new Exception("Something is wrong with vectorizing in the trial " + i + 
                                        " at the time step " + j);
        Debug.Log("Vectorizing is done.");  
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

        for (int k = 0; k < n_targets; k++)
        {
            // find the trial ids of each target
            List<int> indexes = new List<int>();
            for (int l = 0; l<aligned_t.Count; l++)
                if (k == aligned_t[l].Last())
                    indexes.Add(l);

            int min = Int32.MaxValue;
            int min_i = 0;

            // find the minimum length in all features in all trials of the same target
            foreach (int i in indexes)
                for (int v = 0; v < vect.Count; v++)
                {
                    if (vect[v][i].Count < min) 
                    {
                        min = vect[v][i].Count;
                        min_i = i;
                    }
                } 

            // make them all the same size 
            foreach (int i in indexes)
            {
                for (int v = 0; v < vect.Count; v++)
                {
                    if (i==min_i)
                        continue;
                    List<float> temp = new List<float>();
                    temp.AddRange(vect[v][i].Where((s, i) => i < min));
                    vect[v][i] = temp;
                }
            } 
        }
    }

    void Align()
    {
        for (int k = 0; k < n_targets; k++)
        {
            // find the trial id of each target
            List<int> indexes = new List<int>();
            for (int l = 0; l<resample_targets.Count; l++)
                if (k == resample_targets[l].Last())
                    indexes.Add(l);
            
            int max_len = 0;
            int max_ind = 0;
            // find the longest trial
            foreach (int i in indexes)
                if (resample_targets[i].Count > max_len)
                {
                    max_len = resample_targets[i].Count;
                    max_ind = i;
                }
            // each trial with the same target as k
            foreach (int i in indexes)
            {
                if (i==max_ind)
                {   // ignore if it is the longest
                    aligned_x.Add(x_vect[i]);
                    aligned_z.Add(z_vect[i]);
                    aligned_vx.Add(vx_vect[i]);
                    aligned_vz.Add(vz_vect[i]);
                    aligned_rot.Add(resample_rotations[i]);
                    aligned_t.Add(resample_targets[i]);
                    continue;
                }
                // apply DTW to each trial and the longest one 
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

                // extend the target array to match the x array
                float[] arr = new float[aligned_x[i].Count];
                for (int j = 0; j < aligned_x[i].Count; j++) {
                    arr[j] = resample_targets[i].Last();
                }
                aligned_t.Add(new List<float>(arr));

            }
        }
        SameLength();
                
    }

    void AlignControl() 
    {
        List<int> unique_targets = new List<int>(); //finding unique targets (in this case: 0 to 4)
        for(int i=0; i < targets.Count; i++)
            unique_targets.Add((int) (targets[i].Last()));
        
        List<int> unique_aligned_targets = new List<int>(); //finding unique targets (in this case: 0 to 4)
        for(int i=0; i < aligned_t.Count; i++)
            unique_aligned_targets.Add((int) (aligned_t[i].Last()));

        var q = from x in unique_targets //finding the count of each distinct target
                group x by x into g
                let count = g.Count()
                orderby count descending
                select new { Value = g.Key, Count = count };
        
        var q2 = from x in unique_aligned_targets //finding the count of each distinct target
                group x by x into g
                let count = g.Count()
                orderby count descending
                select new { Value = g.Key, Count = count };

        foreach (var x in q) 
        {   
            foreach ( var y in q2)
                if (x.Value == y.Value)
                    if (x.Count != y.Count)
                        throw new Exception ("Something went wrong in aligning");
        }
        Debug.Log("Aligning is done.");

        for (int t = 0; t < n_targets; t++)
            for ( int i = 0; i < aligned_x.Count; i++)
                if (t == aligned_t[i].Last())
                {
                    Debug.Log("Target: " + aligned_t[i].Last() + "---" +
                              "aligned x length: " + aligned_x[i].Count + "---" +
                              "aligned z length: " + aligned_z[i].Count + "---" +
                              "aligned vx length: " + aligned_vx[i].Count + "---" +
                              "aligned vz length: " + aligned_vz[i].Count + "---" +
                              "aligned rot length: " + aligned_rot[i].Count
                              );
                    break;
                }
    }
    
    void FindMean()
    {   
        List<float[]> mean_temp = new List<float[]>();
        for(int i = 0; i < n_targets; i++)
        {   
            // find the indexes of trials with same target
            List<int> indexes = new List<int>();
            for(int j=0; j<aligned_t.Count; j++)
                if(i==aligned_t[j].Last())
                    indexes.Add(j);
            
            // stop with there is no trials for the target i
            if (!indexes.Any())
                continue;

            float x = 0.0f;
            float z = 0.0f;
            float yaw = 0.0f;
            float v_x = 0.0f;
            float v_z = 0.0f;

            mean_temp = new List<float[]>();
            // calculate the mean of all trials in the indexes for each time step k 
            for (int k = 0; k < aligned_t[indexes[0]].Count; k++)
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
                mean_temp.Add(new float[]{x, z, yaw, v_x, v_z});
            }
            mean.Add(mean_temp);
        }
    }
    
    void FindMeanControl()
    {
        if (mean.Count == n_targets)
            Debug.Log("Mean count: " + mean.Count + " equals to number of targets: " + n_targets + ".");
        else 
            throw new Exception("Something went wrong with the means.");

        for (int i = 0; i < n_targets ; i++)
            for (int j = 0; j < aligned_t.Count; j++)
            {
                if (i == aligned_t[j].Last())
                    if (mean[i].Count != aligned_t[j].Count)
                        throw new Exception("Something went wrong with the means.");
            }
        Debug.Log("The means calculation is done.");
        
        // for (int i = 0; i < mean.Count; i++)
        //     for (int j = 0; j < aligned_t.Count; j++)
        //         if (i == aligned_t[j].Last())
        //             Debug.Log(mean[i].Count + " " + aligned_x[j].Count + " " + 
        //                                             aligned_z[j].Count + " " + 
        //                                             aligned_rot[j].Count + " " + 
        //                                             aligned_vx[j].Count + " " + 
        //                                             aligned_vz[j].Count + " " 
        //                     );
    }

    float[,] MultMatrix(float[] mat)
    {   // mult a [n_features, 1] and its transpose
        float[,] res = new float[mat.Length, mat.Length];
        for(int i = 0; i < mat.Length; i++)
        {
            for(int j = 0; j < mat.Length; j++)
            {
                res[i, j] = mat[i] * mat[j];
            }
        }
        // returning a [n_features, n_features] matrix
        return res;
    }

    void SumMatrix(ref float[,] sum, float[,] mat, int n_dem)
    {   // sum two matrix of [n_features, n_features] 
        float[,] res = sum;
        for(int i = 0; i < n_features; i++)
        {
            for(int j = 0; j < n_features; j++)
            {
                res[i, j] += mat[i, j] / (n_dem - 1);
            }
        }
        sum = res;

    }

    void FindVariance()
    {
        // calculating the 4th formula in the early paper  
        for(int i = 0; i < n_targets; i++)
        {
            List<int> indexes = new List<int>();
            // find the trials with the same targets
            for(int j = 0; j < aligned_t.Count; j++)
                if(i == aligned_t[j].Last())
                    indexes.Add(j);
            
            if (indexes.Count <= 1)
            {
                throw new Exception("Class " + i.ToString() + " doesn't have enough records.");
            }

            List<float[,]> variance_temp = new List<float[,]>();
            
            // for each time step
            for(int j = 0; j < mean[i].Count; j++)
            {   
                float [,] sum_res = new float [n_features, n_features];

                // for each similiar trials
                foreach (int k in indexes)
                {
                    float[] arr = new float[]{
                        aligned_x[k][j],
                        aligned_z[k][j],
                        aligned_rot[k][j],
                        aligned_vx[k][j],
                        aligned_vz[k][j]
                    };
                    float[] point_diff = new float[n_features];
                    float[,] mult_res = new float[n_features, n_features];

                    // subtract each feature point in the k index in the j time step
                    // from the mean of the same target as trial at the j time step
                    // returns a n_features x 1 matrix 
                    for(int t = 0; t < arr.Length; t++)
                        point_diff[t] = arr[t] - mean[i][j][t]; 

                    // mult the subtraction matrix to its transpose
                    // returns a n_features x n_features matrix 
                    mult_res = MultMatrix(point_diff); 
                    
                    // sum all the demonstration at the same j time step
                    // returns a n_features x n_features matrix
                    SumMatrix(ref sum_res, mult_res, indexes.Count); 
                }
                variance_temp.Add(sum_res);
            }
            variance.Add(variance_temp); 
        }
    }
    
    void FindVarianceControl()
    {
        string str = "Variance matrix of target 0 at a time step as an example. \n";
        for (int i = 0; i < n_features; i++)
        {
            for (int j = 0; j < n_features; j++)
                str += variance[0][variance[0].Count - 1][i, j] + " ";
            str += "\n";
        }
        Debug.Log(str);
    }

    void CalculateInverse()
    {
        // for each target        
        for (int i=0; i<variance.Count; i++)
        {
            List<float[,]> i_var_temp = new List<float[,]>();

            // at each time step
            for (int j = 0; j < variance[i].Count; j++)
            {
                float [,] _i_var = new float[n_features, n_features];

                // calculating the inverse matrix
                MInverse inv = new MInverse();
                _i_var = inv.Inverse(variance[i][j]);
                i_var_temp.Add(_i_var);
            }
            inverse_variance.Add(i_var_temp);
        }   
    }

    void CalculateInverseControl()
    {
        float [,] _i_var = new float[n_features, n_features];        
        MInverse inv = new MInverse();
        _i_var = inv.Inverse(variance[0][variance[0].Count - 1]);

        string str = "Inverse variance matrix of target 0 at a time step as an example. \n";
        for (int i = 0; i < n_features; i++)
        {
            for (int j = 0; j < n_features; j++)
                str += _i_var[i, j] + " ";
            str += "\n";
        }
        Debug.Log(str);
    }

    void CalculateDet()
    {
        // traversing each target
        for(int i=0; i<variance.Count; i++)
        {
            List<float> det_temp = new List<float>();
            // traversing each time step
            for(int j=0; j<variance[i].Count; j++)
            {   // deep copy the variance
                float [,] m = variance[i][j].Clone() as float[,];

                // instance the class and calculate determinant
                MInverse dett = new MInverse();
                float _det = dett.determinant(m, n_features);
                det_temp.Add(_det);

                // ignore
                // if (Double.IsNaN(_det))
                //     det_temp.Add(0);
                // else 
                //     det_temp.Add(_det);
                
            }
            determinant.Add(det_temp);
        }
    }

    void CalculateDetControl()
    {
        for (int i = 0; i < determinant.Count; i++)      
            for (int j = 0; j < determinant[i].Count; j++)
                Debug.Log("det for target " + i + " at time step " + j + " is " + determinant[i][j]);
    }

    float CalculateDeltaVar(int id, int k, 
                            List<Vector3> positions, 
                            List<Vector3> velocities, 
                            List<float> rotations)
    {
        int N = n_targets;
        float[] arr = new float[]{
                        positions[k].x,
                        positions[k].z,
                        rotations[k],
                        velocities[k].x,
                        velocities[k].z};
        float [] new_arr = new float[N];
        for (int i=0; i < N; i++)
        {
            float temp = 0.0f;
            for(int j=0; j < N; j++)
            {
                temp += (arr[j]-mean[id][k][j]) * inverse_variance[id][k][j, i]; 
            }
            new_arr[i] = temp;
        }
        string str = "";
        foreach (var t in new_arr) 
            str += t + " ";
        // Debug.Log("new array: " + str);

        float res = 0.0f;
        for (int i=0; i < N; i++)
            res += new_arr[i] * (arr[i]-mean[id][k][i]);
        // Debug.Log(k + " res: " + res);
        if (Double.IsNaN(res))
            return 0;
        if (res == Mathf.Infinity)
            return float.MaxValue;
        return res;
    }

    List <float> CalculateProb(List<Vector3> positions, 
                               List<Vector3> velocities, 
                               List<float> rotations)
    {
        int N_f = n_features;
        List <float> target_pro = new List<float>();

        // calculate probability for each target (formula No.7 in the early paper)
        // j is annotating as k in the paper 
        // i is annotating as t in the ppaer  
        for (int i = 0; i < n_targets; i++)
        {   
            double temp = 0.0f;
             
            for(int j = 0; j < positions.Count; j++)
            {
                int jj = j;
                // Debug.Log("jj is " + jj);
                if (j >= determinant[i].Count)
                    jj = determinant[i].Count - 1;

                if (determinant[i][jj] < 1)
                    temp += (- 0.5 * (CalculateDeltaVar(i, jj, positions, velocities, rotations))) / positions.Count;
                else 
                    temp += (double) ((-1.0 * Math.Log(Math.Pow(2 * Math.PI, N_f/2) * Math.Pow(determinant[i][jj], 0.5)) 
                                       - 0.5 * CalculateDeltaVar(i, jj, positions, velocities, rotations)) 
                                       / positions.Count
                                     );
                // Debug.Log(" target " + i + " sum at time step " + j + " from " + jj + " is " + temp);
            }
            // Debug.Log("total value at target " + i + " is " + temp);
            target_pro.Add((float) temp);
        }
        // Debug.Log("target pro " + target_pro.Count);
        return target_pro;
    }

    public void Main(String[] args) 
    {
        CSVParser();
        CSVParserControl();
        Resample();
        ResampleControl();
        Velocity();
        VelocityControl();
        Vectorize();
        VectorizeControl();
        Align();
        AlignControl();
        FindMean();
        FindMeanControl();
        FindVariance();
        FindVarianceControl();
        CalculateInverse();
        CalculateInverseControl();
        CalculateDet();

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> velocities = new List<Vector3>();
        List<float> rotations = new List<float>();

        for (int t = 0; t < aligned_t.Count; t++)
        {
            int tr = t; // select the trial 0:24
            int time_step = aligned_t[tr].Count; // select the time-step 
                                                 // default: end of the trial

            Debug.Log("Presumed target is of trial: " + t + " is " + aligned_t[tr].Last());
            for (int i = 0; i < time_step; i++)
            {
                positions.Add(new Vector3(aligned_x[tr][i], 0, aligned_z[tr][i])); 
                velocities.Add(new Vector3(aligned_vx[tr][i], 0, aligned_vz[tr][i]));
                rotations.Add(aligned_rot[tr][i]);
            }

            var probabilities = CalculateProb(positions, velocities, rotations);
            List<float> p_normalized = new List<float>();


            int max_id = probabilities.IndexOf(probabilities.Max());

            string str = "The most probable target is: " + max_id + 
                         "\n" + 
                         "The probabilities are: \n";

            for (int p = 0 ; p < probabilities.Count; p++)
                str += "Target" + p + ": " + probabilities[p] + "\t";
            Debug.Log(str);

            if (max_id == ((int) aligned_t[tr].Last()))
                Debug.Log("+++++ CORRECT +++++");
            else 
                Debug.Log("---- INCORRECT ----");
        }
    }
}

// class dynamic time warping
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

// class matrix determinant
class MDeterminant {
 
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

// class matrix inverse
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
    public float determinant(float [,] A, int n)
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
        float [,] inverse = new float[N, N];
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