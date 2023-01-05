# Anticipatory-Indicators-of-Human-Walking-Motion

## Environment
The experiment environment consists of five targets and a user. The y of (x,y) coordinates of each target is similar. 

## Library
The library of the trajectories are stored in Assets/Datasets/. it has 6 columns consisting of the ID, (x, y, z), yaw and the target of each trial at each time-step.
The velocities are calculated from 4th order of 1st derivative of the position. To make all the trials the same length, Dynamic Time Warping is applied to each trial. 
A multivariate Gaussian is used reaulting in a mean and variance for each time step of each target.

## Prediction
The online simulation trajectory, velocity and the yaw rotation are stored .csv file in Assets/Log/.
By comparing the current trajectory of the user to library at the previouse section, the target with the highest probability is returned.

### General
The code can be find in Assets/Scripts/BodyController.cs.

