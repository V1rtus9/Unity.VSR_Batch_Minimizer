using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Virtustellar
{
	/**
	 * Name: VSR_Batch_Minimizer
	 * Purpose: Batches minimization ( I'm using this scripts in own project )
	 * Unity: Tried with 2018.2, 2017.4,2017.3, 2017.2. 2017.1
	 * Author: V!rtus9
	 * License: FREE
	 * */

	public class VSR_Batch_Minimizer : MonoBehaviour
	{
		public int totalMeshCount;
		public int totalVertexCount;

		public enum ColliderType
		{
			None,
			Mesh,
			Sphere,
			Box,
			Capsule,
		}

		public enum SaveAction
		{
			None,
			Mesh,
			Prefab
		}

		//
		public ColliderType addComponentCollider = ColliderType.None;
		// If true, new proceduraly created gameobject will be with rigidbody // kinematic by default
		public bool addComponentRigidbody;
		//By default nothing to do, choose if would like to save proceduraly generated mesh or gameobject
		public SaveAction saveAction = SaveAction.None;
		public Dictionary<ColliderType, System.Type> colliderStore = new Dictionary<ColliderType, System.Type>()
			{
				{ColliderType.Mesh, typeof(MeshCollider)},
				{ColliderType.Sphere, typeof(SphereCollider)},
				{ColliderType.Box, typeof(BoxCollider)},
				{ColliderType.Capsule, typeof(CapsuleCollider)}

			};
		// Use this for initialization
		void Start()
		{
			CombineMeshes(this.gameObject);
			AddComponentCollider(this.gameObject);
			AddComponentRigidbody(this.gameObject);
			DeactivateChildrens(this.gameObject);

		}

		private void OnApplicationQuit()
		{
			SaveOptions(this.gameObject);
		}

		private void AddComponentCollider(GameObject procedural)
		{
			if (addComponentCollider == ColliderType.None)
				return;

			procedural.AddComponent(colliderStore[addComponentCollider]);
		}

		private void AddComponentRigidbody(GameObject procedural)
		{
			if (!addComponentRigidbody)
				return;

			procedural.AddComponent<Rigidbody>().isKinematic = true;
		}

		public void DeactivateChildrens(GameObject parent)
		{
			for (int i = 0; i < parent.transform.childCount; i++)
			{
				parent.transform.GetChild(i).gameObject.SetActive(false);
			}
		}

		private bool isSaved;
		private void SaveOptions(GameObject procedural, string name = "0123456789")
		{
			if (isSaved)
				return;

			isSaved = true;

			Mesh msh = procedural.GetComponent<MeshFilter>().mesh;

			switch (saveAction)
			{
				case SaveAction.Mesh:
					SaveAsset(msh, name);
					break;

				case SaveAction.Prefab:
					SaveAsset(msh, name);

					procedural.transform.DetachChildren();
					DestroyImmediate(procedural.GetComponent<VSR_Batch_Minimizer>());

					PrefabUtility.ReplacePrefab(procedural, PrefabUtility.CreateEmptyPrefab("Assets/" + name  + ".prefab"), ReplacePrefabOptions.ConnectToPrefab);
					break;
			}

		}

		private void SaveAsset(Object obj, string name = "0123456789")
		{
			AssetDatabase.CreateAsset(obj, "Assets/" + name + ".asset");
			AssetDatabase.SaveAssets();
		}

		private void CombineMeshes(GameObject parent)
		{

			MeshRenderer[] meshRenderers = parent.GetComponentsInChildren<MeshRenderer>(false);

			if (meshRenderers != null && meshRenderers.Length > 0)
			{
				foreach (MeshRenderer mshRndr in meshRenderers)
				{
					MeshFilter filter = mshRndr.GetComponent<MeshFilter>();

					if (filter != null && filter.sharedMesh != null)
					{
						totalVertexCount += filter.sharedMesh.vertexCount;
						totalMeshCount++;
					}
				}
			}

			if (totalMeshCount == 0)
			{
				Debug.Log("The combine is impossible, no meshes to combine. Parent gameObject name  is: " + this.name);
				return;
			}
			if (totalMeshCount == 1)
			{
				Debug.Log("The combine is impossible, not enought meshes (minimum 2). Parent gameObject name  is: " + this.name);
				return;
			}
			if (totalVertexCount > 65535)
			{
				Debug.Log("There are too many vertices to combine into 1 mesh (" + totalVertexCount + "). The maximum limit is ~65K. Parent gameObject name is: " + this.name);
				return;
			}

			Mesh newMesh = new Mesh();
			Matrix4x4 myTransform = parent.transform.worldToLocalMatrix;

			//Lists to store vertices, normals, uvs and submeshes
			List<Vector2> uv1s = new List<Vector2>();
			List<Vector2> uv2s = new List<Vector2>();

			List<Vector3> normals  = new List<Vector3>();
			List<Vector3> vertices = new List<Vector3>();

			//Dictionary where key is material an value containts list with all submeshes that uses this material
			Dictionary<Material, List<int>> subMeshes = new Dictionary<Material, List<int>>();

			if (meshRenderers != null && meshRenderers.Length > 0)
			{
				foreach (MeshRenderer mshRndr in meshRenderers)
				{
					MeshFilter filter = mshRndr.GetComponent<MeshFilter>();
					if (filter != null && filter.sharedMesh != null)
					{
						GenerateProceduralMesh(filter.sharedMesh, mshRndr.sharedMaterials, myTransform * filter.transform.localToWorldMatrix, vertices, normals, uv1s, uv2s, subMeshes);
					}
				}
			}

			if(vertices.Count > 0)
				newMesh.vertices = vertices.ToArray();

			if (normals.Count > 0)
				newMesh.normals = normals.ToArray();

			if (uv1s.Count > 0)
				newMesh.uv = uv1s.ToArray();

			if (uv2s.Count > 0)
				newMesh.uv2 = uv2s.ToArray();

			newMesh.subMeshCount = subMeshes.Keys.Count;
			Material[] materials = new Material[subMeshes.Keys.Count];

			int index = 0;
			foreach (Material m in subMeshes.Keys)
			{
				materials[index] = m;
				newMesh.SetTriangles(subMeshes[m].ToArray(), index++);
			}

			if (meshRenderers != null && meshRenderers.Length > 0)
			{
				MeshRenderer mshRndr = parent.GetComponent<MeshRenderer>();

				if (mshRndr == null)
					mshRndr = parent.AddComponent<MeshRenderer>();

				mshRndr.sharedMaterials = materials;

				MeshFilter meshFilter = parent.GetComponent<MeshFilter>();

				if (meshFilter == null)
					meshFilter = parent.AddComponent<MeshFilter>();

				meshFilter.sharedMesh = newMesh;
			}

		}

		private void GenerateProceduralMesh(Mesh meshToMerge, Material[] ms, Matrix4x4 transformMatrix, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv1s, List<Vector2> uv2s, Dictionary<Material, List<int>> subMeshes)
		{
			if (meshToMerge == null)
				return;

			int vertexOffset = vertices.Count;

			try
			{

				Vector3[] vrts = meshToMerge.vertices;

				for (int i = 0; i < vrts.Length; i++)
				{
					vrts[i] = transformMatrix.MultiplyPoint3x4(vrts[i]);
				}

				vertices.AddRange(vrts);

			}
			catch (System.Exception ex)
			{
				Debug.Log("GenerateProceduralMesh method error (252):" + ex);
			}


			Quaternion rotation = Quaternion.LookRotation(transformMatrix.GetColumn(2), transformMatrix.GetColumn(1));

			Vector3[] nrmls = meshToMerge.normals;

			if (nrmls != null && nrmls.Length > 0)
			{
				for (int i = 0; i < nrmls.Length; i++) nrmls[i] = rotation * nrmls[i];

				normals.AddRange(nrmls);
			}

			Vector2[] uvs = meshToMerge.uv;

			if (uvs != null && uvs.Length > 0)
				uv1s.AddRange(uvs);

			uvs = meshToMerge.uv2;

			if (uvs != null && uvs.Length > 0)
				uv2s.AddRange(uvs);

			for (int i = 0; i < ms.Length; i++)
			{
				if (i < meshToMerge.subMeshCount)
				{
					int[] ts = meshToMerge.GetTriangles(i);

					if (ts.Length > 0)
					{
						if (ms[i] != null && !subMeshes.ContainsKey(ms[i]))
						{
							subMeshes.Add(ms[i], new List<int>());
						}
						List<int> subMesh = subMeshes[ms[i]];
						for (int t = 0; t < ts.Length; t++)
						{
							ts[t] += vertexOffset;
						}

						subMesh.AddRange(ts);
					}
				}
			}
		}
	}
}
