# BlockGFX

experiments with voxel worlds & lighting techniques using C#, GLFW & Direct3D 11. Each branch is a different experiment.

# Master

The master branch is a basic unlit voxel world. Includes a basic first person character controller and block breaking/placing. It acted as a nice starting point for other experiments.

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

It's noisy (even at 1000 samples) and very slow (about 20 fps). It has to compute 1000 samples for every pixel on every face of every visible block. That's alot.

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/307156a5-9801-463d-a81c-b701c3a19681)

So I started brainstorming how to optimize the path tracer to be usable in real time (>30fps). I eventually realized I'm doing 1000 samples on every pixel in the scene, even for the ones behind camera. 

These samples have little to no effect on the final image, so what if there was a way to skip them? This led to the idea of using the face texture's alpha channel as a visibily "flag" (ie 1=visible, 0=not visible). If I render the chunk and just set the alpha to 1 from the fragment shader, only the visible pixels will be set!  
Then I can just skip sampling rays if the alpha is 0!

Once the conceptual stuff was worked out, the implementation came quick. There were some problems, for example I had to render the mesh (to the depth buffer only) an additonal time before everything else to prevent overdraw (to prevent marking pixels visible that will be occluded by later meshes).
But the final product ran at ~50FPS! That's over twice as fast as before!

The next optimization I did was accumulating lighting on the face texture. This was *very* easy to implement (just the weighted average between this frame's color and the last frame's color) and had astounding results. Since all the lighting was being calculated in world space, It acculated regardless of camera movement.  Any noise vanished after only a few frames, even with the sample count reduced by a factor of 5 (down to 200 samples). Now I was getting ~200 FPS!

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/ed73971b-1d85-46db-8aff-0b0d7285a82e)

There's so much more that could be done here:
- Ray bounces could be skipped entirely if rays use the accumulated light in the face texture.
- The face texture needs a different layout, since the current one fails if there's more than 1024 faces in a chunk (1024*16 = 16384, which is the max texture width in d3d11).
- Other random crashes and problems

But I decided to go in a different direction:

# Partial-Raycast-Lighting

I wanted to see what I could do with raytracing using a more conventional rendering approach. So I made a new branch went back to simple rasterization. The difference now is that I can sample rays whenever I want.  

The first thing I do is use a raycast per pixel to add hard shadows. This is really simple to implement (just shoot a ray in the direction of the sun, if it hits anything the pixel is in shadow):

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/e3281e18-3390-472f-8ab3-2c197fb7c0db)

Afterwards I started to run out of ideas for things to do with this approach. I couldn't come up with anything that looked better than the full path traced version.

So I decided to try to add back multiple chunks (i had been doing everything ray tracing related to on one chunk at this point). 

This came with its own set of challenges: now all the code had to account for stuff being across chunk borders. I commented out enough lighting features such that the only resource that needed to be accessed across chunks was the actual voxel data, which is uploaded to the GPU as a 3D texture. Using multiple textures was impractical, since I wanted to be able to render hundreds of chunks, and you can't bind that many textures at once.

So it seemed there is only option: to keep a really big 3D texture, and copy chunks into sections of it as needed. This probably would've worked but as I researching my options I stumble upon a lesser-used Direct3D 11 feature: tiled resources. Tiled resources are not a complicated feature: just create a huge resource and set a flag indicating it's a tiled resource. The resource isn't mapped to physical memory (thus not taking up space) and you can make api calls to map chunks of it (usually 64kb) to a buffer. I thought this would be an interesting approach so started messing around with the API until I felt I understood things. Then I got started on actually making it work. I allocate an *outrageously* huge 3D texture (2048^3 float4, that's 64 terabytes), and a more reasonably sized buffer to map the resource to. Some modifications to the shader code to account for the chunk not starting at the origin and voila! 

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/3421f486-2db3-4bdf-9532-c50d055b69e6)

With this approach, recentering the player just is just updating some tile mappings. Without it, I'd have to move hundreds of 16kb chunks around in a texture (and reupload the whole thing each time).

My last experiment was sampling rays to make reflections. Another simple one, I all do is reflect the direction from the camera to the pixel pos across the face's normal and sample a ray!

![image](https://github.com/Redninja106/BlockGFX/assets/45476006/0b65926c-c6d3-4858-8ada-a6e5b16649e4)
