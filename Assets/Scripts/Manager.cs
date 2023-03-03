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
    complexAgent,
    mixAgent
}

public enum recordingTo
{
    handControlled,
    simpleAgent,
    complexAgent,
    mixAgent,
    newFile
}

public class Manager : MonoBehaviour
{
    public bool AgentMode; 
    public bool BodyTorso;
    public bool Replay;
    public bool ArchTrajectory;
    public bool AddStepVariation;

    public bool ScalingProbability;
    public bool RandomHeadRotation;
    public bool MouseControl;
    public bool ShowProbability;

    public trainingDataset trainingDataset;
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
                GUILayout.Box($"Target {i}: " + probability[i].ToString(), boxStyle);
            }
        }
    }

    void TargetVisualization()
    {
        var probability = body_controller.probability;

        if (probability.Count != 0)
        {
            var targets = body_controller.targets;
            
            // sorting the probabilities while getting the index in the original list 
            var sorted = probability
                .Select((x, i) => new KeyValuePair<float, int>(x, i))
                .OrderBy(x => x.Key)
                .ToList();
            List <int> idx = sorted.Select(x => x.Value).ToList();

            for(int i = 0; i < targets.Length; i++)
            {   
                // find where the current target is in the sorted probability list
                int id = idx.IndexOf(i);
                
                // define the color of the targets based on their positions in the sorted list
                // red to green from the lowest to the highest
                float r = (targets.Length - id - 1) / ((float) targets.Length);
                float g = (id + 1) / ((float) targets.Length);

                targets[i].GetComponent<Renderer>().material.color = new Color(r, g, 0.0f);
            } 
        }
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
        else if (recording == recordingTo.mixAgent) 
        {
            data_collection = DataCollection.MixAgent;
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
        if (trainingDataset == trainingDataset.handControlled)
            dataset = DataCollection.Hand;
        else if (trainingDataset == trainingDataset.simpleAgent)
            dataset = DataCollection.SimpleAgent;
        else if (trainingDataset == trainingDataset.complexAgent)
            dataset = DataCollection.ComplexAgent;
        else if (trainingDataset == trainingDataset.mixAgent)
            dataset = DataCollection.MixAgent;
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
  public const string MixAgent = "mix agent";
  public const string NewFile = "new file";
}

