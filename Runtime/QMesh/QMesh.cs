using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.Mesh
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
		public UnityEngine.Mesh Mesh { get;  set; } 
		public void SetDirty()
		{
			Dirty = true;
		}
		public void FreshMesh()
		{
			if (Mesh == null) Mesh = new UnityEngine.Mesh();
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


		public static void GenerateMesh(this GameObject root, QVoxel qVoxelData,Material mat=null)
		{
			var filter = root.GetComponent<MeshFilter>(true);
			if (qVoxelData.MeshData.Mesh == null)
			{
				qVoxelData.MeshData.Mesh = filter.sharedMesh;
			}
			else if(qVoxelData.MeshData.Mesh!=filter.sharedMesh)
			{
				filter.sharedMesh = qVoxelData.MeshData.Mesh;
			}
			filter.sharedMesh = qVoxelData.GenerateMesh();
#if UNITY_EDITOR
			if (qVoxelData.MeshData.Mesh.name.IsNull())
			{
				qVoxelData.MeshData.Mesh.name = nameof(QVoxelMesh) + "_" + qVoxelData.MeshData.Mesh.GetHashCode();
				var path = "Assets/" + nameof(QVoxelMesh) + "/" + qVoxelData.MeshData.Mesh.name + ".asset";
				QFileManager.CheckDirectoryPath(path);
				UnityEditor.AssetDatabase.CreateAsset(qVoxelData.MeshData.Mesh, path);
				UnityEditor.AssetDatabase.SaveAssets();
			}
#endif
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
			filter.sharedMesh = new UnityEngine.Mesh();
			filter.sharedMesh.CombineMeshes(combineInfos.ToArray(), true, true);
			Debug.Log(root + " " + nameof(CombineMeshs) + " 顶点数:" + filter.sharedMesh.vertices.Length);
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
			var childs = root.GetComponentsInChildren<Transform>(true);
			var matList = new List<Material>();
			var combineInfos = new List<CombineInstance>();
			var bones = new List<Transform>();
			foreach (var skinedMesh in meshes)
			{
				matList.AddRange(skinedMesh.sharedMaterials);
				for (int sub = 0; sub < skinedMesh.sharedMesh.subMeshCount; sub++)
				{
					CombineInstance combine = new CombineInstance();
					combine.mesh = skinedMesh.sharedMesh;
					combine.subMeshIndex = sub;
					combineInfos.Add(combine);
				}
				foreach (var bone in skinedMesh.bones)
				{
					bones.Add(childs.Get(bone.name, (trans) => trans.name));
				}
			}
			if (root.sharedMesh == null)
			{
				root.sharedMesh = new UnityEngine.Mesh();
			}
			else
			{
				root.sharedMesh.Clear(false);
			}
			root.sharedMesh.CombineMeshes(combineInfos.ToArray(), true, false);
			root.sharedMesh.RecalculateBounds();
			root.sharedMesh.RecalculateNormals();
			root.bones = bones.ToArray();
			root.materials = matList.ToArray();
		}
	}
}
