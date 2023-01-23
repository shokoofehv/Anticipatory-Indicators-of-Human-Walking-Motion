using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

public class Manager : MonoBehaviour
{
    public BodyController body_controller;
    public Recordings recordings;
    public PathManager path_manager;
    public LineRenderer line;
    private NavMeshAgent nav_agent;


    public string data_collection;

    void Setup()
    {
        path_manager.addStepVariation = false; 
        body_controller.agent_mode = true;

        
        if (body_controller.agent_mode)
            data_collection = DataCollection.SimpleAgent;
        else           
            data_collection = DataCollection.Hand;

        Debug.Log("data collection org " + DataCollection.SimpleAgent);
    }

    // Start is called before the first frame update
    void Start()
    {   
        Setup();
        
    }

    // Update is called once per frame
    void Update()
    {   

        TargetVisualization();      
    }   

    // void PathVisualization()
    // {
    //     var path = nav_agent.path.corners;
    //     line.positionCount = path.Length;
    //     line.SetPositions(path);
    // }

    void TargetVisualization()
    {
        var probability = body_controller.probability;
        if (probability.Count != 0)
        {
            var targets = body_controller.targets;
            for(int i = 0; i < targets.Length; i++)
            {   
                // Debug.Log($"in {i} with pro " + probability[i]);
                targets[i].GetComponent<Renderer>().material.color = new Color(0.5f + Math.Abs(probability[i]), 
                                                                               0.0f + Math.Abs(probability[i]) * 2,
                                                                               0.0f); //- Math.Abs(probability[i])
            } 
        }
    }
}

static class DataCollection
{
  public const string Hand = "hand";
  public const string SimpleAgent = "simple agent";
  public const string ComplexAgent = "complex agent";
}

