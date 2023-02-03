using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

public class Manager : MonoBehaviour
{
    public bool AgentMode; 
    public bool BodyTorso;
    public float HeadRotationRate;

    public BodyController body_controller;
    // public Recordings recordings;
    public PathManager path_manager;
    // public LineRenderer line;
    // private NavMeshAgent nav_agent;


    public string data_collection;

    void Start()
    {   
        Setup();
    }

    void Update()
    {   
        TargetVisualization();      
    }   

    void TargetVisualization()
    {
        var probability = body_controller.probability;
        if (probability.Count != 0)
        {
            var targets = body_controller.targets;
            for(int i = 0; i < targets.Length; i++)
            {   
                if (probability[i] == 0)
                    targets[i].GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
                else 
                    targets[i].GetComponent<Renderer>().material.color = new Color(1.0f - Math.Abs(probability[i]), 
                                                                                   1.0f - Math.Abs(probability[i]) * 2,
                                                                                   1.0f - Math.Abs(probability[i]) * 2); 
            } 
        }
    }

    void Setup()
    {
        path_manager.addStepVariation = false; 
        

        
        if (AgentMode) 
        {
            body_controller.agent_mode = true;
            data_collection = DataCollection.SimpleAgent;
        }
        else           
        {
            body_controller.agent_mode = false;
            data_collection = DataCollection.Hand;
        }

        Debug.Log("Library method: " + data_collection);
    }
}

static class DataCollection
{
  public const string Hand = "hand";
  public const string SimpleAgent = "simple agent";
  public const string ComplexAgent = "complex agent";
}

