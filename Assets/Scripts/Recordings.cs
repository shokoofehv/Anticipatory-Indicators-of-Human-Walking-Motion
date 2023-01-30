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

    int path_id = 0;

    public Recordings(string data_collection)
    {   

        if(File.Exists(file_path))
            File.Delete(file_path);

        string[] headers = {"Timestamp"
                            ,"Id"
                            ,"x"
                            ,"y"
                            ,"z"
                            ,"Rotation"
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

        dataset_path += $"train - {data_collection}.csv";

        Debug.Log($"Recording to {dataset_path} ...");
    }

     
    public void SavetoCSV(List <Vector3> positions 
                        , List <float> yaws 
                        , List <List <float>> probs
                        , int target_id
                    )
    {   
        var now = DateTime.Now.ToString();

        Debug.Log("path " + path_id + " is saved.");
        string delimiter = ","; 

        for (int i = 0; i < positions.Count; i++)
        {
            float[] output = {
                            path_id,
                            positions[i].x,
                            positions[i].y,
                            positions[i].z,
                            yaws[i],
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
            File.AppendAllText(dataset_path, res + Environment.NewLine); // write to dataset file

        }

        if (path_id == 750)
            UnityEditor.EditorApplication.isPlaying = false;
        
        path_id++;
        
    }

}