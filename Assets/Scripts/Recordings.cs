using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using System.Globalization;


public class Recordings
{

    string file_path = @"Assets/Log/recordings.csv";
    string dataset_path = @"Assets/Datasets/" ;
    string replay_path = @"Assets/Datasets/" ;

    int path_id = 0;

    public Recordings(string data_collection, string dataset, bool replay)
    {   

        if(File.Exists(file_path))
            File.Delete(file_path);

        string[] headers = {"Timestamp"
                            ,"Id"
                            ,"x"
                            ,"y"
                            ,"z"
                            ,"Rotation"
                            ,"Body Rotation"
                            ,"Target"
                            ,"Probability t0"
                            ,"Probability t1"
                            ,"Probability t2"
                            ,"Probability t3"
                            ,"Probability t4"
                            ,"Probability t5"
                            ,"Probability t6"
                            ,"Probability t7"
                            };

        string header = String.Join(",", headers);
        File.WriteAllText(file_path, header + Environment.NewLine); 

        if (data_collection == "new file")
        {
            data_collection += $" - {DateTime.Now:yyyyMMdd HHmmssfff}";
        }
        dataset_path += $"train - {data_collection}.csv";
        replay_path += $"train - {dataset}.csv";
        if (!replay)
            Debug.Log($"Recording to {dataset_path} ...");
    }

     
    public void SavetoCSV (List <string> timestamps
                        , List <Vector3> positions 
                        , List <float> yaws 
                        , List <float> body_rotations
                        , List <List <float>> probs
                        , int target_id
                        , bool replay
                    )
    {   
        var now = DateTime.Now.ToString();

        string delimiter = ","; 

        for (int i = 0; i < positions.Count; i++)
        {   
            float[] output = {
                            path_id,
                            positions[i].x,
                            positions[i].y,
                            positions[i].z,
                            yaws[i],
                            body_rotations[i],
                            target_id,
                            probs[i][0],
                            probs[i][1],
                            probs[i][2],
                            probs[i][3],
                            probs[i][4],
                            probs[i][5],
                            probs[i][6],
                            probs[i][7]
                        }; 
            
            string res = now + "," + String.Join(delimiter, output);
            File.AppendAllText(file_path, res + Environment.NewLine); // write to temporary file
            
            if (!replay)
                File.AppendAllText(dataset_path, res + Environment.NewLine); // write to dataset file

        }

        Debug.Log("path " + path_id + " is saved.");
        if (path_id == 1600)
            UnityEditor.EditorApplication.isPlaying = false;
        
        path_id++;
        
    }

    public void CSVParser (ref List<float[]> rec_positions, 
                           ref List<float> rec_rotations, 
                           ref List<float> rec_brotations,
                           ref List<float> targets
                           )
    {   
        using(var reader = new StreamReader(replay_path))
        {

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine(); 
                var values = line.Split(',');
                var id = values[1]; 
            
                float[] new_values = new float[values.Length-2]; 
                float out_val = 0.0f;
                for (int i = 2; i < values.Length; i++) 
                {
                    if (float.TryParse(values[i], out out_val)) 
                        new_values[i-2] = float.Parse(values[i]);
                }
                
                rec_positions.Add(new float[] {new_values[0], new_values[1], new_values[2]}); 
                rec_rotations.Add(new_values[3]); 
                rec_brotations.Add(new_values[4]); 
                targets.Add(new_values[5]); 
            }
        }
    }

}