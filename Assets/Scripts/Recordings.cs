using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Linq;
using System;

public class Recordings
{
    string file_path = @"Assets/Log/positions.csv";
    
    public Recordings()
    {
        if(File.Exists(file_path))
            File.Delete(file_path);
    }

    public void SavetoCSV(Vector3 new_position, Vector3 new_velocity, float yaw 
                    //, List<float> probs
                    )
    {   
        string delimiter = ","; 

        float[] output = {
            new_position.x,
            new_position.y,
            new_position.z,
            yaw
            // probs[0],
            // probs[1],
            // probs[2],
            // probs[3],
            // probs[4],
        }; 

        string res = String.Join(delimiter, output);
        
        if(!File.Exists(file_path))
            File.WriteAllText(file_path, res + Environment.NewLine); 
        else
            File.AppendAllText(file_path, res + Environment.NewLine);
    }

}