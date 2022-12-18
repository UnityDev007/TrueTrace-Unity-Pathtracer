![](/Images/Loft1.png)

Notes:</br>
Currently working on:
<ul>
  <li>Polishing it as much as I can, plan to upload to asset store</li>
  <li>Looking desperately for optimizations(let me know if you have any ideas)</li>
</ul>
Currently needs to be done but havent implemented fully:
<ul>
    <li>Volumetric Clouds(struggling, they look garbage)</li>
    <li>Volumetric Voxels(Struggling to make it fast enough, have so far tried sparse voxel octrees, DDA, and now brickmaps, but its not fast enough, any advice is welcome)</li>
</ul>
Currently want to do but havent started:
<ul>
    <li>Need More Ideas</li>
</ul>

# Compute Shader Based Unity PathTracer
A passion projects that has been going on for awhile(about a year in unity, with my earliest version I can find being version 30(whereas I am now on version 271), which was made on 5-7-2021), finally at a place where I feel comfortable tentatively uploading it to Github for others to use
What is it?
Its my attempt at a Real-Time pathtracer built from scratch in Unity using Compute Shaders
## Features: 
<ul>
<li>Somewhat fast Compute Shader based path tracing</li>
<li>Diffuse, Glossy(kinda), Diffuse Transmission, Emissive, Plastic, and Disney BSDF materials</li>
<li>Ability to move, add, and remove objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>ASVGF, SVGF, and Atrous Denoiser</li>
<li>BVH Building off of main thread for loading objects, allows objects to be spawned, and then built without lagging the main thread, and appearing when its done(All lag from spawning objects actually comes from remaking the texture atlas, lower res atlas's remove all lag, still investigating different ways of loading textures because of this)</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure (See Ylitie et al. 2017 below)</li>
<li>PBR Textures(just apply them to the GameObjects material)</li>
<li>Next Event Estimation with Multiple Importance Sampling for Explicit Light Sampling</li>
<li>Objects are loaded as gameobject meshes(most common way of having meshes in unity)</li>
<li>Support for default unity lights which interact via NEE(Supports Directional, Point, and Spot lights)</li>
<li>Support for Normal maps and Emission masks</li>
<li>Global homogenous fog with adjustable density</li>
<li>Taking Full Resolution Screenshots</li>
<li>Bloom</li>
<li>No specific GPU vendor needed(this will run on integrated graphics if you so wish it, aka no RTX cores)</li>
<li>MagicaVoxel support</li>
<li>Ability to pathtrace voxels and triangle scenes at the same time seamlessly</li>
<li>Depth of Field</li>
<li>AutoExposure</li>
<li>Temporal Anti-Aliasing</li>
<li>ReSTIR for better sampling of many lights</li>
<li>Explicit light sampling for faster convergence</li>
<li>Precomputed Multiple Atmospheric Scattering for dynamic and realtime sky(from ebruneton below)</li>
<li>Object Instancing</li>
<li>Multiple Importance Sampling for helping NEE converge much faster</li>
<li>ReSTIR GI for faster convergence in complex scenes and more complete images in scenes with changing lighting</li>
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>[ebruneton](https://ebruneton.github.io/precomputed_atmospheric_scattering/)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or something else! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use, and ways to improve this overall
## You can contact me easiest through my discord: Pjbomb2#6129


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

# Requires Unity 2021 or higher
# Instructions:
## Required Settings Changes:
<ul>
  <li>Set the Color Space to Linear through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Color Space, and change from Gamma to Linear</li>
  <li>Enable Unsafe Code(Its for memory management) through Edit -> Project Settings -> Player -> Other Settings -> "Allow 'unsafe' Code" (near the bottom)</li>
  <li>Change the Graphics Api for Windows to DirectX12 through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Untoggle "Auto Graphics API For Windows", then click the little + that appears, select "Direct3D12(Experimental)", and drag that to the top.  A restart of the editor is required</li>
</ul>
</br>
## Additional Requirements
<ul>
  <li>For Skinned Meshes, their index format needs to be set to 32 bits, and their mesh to Read/Write enabled.  This can be found by clicking on the imported fbx, going to it in the inspector, going to the Model tab, turning on Read/Write, changing the Index Format from Auto to 32 Bit, and clicking Apply at the bottom</li>
</ul>
</br>
## General Setup
<ul>
  <li>Download and import the UnityPackage provided and open the new Pathtracer Settings at the top of the screen(This WILL re-arrange your hierarchy a bit)</li>
  <li>Whenever you add a new object(or tree of objects), you need to add it to under the gameobject named Scene, and its reccomended you press quickstart to automatically assign the needed scripts to it</li>
</ul>
</br>
## Basic script structure breakdown:
<ul>
  <li>Top Level is a gameobject called Scene with an AssetManager script attatched</li>
  <li>Second Level: Parent Object Script - Attatch this to all objects that will have children with meshes you want to raytrace(can be a child to any gameobject as long as its eventual parent is the AssetManager script)</li>
  <li>Third Level: RayTracingObject Script - This defines what meshes get raytraced, must either be a direct child of a gameobject with the ParentObject Script, or in the same gameobject as the ParentObject Script</li>
  <li>Misc Level: Unity Lights - Must have a RayTracingLight script attatched to be considered(and UseNEE needs to be on), can be ANYWHERE in the hierarchy, only supports Point, Spotlight, and 1 Directional</li>
</ul>
## General Use/Notes
<ul>
  <li>The green/red rectangle shows when the acceleration structure is done building, and thus ready to render, red means that its not done, and green means its done building</li>
  <li>Objects can be added and removed at will simply by toggling the associated gameobject with a ParentObject script on/off in the hierarchy(clicking on parent objects with complex objects for children will lag), but they will take time to appear as the acceleration structure needs to  be rebuilt for them</li>
  <li>If you change the emissiveness of an object, you need to dissable and re-enable its parent(basically reloading it) if you want to take advantage of NEE correctly sampling it(Does not need to be reloaded for Naive tracing)</li>
  <li>If you use normal maps, they need to be in unity normal map format</li>
  <li>To set up PBR, all textures go into their proper names, but Roughness goes into the Occlusion texture(Since path tracing gets ambient occlusion by default, this texture is not normally needed, and there being no proper place for a Roughness texture in the default material, I have thought this was a good compromise instead of creating a custom material)</li>
  <li>If you are using blendshapes to change geometry of a skinned mesh, you may need to go to the import settings of it(in the inspector), turn off Legacy Blendshape Normals, and make sure all normals are imported, not calculated, otherwise the normals for blendshapes might be wrong</li>
</ul>
## MagicaVoxel Usage
<ul>
  <li>Before anything, the files need to be in .txt format, to do this, go to the file in file explorer, and rename the .vox to .txt and change the format</li>
  <li>Firstly, you still need to have a gameobject under the scene gameobject to attatch your voxel model to</li>
  <li>Second, you need to attatch a VoxelObject to that gameobject(Located under Assets->Resources->BVH->VoxelObject)</li>
  <li>Next you need to attatch the voxel model to this script, by dragging your voxel model asset in the project tab to the VoxelRef space in the VoxelObject script</li>
</ul>
</br>
## Using Instancing
<ul>
  <li>First, there needs to be a gameobject called InstancedStorage in the scene with the InstanceManager attatched to it as a sibling object of the Scene gameobject(this is automatically created on initial start of the scene and pathtracing settings)</li>
  <li>Second, all objects that will be the source of instanced objects will need to go under the InstancedStorage and can be arranged like normal objects(with regards to the layout of parentobject to raytracingobjects)</li>
  <li>Finally, to instance the objects, you just need gameobjects with the InstanceObject script attatched to them under the Scene gameobject, and then drag the desired object instance from the hierarchy to the Instance Parent slot in the InstanceObject script(all of this is displayed in the demoscene)</li>
</ul>

## Controls:
Camera Controls: WASD, Mouse, and press T to freeze/unfreeze the camera(Camera starts frozen), and shift increases speed
</br>
## Editor Window Guide
BVH Options Description - 
<ul>
  <li>Build Aggregated BVH(Recommended to do any time you change objects in edit mode)- Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.</li>
  <li>Clear Parent Data - Clears the data stored in parent gameobjects, allowing you to actually click them without lagging(but will then require the BVH to be rebuilt)</li>
  <li>Take Screenshot - Requires a folder called Screenshots under the asset folder, takes a screenshot</li>
  <li>QuickStart - Assigns all required scripts to all objects under the Scene gameobject, best way to add objects</li>
  <li>Make All Static - Utility button that takes all objects in the scene and puts them under one parent object, not reccomended for general use</li>
  <li>Force Instances - Looks at all meshes in the scene, sees what objects have the same meshes, and makes them into instances, keep in mind instances use the same material and textures</li>
  <li>Remaining Objects - Objects still being processed</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Render Scale - Render scale in comparison to gameview size, turn to below 1 while in edit mode to decrease rendered resolution(to then be upscaled)</li>
  <li>Atlas Size - Maximum size of the texture atlas used(All textures are packed into atlas's so I can send them to the GPU)</li>
  <li>Use Russian Roulette - Highly reccomended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays/NEE for direct light sampling</li>
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Allow Bloom - Turns on or off Bloom</li>
  <li>Enable DoF - Turns on or off Depth of Field, and its associated settings</li>
  <li>Enable Auto/Manual Exposure - Turns on or off Auto Exposure(impacts a lot more than I thought it would)</li>
  <li>Use ReSTIR - Enables the much better sampling for lots of lights</li>
  <li>Allow ReSTIR Sample Regeneration - Applies if Precomputed Sampling is on, Regenerates the light samples every frame</li>
  <li>Allow ReSTIR Precomputed Sampling - Samples lights in a more efficient way but introduces artifacts due to sample correlation</li>
  <li>Allow ReSTIR Temporal - Enables the Temporal pass of ReSTIR(allows samples to travel across time</li>
  <li>Allow ReSTIR Spatial - Enables the Spatial pass of ReSTIR(Allows pixels to choose to use the neighboring pixels sample instead)</li>
  <li>ReSTIR Spatial M-Cap - Tuneable parameter, increase this if you have lots of lights(standard values would be between 32 and 640 for reference, but going higher or lower is needed at times)</li>
  <li>Use ReSTIR GI - Enables ReSTIR GI which is usually much higher quality</li>
  <li>Do Sample Connection Validation - Confirms that two samples are mutually visable and throws it away if they are not</li>
  <li>Update Rate - How many pixels per frame get re-traced to ensure they are still valid paths</li>
  <li>Enable Temporal - Enables the Temporal pass of ReSTIR GI(allows samples to travel across time</li>
  <li>Temporal M Cap - How long a sample may live for, lower means lighting updates faster(until 0 which is the opposite) but more noise(reccomended either 0 or around 12, but can be played with)</li>
  <li>Permute Temporal Samples - Turns on permutation sampling, can lead to much higher quality much faster, but a lower M cap is reccomended(around 3-12)</li>
  <li>Enable Spatial - Enables the Spatial pass of ReSTIR GI(Allows pixels to choose to use the neighboring pixels sample instead)</li>
  <li>Spatial Sample Count - How many neighboring pixels are looked at</li>
  <li>Enable Spatial Stabalizer - Rarely useful, but turns spatial into spatiotemporal</li>
  <li>Use Temporal Antialiasing - Enables Temporal Antialiasing(TAA)</li>
  <li>Use SVGF Denoiser - Turns on the SVGF denoiser</li>
  <li>(If SVGF Denosier is on)Atrous Kernel Size - The amount of times the SVGF denoiser runs through the Atrous kernel</li>
  <li>Use ASVGF Denoiser - Turns on the ASVGF denoiser</li>
  <li>(If ASVGF Denoiser is on)ASVGF Atrous Kernel Size - The amount of iterations the final ASVGF atrous goes through, limited to 4, 5, and 6</li>
  <li>Enable Tonemapping - Turns on Uchimura Tonemapping</li>
  <li>Enable TAAU - Use TAAU for upscaling(if off, you use my semi custom upscaler instead)</li>
  <li>Atmospheric Scatter Samples - Lower this to 1 if you keep crashing on entering play mode(controls how many atmospheric samples are precomputed)</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
  </ul>
  
 ## Materials(RayTracingObject script)
 <ul>
  <li>Selected Material - Selects which material you want to edit on the mesh</li>
  <li>Material Type - Select your type of material you want(You usually want Disney, its the most versatile)</li>
  <li>Base Color - If theres no Albedo Texture, this is the color the object will be</li>
  <li>Emission - The emittance of an object(how much light it gives off)</li>
  <li>Emission Color - Changes the color of emissive objects, most useful when you have an emission mask on an object</li>
  <li>Roughness - Roughness of the object</li>
</ul>
## Disney BSDF Only Properties
<ul>
  <li>IOR - Index of Refraction of an object.  Affects only Disney BSDF</li>
  <li>Metallic - How metallic an object is.  Affects only Disney BSDF</li>
  <li>Specular - Adds specular reflection to an object, use in conjunction with Roughness and IOR.  Affects only Disney BSDF</li>
  <li>Specular Tint - Weights color more towards the objects color for specular reflections.  Affects only Disney BSDF</li>
  <li>Sheen - Adds Sheen to objects.  Affects only Disney BSDF</li>
  <li>SheenTint - Allows you to choose if an objects sheen is white or is that objects base color.  Affects only Disney BSDF</li>
  <li>ClearCoat - Adds a Clearcoat effect to the object.  Affects only Disney BSDF</li>
  <li>ClearCoatGloss - Influences the Clearcoat.  Affects only Disney BSDF</li>
  <li>Anisotropic - Makes the material(mostly metallic and specular) anisotropic.  Affects only Disney BSDF</li>
  <li>Specular Transmission - Makes an object more or less like glass.  Affects only Disney BSDF(Play with the IOR for this)</li>
  <li>Diffuse Transmission - Makes an object Diffuse but Transmissive(transluscent).  Affects only Disney BSDF</li>
  <li>Transmission Color - Doesnt do anything for now, used for volumetric disney bsdf(which is not yet implemented)</li>
  <li>Flatness - Affects Thin objects.  Affects only Disney BSDF</li>
  <li>Thin - Marks an object as thing so it can be better handled by the BSDF.  Affects only Disney BSDF, can be either 0 or 1</li>
 </ul>
</br>  
# Known Bugs:
</br>
<ul>
  <li>Error that RayTracingShader is using too many UAV's.  This isnt really a bug, but happens because you cant disable DX11 which doesnt allow more than 8(whereas DX12, which is actually used, allows a lot more), so it yells at you for a non-issue</li>
</ul>


# Sample Images(Taken from various stages of development)

![](/Images/Loft2.png)
![](/Images/Home1.png)
![](/Images/Mall1.png)
![](/Images/Mall2.png)
![](/Images/Mall3.png)
![](/Images/Loft1.png)
![](/Images/Loft2.png)
![](/Images/Furry2.png)
![](/Images/Portal2.png)
https://user-images.githubusercontent.com/31225585/194152525-e77ad1d2-546d-4a91-8069-91897b1a7130.mp4
![](/Images/NewReSTIRV2.png)
![](/Images/NewSponza3V2.png)
![](/Images/NewSponza1V2.png)
![](/Images/NewSponza2V2.png)
![](/Images/Cornell.png)
![](/Images/ReSTIR1V2.png)
![](/Images/SanMiguel1.png)
![](/Images/SanMiguel2.png)
![](/Images/MeshWithVoxels1V2.png)
![](/Images/Sponza1V2.png)
![](/Images/Voxels1V2.png)
![](/Images/BistroUpdated.png)
![](/Images/Room.png)
![](/Images/NewBlender.png)
![](/Images/Lego.png)
![](/Images/PhotoGam1V2.png)
![](/Images/PhotoGam2V2.png)
![](/Images/Bloom.png)
![](/Images/RealisticSponza.png)
![](/Images/AnotherRestir.png)
![](/Images/Blender1.png)
![](/Images/ReSTIR5.png)
![](/Images/ReSTIR2.png)
![](/Images/Restir3.png)
![](/Images/SSS1.png)
![](/Images/AutoExpose1.png)
![](/Images/DoF2.png)
![](/Images/Voxels2.png)
![](/Images/DragoonInAScene.png)
![](/Images/PBRTest1.png)
![](/Images/NewFog.png)
![](/Images/Sunset1.png)
![](/Images/VolumeScene2.png)
![](/Images/VolumeScene1.png)
![](/Images/FurryGif1.gif)
![](/Images/FurryRender.png)
![](/Images/SpaceShip.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
https://github.com/jan-van-bergen
</br></br>
Scenes From:
<ul>
  <li>https://benedikt-bitterli.me/resources/</li>
  <li>https://casual-effects.com/data/</li>
  <li>https://www.intel.com/content/www/us/en/developer/topic-technology/graphics-research/samples.html</li>
  <li>https://sketchfab.com/3d-models/tallers-de-la-fundacio-llorenc-artigas-22-5224b721b0854998a1808fcea3cff924</li>
  <li>https://sketchfab.com/3d-models/dae-bilora-bella-46-camera-game-ready-asset-eeb9d9f0627f4783b5d16a8732f0d1a4</li>
  <li>https://www.unrealengine.com/marketplace/en-US/product/victorian-train-station-and-railroad-modular-set</li>
</ul>
</br>
Disney BSDF from: https://schuttejoe.github.io/post/disneybsdf/
Rectangle packer for faster atlas creation from here: https://github.com/ThomasMiz/RectpackSharp/tree/main/RectpackSharp

This project uses:
Crytek Sponza
CC BY 3.0
© 2010 Frank Meinl, Crytek

## Ideas/Reminders for later
Reduce memory consumption, work on adding back in Mitsuba scene support
