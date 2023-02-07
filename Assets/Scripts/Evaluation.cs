using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Evaluation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
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
        for (int i = 0; i < positions.Count; i++)
        {
            // Debug.Log("correct " + correct);
            var probabilities = probs[i];
            int max_id = probabilities.IndexOf(probabilities.Max());
            if (max_id == target_id)
                correct++;
        }
        float accu = correct / (float)positions.Count;
        Debug.Log($"correct: {correct} total: {positions.Count} " + "Accuracy: " + accu); 

    }
}
