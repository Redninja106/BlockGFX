# BlockGFX

experiments with voxel worlds & lighting techniques using C#, GLFW & Direct3D 11. Each branch is a different experiment.

# Master

The master branch is a basic unlit voxel world. Includes a basic first person character controller and block breaking/placing. It acted as a nice starting point for adding lighting.

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/f1024264-98ff-4b98-91f9-b29c60588b88)

# Deferred

The first thing I did was switch to a deferred renderer. I didn't end up doing much with this as I had more interesting ideas (and shadows would have required shadow mapping).

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/51989ce4-5ffa-459c-94d3-af016b72e011)

# Raycast-lighting

After some messing around I had the idea to compute lighting in world space. The idea was to use path tracing to compute lighting in world space, a sort of realtime baking approach. 

I decided to compute the lighting at the same scale as the individual pixels on the world textures, so in increments 1/16th the size of a voxel. So when building the mesh for a block chunk (a 16x16x16 region of blocks), I track every face added, assigning an ID (just an int starting from 0). Each vertex in the mesh has the ID of the face it belongs to as an attribute. Afterward, I create a texture that can fit the lighting data for that many faces (16*faces x 16). This texture stores the lighting information for the chunk. Next, I upload the face position, orientation (in the form of up and right vectors), and texture atlas coordinates to a buffer. This buffer can be indexed by the face ID.

Now for the path tracing. I implemented a basic voxel path tracer, based off of [this paper](http://www.cs.yorku.ca/~amana/research/grid.pdf) (as well as ray-box intersection to cull entire chunks). I run a compute shader on each chunk's face texture (16x16 threadgroups with face # of groups) and sample rays in random directions, which are then averaged and saved.

Next, the chunk's mesh is actually rendered using its face texture. The result is a (albiet noisy and slow) path traced voxel chunk!

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/9fad9167-fa3b-47a1-99d9-857a9bc973ac)

It's noisy (even at 1000 samples) and very slow. It has to compute 1000 samples for every pixel on every face of every visible block. That's alot.

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/307156a5-9801-463d-a81c-b701c3a19681)

So I started brainstorming how to optimize the path tracer to be usable in real time (>30fps). I eventually realized I'm doing 1000 samples on every pixel in the scene, even for the ones behind camera. 
What if there was a way to skip these samples, as they'll have little to no effect on the final image? After some more thinking I came up with the idea of using the alpha channel of the face texture to store visibility information (ie 1=visible, 0=not visible). 
Also, When rendering the chunk's mesh, I can determine each fragment's corresponding pixel in the face texture. Next came the critical realization: if i clear the Alpha channel of the face buffer, then render the mesh, I can just write a 1 to the face texture from the fragment shader. 
This way, visible pixels on the face texture have an alpha of 1, and occluded ones have an alpha of 0! 

Once the conceptual stuff was worked out, the implementation came quick. There were some problems, for example I had to render the mesh (to the depth buffer only) an additonal time before everything else to prevent overdraw (to prevent marking pixels visible that will be occluded by later meshes).
But the final product ran at ~50FPS!

There's so much more that could be done here. The most obvious of which is probably accumulating lighting over multiple frames (especially since it's stored in world space -- so it wouldn't have to reaccumulate every time the camera moved).
But I decided to go in a different direction

# Partial-Raycast-Lighting

