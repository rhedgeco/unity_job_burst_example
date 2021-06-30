# Unity Job System and Burst Compiler Example
This repo is an example using Unitys Job System and Burst Compile.

The system procedurally generates a plane with tens of thousands of vertices.
Every frame it advances a noise algorithm and modifies the vertices of the mesh.
It also generates a lower resolution mesh to use as a physics collider.

Without the job system, this would run at about 5-15 fps on my machine.
With the job system, It runs at about 60 fps.
With burst compiling turned on the simulation runs at around 200+ fps even when vertex count is cranked up.

![example animation](animation.gif)