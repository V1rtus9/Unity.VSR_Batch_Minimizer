# VSR_Batch_Minimizer

There are numerous methods to minimize batches in Unity
  1. Unity own draw call batching  - https://docs.unity3d.com/Manual/DrawCallBatching.html
  2. GPU Instancing  - https://docs.unity3d.com/Manual/GPUInstancing.html
  3. Combine meshes by yourself
  4. GPU skinning (skinned mesh optimization)

VSR_Batch_Minimizer allows you to combine several meshes (with different or multiple materials) in one mesh and save as mesh asset or prefab.

Example - 
before: 38 batches, 
after: 9 batches 
  
  Asset - https://assetstore.unity.com/packages/3d/environments/historic/medieval-defense-low-poly-all-maps-27667
  
 
