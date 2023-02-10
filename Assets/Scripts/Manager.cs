using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

public class Manager : MonoBehaviour
{
    public bool AgentMode; 
    public bool BodyTorso;
    public bool Replay;
    public bool ArchTrajectory;
    public bool HandControlledDataset;
    public bool SimpleAgentDataset;
    public bool ComplexAgentDataset;
    public float HeadRotationRate;

    public BodyController body_controller;
    // public Recordings recordings;
    public PathManager path_manager;
    // public LineRenderer line;
    // private NavMeshAgent nav_agent;


    public string data_collection;
    public string dataset;

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
                targets[i].GetComponent<Renderer>().material.color = new Color(0.0f + Math.Abs(probability[i]) * 5, 
                                                                               1.0f - Math.Abs(probability[i]) * 3,
                                                                         //    1.0f - Math.Abs(probability[i]) * 2
                                                                               0
                                                                              ); 
            } 
        }
    }

    void Setup()
    {
        path_manager.addStepVariation = false; 
        

        
        if (AgentMode && !ArchTrajectory) 
        {
            body_controller.agent_mode = true;
            data_collection = DataCollection.SimpleAgent;
        }
        else if (AgentMode && ArchTrajectory) 
        {
            body_controller.agent_mode = true;
            data_collection = DataCollection.ComplexAgent;
        }
        else           
        {
            body_controller.agent_mode = false;
            data_collection = DataCollection.Hand;
        }

        if (HandControlledDataset)
            dataset = DataCollection.Hand;
        else if (SimpleAgentDataset)
            dataset = DataCollection.SimpleAgent;
        else if (ComplexAgentDataset)
            dataset = DataCollection.ComplexAgent;

        Debug.Log("Library method: " + data_collection);
    }
}

static class DataCollection
{
  public const string Hand = "hand controlled";
  public const string SimpleAgent = "simple agent";
  public const string ComplexAgent = "complex agent";
}

