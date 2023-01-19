using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;
using UnityEngine.SceneManagement;


public class Recordings
{
    string file_path = @"Assets/Log/recordings.csv";

    int path_id = 0;

    public Recordings()
    {
        if(File.Exists(file_path))
            File.Delete(file_path);

        string[] headers = {"id"
                            ,"x"
                            ,"y"
                            ,"z"
                            ,"yaws"
                            ,"probability t0"
                            ,"probability t1"
                            ,"probability t2"
                            ,"probability t3"
                            ,"probability t4"
                            ,"target"
                            };
        string header = String.Join(",", headers);
        File.WriteAllText(file_path, header + Environment.NewLine); 

    }

    bool ResetHappend() 
    {
        if (Input.GetKeyDown(KeyCode.R)) 
        {   
            Debug.Log("New path started.");
            return true;
        }
        else 
            return false;
    }

    public void SavetoCSV(List <Vector3> positions 
                        , List <float> yaws 
                        , List <List <float>> probs
                        , int target_id
                    )
    {   
        string delimiter = ","; 

        for (int i = 0; i < positions.Count; i++)
        {
            float[] output = {
                            path_id,
                            positions[i].x,
                            positions[i].y,
                            positions[i].z,
                            yaws[i],
                            probs[i][0],
                            probs[i][1],
                            probs[i][2],
                            probs[i][3],
                            probs[i][4],
                            target_id
                        }; 

            string res = String.Join(delimiter, output);
            File.AppendAllText(file_path, res + Environment.NewLine);
        }
        path_id++;
        // if(!File.Exists(file_path))
        //     File.WriteAllText(file_path, res + Environment.NewLine); 
        // else
        //     File.AppendAllText(file_path, res + Environment.NewLine);
        
    }

}