# Anticipatory-Indicators-of-Human-Walking-Motion

## Environment
The experiment environment consists of five targets and a user. Every target has the same y of (x,y) coordinates.

## Library
The library of the trajectories is stored in Assets/Datasets/. It has 6 columns consisting of the ID, (x, y, z), yaw in degress and the target of each trial at each time step.
The datasets contain two files "train" and "train - multiple start" to indicate one or five starting point respectively.
To switch between these two you need to toggle the "random_initial_position_flag" in the User Body inspecter, enabled for the multiple starting points and disabled otherwise. 



## Prediction
The online simulation trajectory, head rotation, probability of each target and the target are stored in "recordings.csv" file in Assets/Log/. The file get clear after each restart. To start the simulation with agent, the agent mode in the User Body inspecter should be enabled. 

### General
The codes can be found in Assets/Scripts/.

