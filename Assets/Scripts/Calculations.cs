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
    string dataset_path = @"Assets/Datasets/train.csv";
    bool debug_mode = false;

    List <List <float[]>> rec_positions = new List <List <float[]>>(); //variable for the recordings from the csv 
                                                                       //containing each trial -> each time step -> (x, y, z)
    List <List <float>> rec_rotations = new List <List <float>>(); //variable for the recordings from the csv 
                                                                   //containing each trial -> each time step -> yaw
    List <List <float>> rec_brotations = new List <List <float>>(); //variable for the recordings from the csv 
                                                                   //containing each trial -> each time step -> body rotation
    List <List <float>> targets = new List <List <float>>(); //variable for the recordings from the csv 
                                                             //containing each trial -> each time step -> target
    
    List <List <float[]>> resample_positions = new List <List <float[]>>(); //variable after resampling the recordings 
                                                                            //containing each trial -> each time step -> (x,z)
    List <List <float>> resample_rotations = new List <List <float>>(); //variable after resampling the recordings 
                                                                        //containing each trial -> each time step -> yaw
    List <List <float>> resample_brotations = new List <List <float>>(); //variable after resampling the recordings 
                                                                        //containing each trial -> each time step -> body rotation
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
    List<List<float>> aligned_brot = new List<List<float>>(); //aligned array of resample_brotations after DTW

    List<List<float[]>> mean = new List<List<float[]>>(); // mean of multivariate Gaussian distribution for each feature  
                                                          // at each time step in each trial
    List<List<float[,]>> variance = new List<List<float[,]>>(); // variance matrix of multivariate Gaussian distribution   
                                                                // for each feature at each time step in each trial
                                                                // with the shape of [n_features, n_features]
    List<List<float[,]>> inverse_variance = new List<List<float[,]>>(); // inverse of variance
    List<List<float>> determinant = new List<List<float>>(); // determinant of variance matrix

    int last_target_id = 1000;

    int frame_rate = 90;
    int resample_freq = 100;
    public int n_targets = 8;
    int n_features = 5; 
    int test_size = 80;  
    bool body_torso;
    bool scaling;

    public Calculations(bool BodyTorso, string dataset, bool ScalingProbability)
    {   
        if (BodyTorso)
        {
            body_torso = BodyTorso;
            n_features = 6;
        }
        
        scaling = ScalingProbability;

        dataset_path = $"Assets/Datasets/train - {dataset}.csv";
        Debug.Log($"Training from {dataset_path} ...");
    }

    public void CSVParser()
    {   // reading from path csv and adding the data to rec_positions, rec_rotations and targets
        // each line contains id, x, y, z, yaw, target
        using(var reader = new StreamReader(dataset_path))
        {
            List <float[]> position_tmp = new List<float[]>();
            List <float> rotation_tmp = new List<float>();
            List <float> brotation_tmp = new List<float>();
            List <float> target_tmp = new List<float>();

            var id_prev = "0";
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine(); // read each line of file
                var values = line.Split(',');
                // first element is timestamp
                var id = values[1]; // take the first element as id of tial
                if (id_prev != id) // to seperate each trial
                {   
                    rec_positions.Add(position_tmp); // add list of the (x,y,z) for each time step to rec_positions after each trial
                    rec_rotations.Add(rotation_tmp); // add list of the yaw for each time step to rec_positions after each trial
                    rec_brotations.Add(brotation_tmp); // add list of the body rotation for each time step to rec_positions after each trial
                    targets.Add(target_tmp); // add list of the targets for each time step to rec_positions after each trial
                    
                    position_tmp = new List<float[]>(); // clear the temporary list of positions for each time step 
                    rotation_tmp = new List<float>(); // clear the temporary list of rotations for each time step
                    brotation_tmp = new List<float>(); // clear the temporary list of brotations for each time step
                    target_tmp = new List<float>(); // clear the temporary list of targets for each time step
                }
                id_prev = id;

                float[] new_values = new float[values.Length-2]; // set a new array dropping the timestamp and id
                float out_val = 0.0f;
                for (int i = 2; i < values.Length; i++) // convert each element of each line to float
                {
                    if (float.TryParse(values[i], out out_val)) 
                        new_values[i-2] = float.Parse(values[i]);
                }
                
                position_tmp.Add(new float[] {new_values[0], new_values[1], new_values[2]}); // add (x,y,z) of each time step to temporary file
                rotation_tmp.Add(new_values[3]); // add yaw of each time step to temporary file
                brotation_tmp.Add(new_values[4]); // add body rotation of each time step to temporary file
                target_tmp.Add(new_values[5]); // add target of each time step to temporary file
            }
            rec_positions.Add(position_tmp); // add the temp list at the end of the file
            rec_rotations.Add(rotation_tmp); // add the temp list at the end of the file
            rec_brotations.Add(brotation_tmp); // add the temp list at the end of the file
            targets.Add(target_tmp); // add the temp list at the end of the file

        }
        // // temporary files to select same number of records for each target
        // List <List <float[]>> trec_positions = new List <List <float[]>>();
        // List <List <float>> trec_rotations = new List <List <float>>();
        // List <List <float>> trec_brotations = new List <List <float>>(); 
        // List <List <float>> ttargets = new List <List <float>>(); 

        // List<int> unique_targets = new List<int>(); //finding unique targets (in this case: 0 to 7)
        // for(int i = 0; i < targets.Count; i++) {
        //     unique_targets.Add((int) (targets[i].Last())); } 
        // unique_targets.Sort();

        // var q = from x in unique_targets //finding the count of each distinct target
        //         group x by x into g
        //         let count = g.Count()
        //         orderby count descending
        //         select new { Value = g.Key, Count = count };
        
        // int min = Int32.MaxValue;
        // foreach (var x in q) 
        // {
        //     if  (x.Count < min)
        //         min = x.Count;
        // }
        // for (int i  = 0; i < unique_targets.Count; i++)
        // {
        //     int n_rec = 1;
        //     for (int j = 0; j < targets.Count; j++)
        //     {
        //         if (n_rec == min)
        //             continue;
        //         if (targets[j].Last() == unique_targets[i])
        //         {
        //             trec_positions.Add(rec_positions[j]);
        //             trec_rotations.Add(rec_rotations[j]);
        //             trec_brotations.Add(rec_brotations[j]);
        //             ttargets.Add(targets[j]);
        //             n_rec++;
        //         }
        //     }
        // }
        // rec_positions = trec_positions;
        // rec_rotations = trec_rotations;
        // rec_brotations = trec_brotations; 
        // targets = ttargets;

    }

    void CSVParserControl() 
    {   // control the output of CSVParser
        Debug.Log("Total number of trials: " + targets.Count);

        List<int> unique_targets = new List<int>(); //finding unique targets (in this case: 0 to 7)
        for(int i = 0; i < targets.Count; i++) {
            unique_targets.Add((int) (targets[i].Last())); } 
        unique_targets.Sort();

        var q = from x in unique_targets //finding the count of each distinct target
                group x by x into g
                let count = g.Count()
                orderby count descending
                select new { Value = g.Key, Count = count };
        
        string str = "Number of trials for each target: \n";
        foreach (var x in q) 
            str += "Target " + x.Value + ": " + x.Count + " ~~~ ";
            
        Debug.Log(str);
    }

    void RemoveDuplicates()
    {
        List <List <float[]>> positions_temp = new List <List <float[]>>(); 
        List <List <float>> rotations_temp = new List <List <float>>(); 
        List <List <float>> targets_temp = new List <List <float>>(); 

        for (int i = 0; i < rec_positions.Count; i++)
        {
            float threshold = 0.00002f;
            List <float[]> pos_temp = new List <float[]>(); 
            List <float> rot_temp = new List <float>();
            List <float> tar_temp = new List <float>();

            for (int j = 0; j < rec_positions[i].Count; j++)
            {
                if (j == 0)
                {
                    pos_temp.Add(rec_positions[i][j]);
                    rot_temp.Add(rec_rotations[i][j]);
                    tar_temp.Add(targets[i][j]);
                }
                else 
                {
                    if (Math.Abs(rec_positions[i][j][0] - rec_positions[i][j - 1][0]) > threshold || 
                        Math.Abs(rec_positions[i][j][2] - rec_positions[i][j - 1][2]) > threshold || 
                        Math.Abs(rec_rotations[i][j] - rec_rotations[i][j - 1]) > threshold)
                    {
                        pos_temp.Add(rec_positions[i][j]);
                        rot_temp.Add(rec_rotations[i][j]);
                        tar_temp.Add(targets[i][j]);
                    }
                }
            }

            positions_temp.Add(pos_temp);
            rotations_temp.Add(rot_temp);
            targets_temp.Add(tar_temp);
        }
        rec_positions = positions_temp;
        rec_rotations = rotations_temp;
        targets = targets_temp;
    }

    void RemoveDuplicatesControl()
    {
        string str = "";
        for (int i = 0; i < rec_positions.Count; i++)
            str += "Trial " + i + " length: " + rec_positions[i].Count + "\t";
        Debug.Log(str);
        
        // using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/duplicate.csv"))
        // {
        //     for ( int i = 0; i < rec_positions[13].Count; i++)
        //     {
        //         string res = string.Join(",", targets[13][i]) + Environment.NewLine;
        //         file.Write(res);
        //     }
        // }   
    }

    public List <float> Downsampling(float[] array, int Length)
    {
        int insert = 0;
        float[] window = new float[Length];
        float[] window_x = new float[Length];
        int bucket_size_less_start_and_end = Length - 2;

        float bucket_size = (float)(array.Length - 2) / bucket_size_less_start_and_end; 
        int a = 0;
        int next_a = 0;
        int max_area_point_x = 0;
        float max_area_point_y = 0f;
        window[insert] = array[a]; // Always add the first point
        window_x[insert] = 0;
        insert++;
        for (int i = 0; i < bucket_size_less_start_and_end; i++)
        {
            // Calculate point average for next bucket (containing c)
            float avg_x = 0;
            float avg_y = 0;
            int start = (int)(Math.Floor((i + 1) * bucket_size) + 1);
            int end = (int)(Math.Floor((i + 2) * bucket_size) + 1);
            if (end >= array.Length)
            {
                end = array.Length;
            }
            int span = end - start;
            for (; start < end; start++)
            {
                avg_x += start;
                avg_y += array[start];
            }
            avg_x /= span;
            avg_y /= span;

            // Get the range for this bucket
            int bucket_start = (int)(Math.Floor((i + 0) * bucket_size) + 1);
            int bucket_end = (int)(Math.Floor((i + 1) * bucket_size) + 1);

            // Point a
            float a_x = a;
            float a_y = array[a];
            float max_area = -1;
            for (; bucket_start < bucket_end; bucket_start++)
            {
                // Calculate triangle area over three buckets
                float area = Math.Abs((a_x - avg_x) * (array[bucket_start] - a_y) - (a_x - (float)bucket_start) * (avg_y - a_y)) * 0.5f;
                if (area > max_area)
                {
                    max_area = area;
                    max_area_point_x = bucket_start;
                    max_area_point_y = array[bucket_start];
                    next_a = bucket_start; // Next a is this b
                }
            }
            // Pick this point from the Bucket
            window[insert] = max_area_point_y;
            window_x[insert] = max_area_point_x;
            insert++;

            // Current a becomes the next_a (chosen b)
            a = next_a;
        }

        window[insert] = array[array.Length - 1]; // Always add last
        window_x[insert] = array.Length;

        return  new List<float>(window);
    }

    void Downsample()
    {

        List <List <float[]>> positions_temp = new List <List <float[]>>(); 
        List <List <float>> rotations_temp = new List <List <float>>(); 
        List <List <float>> brotations_temp = new List <List <float>>(); 
        List <List <float>> targets_temp = new List <List <float>>(); 

        int min_id = test_size;
        for (int i = 0; i < rec_positions.Count; i++)
            if (rec_positions[i].Count < min_id && 
                rec_positions[i].Count > test_size)
                min_id = rec_positions[i].Count;
        test_size = min_id; 
        var counts = rec_positions.Select(x => x.Count);
        double average = counts.Average();
        test_size = (int) Math.Floor(average);

        Debug.Log("Each test size Length is " + test_size);
        Debug.Log("Average size Length is " + average);

        for (int i = 0; i < rec_positions.Count; i++)
        {
            List <float> downsampling_x = new List <float>();
            List <float> downsampling_z = new List <float>();
            List <float> downsampling_rot = new List <float>();
            List <float> downsampling_brot = new List <float>();
            List <float> downsampling_target = new List <float>();

            List <float> x = new List<float>();
            List <float> z = new List<float>();

            for (int j = 0; j < rec_positions[i].Count; j++)
            {
                x.Add(rec_positions[i][j][0]);
                z.Add(rec_positions[i][j][2]);
            }

            if (rec_positions[i].Count < test_size) 
            {
                // resample_freq = test_size;
                downsampling_x = Resampling(x, test_size); 
                downsampling_z = Resampling(z, test_size); 
                downsampling_rot = Resampling(rec_rotations[i], test_size); 
                downsampling_brot = Resampling(rec_brotations[i], test_size); 
                downsampling_target = Resampling(targets[i], test_size);
            }
            else 
            {
                downsampling_x = Downsampling(x.ToArray(), test_size); 
                downsampling_z = Downsampling(z.ToArray(), test_size); 
                downsampling_rot = Downsampling(rec_rotations[i].ToArray(), test_size); 
                downsampling_brot = Downsampling(rec_brotations[i].ToArray(), test_size); 
                downsampling_target = Downsampling(targets[i].ToArray(), test_size); 
            }

            List <float[]> downsampled = new List <float[]>();
            for(int j = 0; j < test_size; j++)
                downsampled.Add(new float[] {downsampling_x[j], downsampling_z[j]});
            
            positions_temp.Add(downsampled);
            rotations_temp.Add(downsampling_rot);
            brotations_temp.Add(downsampling_brot);
            targets_temp.Add(downsampling_target);
        }
        rec_positions = positions_temp;
        rec_rotations = rotations_temp;
        rec_brotations = brotations_temp;
        targets = targets_temp;
    }

    void DownsampleControl()
    {
        // string str = "";
        // for (int i = 0; i < rec_positions.Count; i++)
        // {
        //     str += "trial " + i + " positions " + rec_positions[i].Count + " rotations " + rec_rotations[i].Count 
        //                 + " targets " + targets[i].Count + "\n";
                
        // }
        // Debug.Log(str);

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/downsample.csv"))
        {   
            for (int tr = 0; tr < 5; tr++)
                for ( int i = 0; i < rec_positions[tr].Count; i++)
                {
                    string res = tr + "," + string.Join(",", rec_positions[tr][i]) +  Environment.NewLine;
                    file.Write(res);
                }
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

    List<float> Resampling(List <float> r, int _dest_size)
    {   
        float[] source = r.ToArray();
        // find the resample array size 
        // int dest_size = (int) Math.Round((double) source.Length * resample_freq / frame_rate);
        int dest_size = _dest_size;

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
            List<float> resampled = Resampling(r, test_size);
            resample_rotations.Add(resampled);
        }

        
        foreach (var r in rec_positions){
            // distinct x and z of each trial into distinct arrays
            List <float> x_axis = new List <float>();
            List <float> z_axis = new List <float>();
            foreach (var p in r){
                x_axis.Add(p[0]);
                z_axis.Add(p[1]);
            }
            // resample x and z of each trial
            List<float> resampled1 = Resampling(x_axis, test_size);
            List<float> resampled2 = Resampling(z_axis, test_size);

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
            for(int i = 0; i < p.Count; i++)
            {   // calculate velocity for x and z 
                if(i >= 5)
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
        // string str = "";
        // for (int i = 0; i < resample_positions.Count; i++)
        //     for (int j = 0; j < resample_positions[i].Count; j++)
        //     {   
        //         str += velocity[i][j][0] + " " + velocity[i][j][1] + "\n"; 
        //     }
        // Debug.Log(str);
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/velocity.csv"))
        {
            for ( int i = 0; i < resample_positions[0].Count; i++)
            {
                string res = string.Join(",", velocity[0][i]) + Environment.NewLine;
                file.Write(res);
            }
        }   

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
        // Debug.Log("Vectorizing is done.");  

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/vectors.csv"))
        {   
            for (int tr = 0; tr < x_vect.Count; tr++)
                for ( int i = 0; i < x_vect[tr].Count; i++)
                {
                    string res = tr + "," + x_vect[tr][i] + "," + 
                                            z_vect[tr][i] + "," + 
                                            resample_rotations[tr][i] + "," + 
                                            vx_vect[tr][i] + "," + 
                                            vz_vect[tr][i] + "," +
                                            Environment.NewLine;
                    file.Write(res);
                }
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
                    // temp.AddRange(vect[v][i].Where((s, i) => i < min));
                    for (int ii = Math.Max(0, vect[v][i].Count - min); ii < vect[v][i].Count; ++ii)
                    {
                        temp.Add(vect[v][i][ii]);
                    }
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
            for(int j = 0; j < aligned_t.Count; j++)
                if(i == aligned_t[j].Last())
                    indexes.Add(j);
            
            // stop with there is no trials for the target i
            if (!indexes.Any())
                continue;

            
            mean_temp = new List<float[]>();
            // calculate the mean of all trials in the indexes for each time step k 
            for (int k = 0; k < test_size; k++)
            {
                float x = 0.0f;
                float z = 0.0f;
                float yaw = 0.0f;
                float byaw = 0.0f;
                float v_x = 0.0f;
                float v_z = 0.0f;

                foreach (int id in indexes)
                {
                    int n_dem = indexes.Count;
                    x += aligned_x[id][k] / n_dem;
                    z += aligned_z[id][k] / n_dem;
                    yaw += aligned_rot[id][k] / n_dem;
                    byaw += aligned_brot[id][k] / n_dem;
                    v_x += aligned_vx[id][k] / n_dem;
                    v_z += aligned_vz[id][k] / n_dem;
                }
                if (body_torso)
                    mean_temp.Add(new float[]{x, z, yaw, byaw, v_x, v_z});    
                else
                    mean_temp.Add(new float[]{x, z, yaw, v_x, v_z});
            }
            mean.Add(mean_temp);
        }
    }
    
    void FindMeanControl()
    {
        // if (mean.Count == n_targets)
        //     Debug.Log("Mean count: " + mean.Count + " equals to number of targets: " + n_targets + ".");
        // else 
        //     throw new Exception("Something went wrong with the means.");

        // for (int i = 0; i < n_targets ; i++)
        //     for (int j = 0; j < aligned_t.Count; j++)
        //     {
        //         if (i == aligned_t[j].Last())
        //             if (mean[i].Count != aligned_t[j].Count)
        //                 throw new Exception("Something went wrong with the means.");
        //     }
        // Debug.Log("The means calculation is done.");
        
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/mean.csv"))
        {
            for (int t = 0; t < mean.Count; t++)
                for ( int i = 0; i < mean[t].Count; i++)
                {
                    string res = t + "," + string.Join(",", mean[t][i]) + Environment.NewLine;
                    file.Write(res);
                }
        }   
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
                    float[] arr = new float[n_features];
                    if (body_torso)
                        arr = new float[]{
                            aligned_x[k][j],
                            aligned_z[k][j],
                            aligned_rot[k][j],
                            aligned_brot[k][j],
                            aligned_vx[k][j],
                            aligned_vz[k][j]
                        };
                    else 
                        arr = new float[]{
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
 
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/variance.csv"))
        {
            for (int t = 0; t < variance.Count; t++)
                for ( int i = 0; i < variance[t].Count; i++)
                {
                    string res = t + ",";
                    for(int ii = 0; ii < n_features; ii++)
                        for(int jj = 0; jj < n_features; jj++)
                            res += variance[t][i][ii, jj] + ",";

                    res += "\n";
                    file.Write(res);
                }
        }   
    }

    void CalculateInverse()
    {
        // for each target        
        for (int i = 0; i < variance.Count; i++)
        {
            List<float[,]> i_var_temp = new List<float[,]>();

            // at each time step
            for (int j = 0; j < variance[i].Count; j++)
            {
                // Debug.Log("n_features " + n_features);
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
        // Debug.Log(inverse_variance[0][0].GetLength(0) + " " + inverse_variance[0][0].GetLength(1));
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/inverse variance.csv"))
        {
            for (int t = 0; t < inverse_variance.Count; t++)
                for ( int i = 0; i < inverse_variance[t].Count; i++)
                {
                    string res = t + ",";
                    for(int ii = 0; ii < n_features; ii++)
                        for(int jj = 0; jj < n_features; jj++) { 
                            // Debug.Log(ii + " " + jj + " " + t + " " + i + " " + n_features);
                            res += inverse_variance[t][i][ii, jj] + ","; } 

                    res += "\n";
                    file.Write(res);
                }
        }   
    }

    void CalculateDet()
    {
        // traversing each target
        for(int i = 0; i < variance.Count; i++)
        {
            List<float> det_temp = new List<float>();
            // traversing each time step
            for(int j = 0; j < variance[i].Count; j++)
            {   // deep copy the variance
                float [,] m = variance[i][j].Clone() as float[,];

                // instance the class and calculate determinant
                MInverse dett = new MInverse();
                float _det = dett.determinant(m, n_features);
                det_temp.Add(_det);                
            }
            determinant.Add(det_temp);
        }
    }

    void CalculateDetControl()
    {
        // for (int i = 0; i < determinant.Count; i++)      
        //     for (int j = 0; j < determinant[i].Count; j++)
        //         Debug.Log("det for target " + i + " at time step " + j + " is " + determinant[i][j]);
        
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Assets/Log/determinant.csv"))
        {   
            for (int t = 0; t < determinant.Count; t++)
                for ( int i = 0; i < determinant[t].Count; i++)
                {
                    
                    string res = t + "," + determinant[t][i] + Environment.NewLine;
                    file.Write(res);
                }
        }   
    }

    int FindNearest(Vector3 position, int id)
    {
        double min_distance = float.MaxValue;
        int min_id = 0;

        for(int i = 0; i < mean[id].Count; i++)
        {
            double distance = Math.Sqrt(Math.Pow(position.x - mean[id][i][0], 2) + 
                                        Math.Pow(position.z - mean[id][i][1], 2)
                                        );
            if (distance < min_distance)
            {
                min_distance = distance;
                min_id = i;
            }
        }
        return min_id;
    }

    float CalculateDeltaVar(int id, int k, int near,
                            List<Vector3> positions, 
                            List<Vector3> velocities, 
                            List<float> rotations,
                            List<float> brotations)
    {
        if (determinant[id][near] == 0)
            return -50;
            // return 0;

        int N = n_features;

        float[] arr = new float[N];
        if (body_torso)
            arr = new float[]{
                            positions[k].x,
                            positions[k].z,
                            rotations[k],
                            brotations[k],
                            velocities[k].x,
                            velocities[k].z};
        else
            arr = new float[]{
                            positions[k].x,
                            positions[k].z,
                            rotations[k],
                            velocities[k].x,
                            velocities[k].z};

        float [] new_arr = new float[N];
        for (int i = 0; i < N; i++)
        {
            float temp = 0.0f;
            for(int j = 0; j < N; j++)
            {   
                temp += (arr[j]-mean[id][near][j]) * inverse_variance[id][near][j, i]; 
            }
            new_arr[i] = temp;
        }

        float res = 0.0f;
        for (int i = 0; i < N; i++)
            res += new_arr[i] * (arr[i]-mean[id][near][i]);

        return -0.5f * res;
    }
    
    double G_term(int i, int k)
    {
        int N_f = n_features;
        var det = Math.Abs(determinant[i][k]);
        if (det == 0)
            return -50;
            // return 0;

        int exponent = det == 0 ? 0 : (int) Math.Floor((Math.Log10(det)));
        var mantissa = det * Math.Pow(10, -exponent);

        var log_det = -0.5 * (Math.Log10(mantissa) + exponent);

        double g = Math.Log10(Math.Pow(2 * Math.PI, N_f/2)) 
                   + log_det;

        return -g;
    }

    List <float> CalculateProb(List<Vector3> positions, 
                               List<Vector3> velocities, 
                               List<float> rotations,
                               List<float> brotations)
    {
        List <float> target_pro = new List<float>();

        // calculate probability for each target (formula No.7 in the early paper)
        // j is annotating as k in the paper 
        // i is annotating as t in the paper  
        for (int i = 0; i < n_targets; i++)
        {   
            double temp = 0.0f;
            
            // for(int j = 0; j < positions.Count; j++)
            for(int j = Math.Max(0, positions.Count - 50); j < positions.Count; j++)
            {
                int k = FindNearest(positions[j], i);

                var delta_var_delta = CalculateDeltaVar(i, j, k, positions, velocities, rotations, brotations);
                var g = G_term(i, k);
                var pro = (double) ((g + delta_var_delta) / positions.Count);
                if (scaling) pro /= Math.Pow(2, (positions.Count - j)/5 + 1);
                // if (scaling && j < 5) pro /= (positions.Count - j + 1);
                // if (scaling && j >= (positions.Count - 3)) 
                //     pro /= Math.Pow(2, positions.Count - j);
                // else if (scaling) 
                //     pro /= 10;
                temp += pro;
            }
            target_pro.Add((float) temp);
            
        }

        return target_pro;
    }

    void Test()
    {
        int corrects = 0, incorrects = 0;

        for (int t = 0; t < aligned_t.Count; t++) //aligned_t.Count
        {
            int tr = t; // select the trial 0:24
            int time_step = aligned_t[tr].Count; // select the time-step 
                                                 // default: end of the trial
            
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> velocities = new List<Vector3>();
            List<float> rotations = new List<float>();
            List<float> brotations = new List<float>();

            for (int i = 0; i < time_step; i++)
            {
                positions.Add(new Vector3(aligned_x[tr][i], 0, aligned_z[tr][i])); 
                velocities.Add(new Vector3(aligned_vx[tr][i], 0, aligned_vz[tr][i]));
                rotations.Add(aligned_rot[tr][i]);
                brotations.Add(aligned_brot[tr][i]);
            }

            var probabilities = CalculateProb(positions, velocities, rotations, brotations);
            List<float> p_normalized = new List<float>();


            int max_id = probabilities.IndexOf(probabilities.Max());

            string str = "Presumed target is of trial: " + t + " is " + aligned_t[tr].Last() + "\n" +
                         "The most probable target is: " + max_id + "\n" + 
                         "The probabilities are: \n";

            for (int p = 0 ; p < probabilities.Count; p++)
                str += "Target " + p + ": " + probabilities[p] + " ~~~ ";
            Debug.Log(str);

            if (max_id == ((int) aligned_t[tr].Last()))
            {   
                corrects++;
                Debug.Log("+++++ CORRECT +++++");
            }
            else
            { 
                incorrects++;
                Debug.Log("---- INCORRECT ----");
            }
        }

        Debug.Log("Number of correct predictions: " + corrects + "\n" 
                + "Number of incorrect predictions: " + incorrects);
    }

    public List <float> CalculateOnRun(List<Vector3> positions, 
                               List<Vector3> velocities, 
                               List<float> rotations,
                               List<float> brotations)
    {   
        // int max_id = 0;
        // float max_value = float.NegativeInfinity;
        var probabilities = CalculateProb(positions, velocities, rotations, brotations);
        
        // for (int i = 0; i < probabilities.Count; i++)
        //     if (probabilities[i] > max_value)
        //     {
        //         max_value = probabilities[i];
        //         max_id = i;
        //     }

        int max_id = probabilities.IndexOf(probabilities.Max());
        var sum_list = Math.Abs(probabilities.Sum());
        var p_normalized = probabilities.Select(x => x/sum_list).ToArray();
        probabilities = new List <float> (p_normalized);

        if (!Double.IsNaN(probabilities[0]))
            if (max_id != last_target_id)
            {
                last_target_id = max_id;

                string str = "Heading target is: " + max_id + "\n" 
                             + "The probabilities are: ";
                for (int p = 0 ; p < probabilities.Count; p++) 
                    str += "Target " + p + ": " + probabilities[p].ToString("F3") + "\t"; 
                Debug.Log(str);

            }

        return probabilities;
 
    }

    public void Train() 
    {
        if (debug_mode)
        {
            CSVParser();
            CSVParserControl();
            // RemoveDuplicates();
            // RemoveDuplicatesControl();
            Downsample();
            DownsampleControl();
            // Resample();
            // ResampleControl();

            resample_positions = rec_positions;  
            resample_rotations = rec_rotations;
            resample_brotations = rec_brotations;
            resample_targets = targets;

            Velocity();
            VelocityControl();
            Vectorize();
            VectorizeControl();
            aligned_x = x_vect;
            aligned_z = z_vect;
            aligned_rot = resample_rotations;
            aligned_brot = resample_brotations;
            aligned_t = resample_targets;
            aligned_vx = vx_vect;
            aligned_vz = vz_vect;

            // Align();
            // AlignControl();
            FindMean();
            FindMeanControl();
            FindVariance();
            FindVarianceControl();
            CalculateInverse();
            CalculateInverseControl();
            CalculateDet();
            CalculateDetControl();
        }
        else
        {
            CSVParser();
            CSVParserControl();
            // RemoveDuplicates();
            Downsample();

            resample_positions = rec_positions;  
            resample_rotations = rec_rotations;
            resample_brotations = rec_brotations;
            resample_targets = targets;

            Velocity();
            Vectorize();

            aligned_x = x_vect;
            aligned_z = z_vect;
            aligned_rot = resample_rotations;
            aligned_brot = resample_brotations;
            aligned_t = resample_targets;
            aligned_vx = vx_vect;
            aligned_vz = vz_vect;

            FindMean();
            FindVariance();
            CalculateInverse();
            CalculateDet();
        }
        
        Debug.Log("Training phase is done." 
                + "\n" 
                + "=============================================");
        
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
	int N = 6;

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
        // N = A.GetLength(0);
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
        N = A.GetLength(0);
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
        N = A.GetLength(0);
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