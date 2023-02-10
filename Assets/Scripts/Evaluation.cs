using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

public class Evaluation : MonoBehaviour
{
    string path = @"Assets/Log/evaluation.csv";
    // Start is called before the first frame update
    void Start()
    {
        if(File.Exists(path))
            File.Delete(path);

        string[] headers = { "correct",
                             "total",
                             "accuracy",
                             "target",
                             "timestep ahead ratio",
                            //  "mean position",
                            //  "variance position",
                             "avg head rotation",
                             "std head rotation",
                             "avg body rotation",
                             "std body rotation"
                            };
        string header = String.Join(",", headers);
        File.WriteAllText(path, header + Environment.NewLine); 

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Eval(Vector3 new_position, float yaw, float body_rotation, List <float> probability, int target)
    {   
        int max_id = probability.IndexOf(probability.Max());
        // if (max_id == target)
            // Debug.Log("Yaaaay");
    }

    public void GetResults(List <Vector3> positions 
                    , List <float> yaws 
                    , List <float> body_rotations
                    , List <List <float>> probs
                    , int target_id )
    {
        int correct = 0;
        int furthest_time = -1;
        for (int i = 0; i < positions.Count; i++)
        {
            // Debug.Log("correct " + correct);
            var probabilities = probs[i];
            int max_id = probabilities.IndexOf(probabilities.Max());
            if (max_id == target_id)
            {   
                if (furthest_time == -1)
                    furthest_time = i;
                correct++;
            }
            else
                furthest_time = -1;
        }
        string accu = (correct / (float)positions.Count).ToString("F3");
        string timestep_ahead = (furthest_time != -1) ? 
                               ((positions.Count - furthest_time) / (float) positions.Count).ToString("F3") : 
                                "0";
        var total = positions.Count;
        Debug.Log($"correct: {correct} total: {total} accuracy: {accu} with {timestep_ahead} timestep ratio ahead.");
        
        double head_rot_avg = yaws.Average();
        double head_rot_std = yaws.Select(val => (val - head_rot_avg) * (val - head_rot_avg)).Sum();
        head_rot_std = Math.Sqrt(head_rot_std / yaws.Count); 

        double body_rot_avg = body_rotations.Average();
        double body_rot_std = body_rotations.Select(val => (val - body_rot_avg) * (val - body_rot_avg)).Sum();
        body_rot_std = Math.Sqrt(body_rot_std / body_rotations.Count); 

        string res = "";
        res += correct + "," + 
               total + "," + 
               accu + "," + 
               target_id  + "," + 
               timestep_ahead + "," +
               head_rot_avg + "," +
               head_rot_std + "," +
               body_rot_avg + "," +
               body_rot_std + "," ;
        res += "\n";
        File.AppendAllText(path, res);
    
    }
}
