using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool
{
	
	public class QMeshData
	{
		public QList<Vector3> vertices = new QList<Vector3>();
		public QList<Vector3> normals = new QList<Vector3>();
		public QList<Color32> colors = new QList<Color32>();
		public QList<int> triangles = new QList<int>();
		public QList<Vector2> uvs = new QList<Vector2>();
		public void AddTriangle(Vector3 a,Vector3 b,Vector3 c,Color color)
		{
			AddPoint(a, color);
			AddPoint(b, color);
			AddPoint(c, color);
		}
		public void AddPoint(Vector3 pos, Color color)
		{
			triangles.Add(vertices.Count);
			vertices.Add(pos);
			colors.Add(color);
		}
		public int Index { get;  set; }
		public void Clear()
		{
			Index = 0;
			vertices.Clear();
			normals.Clear();
			colors.Clear();
			triangles.Clear();
			uvs.Clear();
		}
		public bool Dirty { get; private set; } = true;
		public bool Changing { get; set; } = false;
		public Mesh Mesh { get;  set; } 
		public void SetDirty()
		{
			Dirty = true;
		}
		public void UpdateMesh()
		{
			if (Mesh == null) Mesh = new Mesh();
			Dirty = false;
			Mesh.Clear(false);
			Mesh.vertices = vertices.ToArray();
			Mesh.uv = uvs.ToArray();
			Mesh.colors32 = colors.ToArray();
			Mesh.triangles = triangles.ToArray();
			if (normals.Count == 0)
			{
				Mesh.RecalculateNormals();
			}
			else
			{
				Mesh.normals = normals.ToArray();
			}
		}
		public override string ToString()
		{
			return "Tris: "+triangles.Count/3+ "  Verts: " + vertices.Count;
		}
	}
	public static class QMeshTool
	{
		public static void GenerateMesh(this GameObject root, QVoxelData qVoxelData, Material mat = null)
		{
			qVoxelData.GenerateMesh();
			root.GenerateMesh(qVoxelData.MeshData.Mesh, mat);
		}
		public static void GenerateMesh(this GameObject root, QMeshData meshData, Material mat = null)
		{
			meshData.UpdateMesh();
			root.GenerateMesh(meshData.Mesh, mat);
		}
		public static void GenerateMesh(this GameObject root, Mesh Mesh,Material mat=null)
		{
			var filter = root.GetComponent<MeshFilter>(true);
			if (Mesh == null)
			{
				Mesh = filter.sharedMesh;
			}
			else if(Mesh!=filter.sharedMesh)
			{
				filter.sharedMesh = Mesh;
			}
			filter.sharedMesh = Mesh;
			if (Mesh.name.IsNull())
			{
				root.PrefabSaveAsset(Mesh);
			}
			var renderer = root.GetComponent<MeshRenderer>(true);
			if (mat != null)
			{
				renderer.material = mat;
			}
			var collider= root.GetComponent<MeshCollider>();
			if (collider != null)
			{
				collider.sharedMesh = filter.sharedMesh;

			}
		}
		public static void CombineMeshs(this MeshRenderer root, MeshRenderer[] meshes = null)
		{
			bool deleteOld = false;
			if (meshes == null)
			{
				meshes = root.GetComponentsInChildren<MeshRenderer>();
				deleteOld = true;
			}
			var matList = new List<Material>();
			var combineInfos = new List<CombineInstance>();
			foreach (var meshObj in meshes)
			{
				if (meshObj == root) continue;
				var mesh = meshObj.GetComponent<MeshFilter>()?.sharedMesh;
				matList.AddRange(meshObj.sharedMaterials);
				CombineInstance combine = new CombineInstance();
				combine.transform = Matrix4x4.TRS(meshObj.transform.localPosition, meshObj.transform.rotation, meshObj.transform.localScale);
				combine.mesh = mesh;
				combineInfos.Add(combine);
			}
			root.sharedMaterials = matList.ToArray();
			var filter = root.GetComponent<MeshFilter>(true);
			filter.sharedMesh = new Mesh();
			filter.sharedMesh.CombineMeshes(combineInfos.ToArray(), true, true);
			QDebug.Log(root + " " + nameof(CombineMeshs) + " 顶点数:" + filter.sharedMesh.vertices.Length);
			if (deleteOld)
			{
				foreach (var mesh in meshes)
				{
					if (mesh != null)
					{
						mesh.gameObject.CheckDestory();
					}
				}
			}
		}
		public static void CombineMeshs(this SkinnedMeshRenderer root, SkinnedMeshRenderer[] meshes)
		{
			var animator = root.GetComponentInParent<Animator>();
			var childs = animator.GetComponentsInChildren<Transform>(true);

			#region 合并Mesh	
			var matList = new List<Material>();
			root.sharedMesh = new Mesh();
			var combineInfos = new List<CombineInstance>();
			foreach (var skinedMesh in meshes)
			{
				matList.AddRange(skinedMesh.sharedMaterials);
				for (int sub = 0; sub < skinedMesh.sharedMesh.subMeshCount; sub++)
				{
					var combine = new CombineInstance();
					combine.mesh = skinedMesh.sharedMesh;
					combine.transform = skinedMesh.localToWorldMatrix;
					combine.subMeshIndex = sub;
					combineInfos.Add(combine);
				}
			}
			root.sharedMesh.CombineMeshes(combineInfos.ToArray(), true, true);
			root.sharedMesh.RecalculateNormals();
			root.materials = matList.ToArray();
			#endregion

			#region 构建骨骼数组
			var bones = new List<Transform>();
			var boneWeights = new List<BoneWeight>();
			var bindppses = new List<Matrix4x4>();
			foreach (var skinedMesh in meshes)
			{
				foreach (var weight in skinedMesh.sharedMesh.boneWeights)
				{
					var newWeight = weight;
					newWeight.boneIndex0 += bones.Count;
					newWeight.boneIndex1 += bones.Count;
					newWeight.boneIndex2 += bones.Count;
					newWeight.boneIndex3 += bones.Count;
					boneWeights.Add(newWeight);
				}
				foreach (var bone in skinedMesh.bones)
				{
					var newBone = childs.Get(bone.name, (trans) => trans.name);
					bones.Add(newBone);
					var bind = newBone.worldToLocalMatrix * root.localToWorldMatrix;
					//if (skinedMesh.transform.parent.parent != null)
					//{
					//	bind *= skinedMesh.transform.parent.parent.localToWorldMatrix;
					//}
					//else
					//{
					//	bind*=skinedMesh.transform.parent.localToWorldMatrix;
					//}
					bindppses.Add(bind);
				}
			}
			root.bones = bones.ToArray();
			root.sharedMesh.boneWeights = boneWeights.ToArray();
			root.sharedMesh.bindposes = bindppses.ToArray();
			#endregion
			root.sharedMesh.RecalculateBounds();
			//root.localBounds = default;

		}
	}
}
