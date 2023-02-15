using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Linq;

public enum trainingDataset
{
    handControlled,
    simpleAgent,
    complexAgent
}

public enum recordingTo
{
    handControlled,
    simpleAgent,
    complexAgent,
    newFile
}

public class Manager : MonoBehaviour
{
    public bool AgentMode; 
    public bool BodyTorso;
    public bool Replay;
    public bool ArchTrajectory;
    // public bool HandControlledDataset;
    // public bool SimpleAgentDataset;
    // public bool ComplexAgentDataset;
    public bool ScalingProbability;
    public bool AddStepVariation;
    public bool ShowProbability;

    public trainingDataset training_dataset;
    public recordingTo recording;

    public float HeadRotationRate;

    public BodyController body_controller;
    // public Recordings recordings;
    public PathManager path_manager;
    // public LineRenderer line;
    // private NavMeshAgent nav_agent;


    public string data_collection;
    public string dataset;

    GUIStyle boxStyle;

    void Start()
    {   
        Setup();
    }

    void Update()
    {   
        TargetVisualization();      
    }   

    void OnGUI()
    {
        if (ShowProbability)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.textColor = Color.cyan;

            var probability = body_controller.probability;

            GUILayout.Box("Probabilities: ", boxStyle); //, GUILayout.Height(10)
            for (int i = 0; i < probability.Count; i++)
            {
                GUILayout.Box($"Target {i}: " + probability[i].ToString("F4"), boxStyle);
            }
        }
    }

    void TargetVisualization()
    {
        var probability = body_controller.probability;
        if (probability.Count != 0)
        {
            var targets = body_controller.targets;
            for(int i = 0; i < targets.Length; i++)
            {   
                targets[i].GetComponent<Renderer>().material.color = new Color(0.0f + Math.Abs(probability[i]) * 8, 
                                                                               1.0f - Math.Abs(probability[i]) * 9,
                                                                               0
                                                                            // Math.Abs(probability[i])
                                                                              ); 
            } 
        }

        List<float> log_p = new List<float>();  
        for (int i = 0; i < probability.Count; i++)
        {
            log_p.Add((float) Math.Abs(Math.Log10(Math.Abs(probability[i]))));
        }
        var sum_list = log_p.Sum();
        var p_normalized = log_p.Select(x => x/sum_list).ToArray();

        // if (probability.Count != 0)
        // {
        //     var targets = body_controller.targets;
        //     for(int i = 0; i < targets.Length; i++)
        //     {   
        //         Debug.Log($"p_normalized[{i}] " + p_normalized[i]);
        //         targets[i].GetComponent<Renderer>().material.color = new Color(0.8f - p_normalized[i], 
        //                                                                        0.2f + p_normalized[i],
        //                                                                        0
        //                                                                       ); 
        //     } 
        // }
    }

    void Setup()
    {
        path_manager.addStepVariation = AddStepVariation; 
        body_controller.agent_mode = AgentMode;

        
        // recording simulation management
        if (recording == recordingTo.simpleAgent) 
        {
            data_collection = DataCollection.SimpleAgent;
        }
        else if (recording == recordingTo.complexAgent) 
        {
            data_collection = DataCollection.ComplexAgent;
        }
        else if (recording == recordingTo.handControlled) 
        {
            data_collection = DataCollection.Hand;
        }
        else if (recording == recordingTo.newFile) 
        {
            data_collection = DataCollection.NewFile;
        }
        else 
            data_collection = DataCollection.NewFile;
        Debug.Log("Library method: " + data_collection);

        // training dataset management
        if (training_dataset == trainingDataset.handControlled)
            dataset = DataCollection.Hand;
        else if (training_dataset == trainingDataset.simpleAgent)
            dataset = DataCollection.SimpleAgent;
        else if (training_dataset == trainingDataset.complexAgent)
            dataset = DataCollection.ComplexAgent;
        else 
        {
            Debug.Log("Please select a dataset!");
            dataset = DataCollection.SimpleAgent;
        }
        
        
    }
}

static class DataCollection
{
  public const string Hand = "hand controlled";
  public const string SimpleAgent = "simple agent";
  public const string ComplexAgent = "complex agent";
  public const string NewFile = "new file";
}

