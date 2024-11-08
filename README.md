# BoidzDots

Testing the Boids algorithm with Unity DOTS for efficient handling of large swarms.

## Overview
This project explores the implementation of the Boids algorithm within Unity's Data-Oriented Technology Stack (DOTS). It aims to assess the performance of DOTS for simulating large numbers of boids with basic flocking behaviors, targeting a minimum of 60 FPS.

## Setup and Testing
1. **Boid Subscene**: Adjust the spawn amount in the BoidSpawner to control the number of boids.
2. **Testing Environment**: Testing <u>MUST</u> be done with the Scene view open. Use the provided `ECSLayout` or your own custom layout for optimal workflow.
   - can be found in Assets/_Layout
4. **Performance Goal**: Maintain performance above 60 FPS during testing.

## Branches of Interest
### `SimpleMovingWithDots`
This branch focuses on basic movement with DOTS, testing how many entities your PC can handle without flocking behaviors.

### `BoidsWithDots`
A prototype branch for implementing simple boid behaviors with DOTS. It is a minimal and experimental setup to test how many boids can be handled with flocking behavior. Adjust the Boid settings as needed to experiment with different flocking configurations.

---

Adjust the settings in the BoidSpawner and Boid settings to test various configurations and observe how the system handles different levels of complexity and load.
