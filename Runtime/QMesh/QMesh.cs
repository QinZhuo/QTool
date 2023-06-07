using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	public class QMeshData
	{
		public List<Vector3> vertices = new List<Vector3>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Color32> colors = new List<Color32>();
		public List<int> triangles = new List<int>();
		public List<Vector2> uvs = new List<Vector2>();
		public List<Vector4> tangents = new List<Vector4>();
		public List<BoneWeight> boneWeights = new List<BoneWeight>();
		public List<Matrix4x4> bindposes = new List<Matrix4x4>();
		public int Index { get; set; }
		public bool Dirty { get; private set; } = true;
		public bool Changing { get; set; } = false;
		public Mesh Mesh { get; set; }
		public QMeshData() { }
		public void Add(Mesh mesh)
		{
			for (int i = 0; i < mesh.vertexCount; i++)
			{
				vertices.Add(mesh.vertices[i]);
				uvs.Add(mesh.uv[i]);
				normals.Add(mesh.normals[i]);
				tangents.Add(mesh.tangents[i]);
			}
			int length = triangles.Count;
			for (int i = 0; i < mesh.triangles.Length; i++)
			{
				triangles.Add(mesh.triangles[i] + length);
			}
		}
		public void AddPoint(Mesh mesh,int index)
		{
			Add(mesh.vertices[index], mesh.uv[index], mesh.normals[index], mesh.tangents[index], mesh.boneWeights.Get(index));
		}
		public void Add(Vector3 vert, Vector2 uv, Vector3 normal, Vector4 tangent, BoneWeight boneWeight)
		{
			vertices.Add(vert);
			uvs.Add(uv);
			normals.Add(normal);
			tangents.Add(tangent);
			boneWeights.Add(boneWeight);
		}
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
		public void Clear()
		{
			Index = 0;
			vertices.Clear();
			normals.Clear();
			colors.Clear();
			triangles.Clear();
			uvs.Clear();
			boneWeights.Clear();
			bindposes.Clear();
		}
	
		public void SetDirty()
		{
			Dirty = true;
		}
		public Mesh GetMesh()
		{
			if (Mesh == null) Mesh = new Mesh();
			Dirty = false;
			Mesh.Clear(false);
			Mesh.vertices = vertices.ToArray();
			Mesh.uv = uvs.ToArray();
			Mesh.colors32 = colors.ToArray();
			Mesh.triangles = triangles.ToArray();
			Mesh.boneWeights = boneWeights.ToArray();
			Mesh.bindposes = bindposes.ToArray();
			if (normals.Count == 0)
			{
				Mesh.RecalculateNormals();
			}
			else
			{
				Mesh.normals = normals.ToArray();
			}
			if (tangents.Count == 0)
			{
				Mesh.RecalculateTangents();
			}
			else
			{
				Mesh.tangents = tangents.ToArray();
			}
			return Mesh;
		}
		public void CombineVertices(float range)
		{
			range *= range;
			for (int i = 0; i < vertices.Count; i++)
			{
				for (int j = i + 1; j < vertices.Count; j++)
				{
					bool dis = (vertices[i] - vertices[j]).sqrMagnitude < range;
					bool uv = (uvs[i] - uvs[j]).sqrMagnitude < range;
					bool dir = Vector3.Dot(normals[i], normals[j]) > 0.999f;
					if (dis && uv && dir)
					{
						for (int k = 0; k < triangles.Count; k++)
						{
							if (triangles[k] == j)
								triangles[k] = i;
							if (triangles[k] > j)
								triangles[k]--;
						}
						vertices.RemoveAt(j);
						normals.RemoveAt(j);
						tangents.RemoveAt(j);
						boneWeights.RemoveAt(j);
						uvs.RemoveAt(j);
					}
				}
			}
		}
		public void Reverse()
		{
			int count = triangles.Count / 3;
			for (int i = 0; i < count; i++)
			{
				int t = triangles[i * 3 + 2];
				triangles[i * 3 + 2] = triangles[i * 3 + 1];
				triangles[i * 3 + 1] = t;
			}
			count = vertices.Count;
			for (int i = 0; i < count; i++)
			{
				normals[i] *= -1;
				Vector4 tan = tangents[i];
				tan.w = -1;
				tangents[i] = tan;
			}
		}
		public override string ToString()
		{
			return "Tris: "+triangles.Count/3+ "  Verts: " + vertices.Count;
		}
	}
	public static class QMeshTool
	{
		public static Mesh GetMesh(this PrimitiveType primitiveType)
		{
			var obj = GameObject.CreatePrimitive(primitiveType);
			var mesh = obj.GetComponent<MeshFilter>().sharedMesh;
			obj.CheckDestory();
			return mesh;
		}
		public static Vector4 CalculateTangent(this Vector3 normal)
		{
			Vector3 tan = Vector3.Cross(normal, Vector3.up);
			if (tan == Vector3.zero)
				tan = Vector3.Cross(normal, Vector3.forward);
			tan = Vector3.Cross(tan, normal);
			return new Vector4(tan.x, tan.y, tan.z, 1.0f);
		}
		public static void GenerateMesh(this GameObject root, QVoxelData qVoxelData, Material mat = null)
		{
			qVoxelData.GenerateMesh();
			root.GenerateMesh(qVoxelData.MeshData.Mesh, mat);
		}
		public static void GenerateMesh(this GameObject root, QMeshData meshData, Material mat = null)
		{
			meshData.GetMesh();
			root.GenerateMesh(meshData.Mesh, mat);
		}
		public static void GenerateMesh(this GameObject root, Mesh Mesh, Material mat = null)
		{
			var filter = root.GetComponent<MeshFilter>(true);
			if (Mesh == null)
			{
				Mesh = filter.sharedMesh;
			}
			else if (Mesh != filter.sharedMesh)
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
			var collider = root.GetComponent<MeshCollider>();
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
		public static void SetShareMaterails(this Renderer renderer, params Material[] materials)
		{
			if (Application.IsPlaying(renderer))
			{
				renderer.materials = materials;
			}
			else
			{
				renderer.sharedMaterials = materials;
			}
		}
		public static Mesh CombineMeshes(bool mergeSubMeshes, params Mesh[] meshes)
		{
			List<CombineInstance> combineInstances = new List<CombineInstance>();
			foreach (var meshInfo in meshes)
			{
				for (int i = 0; i < meshInfo.subMeshCount; i++)
				{
					combineInstances.Add(new CombineInstance
					{
						mesh = meshInfo,
						subMeshIndex = i,
						transform = Matrix4x4.identity,
					});
				}
			}
			var mesh = new Mesh();
			mesh.CombineMeshes(combineInstances.ToArray(), mergeSubMeshes);
			mesh.RecalculateBounds();
			return mesh;
		}
		public static Mesh CombineMeshes(this Mesh mesh, Mesh other = null, bool mergeSubMeshes = false)
		{
			return CombineMeshes(mergeSubMeshes, mesh, other);
		}
		public static void CombineMeshes(this SkinnedMeshRenderer root, SkinnedMeshRenderer[] meshes, params string[] combineTextures)
		{
			var animator = root.GetComponentInParent<Animator>();
			var childs = animator.GetComponentsInChildren<Transform>(true);

			#region 合并Mesh	
			var mats = new QList<Material>();
			root.sharedMesh = new Mesh();
			var combineInfos = new List<CombineInstance>();
			var uvs = new List<Vector2[]>();
			foreach (var skinedMesh in meshes)
			{
				uvs.Add(skinedMesh.sharedMesh.uv);
				mats.AddRange(skinedMesh.sharedMaterials);
				for (int sub = 0; sub < skinedMesh.sharedMesh.subMeshCount; sub++)
				{
					var combine = new CombineInstance();
					combine.mesh = skinedMesh.sharedMesh;
					combine.transform = skinedMesh.localToWorldMatrix;
					combine.subMeshIndex = sub;
					combineInfos.Add(combine);
				}
			}
			root.sharedMesh.CombineMeshes(combineInfos.ToArray(), combineTextures.Length != 0, true);
			root.sharedMesh.RecalculateNormals();
			if (combineTextures.Length == 0)
			{
				root.SetShareMaterails(mats.ToArray());
			}
			else if (mats.Count > 0)
			{
				Rect[] UVRects = null;
				var combineMaterial = new Material(mats[0]);
				var texSize = Mathf.Min(combineMaterial.mainTexture.width, 512);
				var combineSize = Mathf.Min(texSize * Mathf.CeilToInt(Mathf.Sqrt(mats.Count)), 2048);
				foreach (var textureKey in combineTextures)
				{
					var texs = new List<Texture2D>();
					foreach (var mat in mats)
					{
						var tex = mat.GetTexture(textureKey) as Texture2D;
						if (tex != null)
						{
							texs.Add(tex.ToSizeTexture(texSize, texSize));
						}
					}
					var combineTexture = new Texture2D(combineSize, combineSize);
					if (UVRects == null)
					{
						UVRects = combineTexture.PackTextures(texs.ToArray(), 0);
					}
					else
					{
						combineTexture.PackTextures(texs.ToArray(), 0);
					}
					combineMaterial.SetTexture(textureKey, combineTexture);
				}
				var index = 0;
				var combineUV = new QList<Vector2>();
				for (int i = 0; i < uvs.Count; i++)
				{
					foreach (var uv in uvs[i])
					{
						combineUV[index++] = new Vector2(Mathf.Lerp(UVRects[i].xMin, UVRects[i].xMax, uv.x), Mathf.Lerp(UVRects[i].yMin, UVRects[i].yMax, uv.y));
					}
				}
				root.sharedMesh.uv = combineUV.ToArray();
				root.SetShareMaterails(combineMaterial);
			}
			#endregion

			#region 构建骨骼
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
					bindppses.Add(bone.worldToLocalMatrix);
				}
			}
			root.bones = bones.ToArray();
			root.sharedMesh.boneWeights = boneWeights.ToArray();
			root.sharedMesh.bindposes = bindppses.ToArray();
			#endregion
		}

		public static SkinnedMeshRenderer Split(this SkinnedMeshRenderer skinnedMesh, HumanBodyBones humanBodyBone)
		{
			var mesh = skinnedMesh.sharedMesh;
			QMeshData splitMesh = new QMeshData();
			QMeshData bodyMesh = new QMeshData();
			splitMesh.bindposes.AddRange(mesh.bindposes);
			bodyMesh.bindposes.AddRange(mesh.bindposes);
			bool[] isBody = new bool[mesh.vertices.Length];
			int[] newTriangles = new int[mesh.vertices.Length];
			var bones = new List<Transform>(skinnedMesh.bones);
			var bodyBone = skinnedMesh.GetComponentInParent<Animator>().GetBoneTransform(humanBodyBone);
			bones.RemoveAll((bone) => !bone.ParentHas(bodyBone));
			for (int i = 0; i < newTriangles.Length; i++)
			{
				var vert = mesh.vertices[i];
				isBody[i] = bones.Contains(skinnedMesh.bones[mesh.boneWeights[i].boneIndex0]);
				if (isBody[i])
				{
					newTriangles[i] = splitMesh.vertices.Count;
					splitMesh.AddPoint(mesh, i);
				}
				else
				{
					newTriangles[i] = bodyMesh.vertices.Count;
					bodyMesh.AddPoint(mesh, i);
				}
			}
			int triangleCount = mesh.triangles.Length / 3;
			for (int i = 0; i < triangleCount; i++)
			{
				int a = mesh.triangles[i * 3];
				int b = mesh.triangles[i * 3 + 1];
				int c = mesh.triangles[i * 3 + 2];

				bool aIsUp = isBody[a];
				bool bIsUp = isBody[b];
				bool cIsUp = isBody[c];
				if (aIsUp && bIsUp && cIsUp)
				{
					splitMesh.triangles.Add(newTriangles[a]);
					splitMesh.triangles.Add(newTriangles[b]);
					splitMesh.triangles.Add(newTriangles[c]);
				}
				else if (!aIsUp && !bIsUp && !cIsUp)
				{
					bodyMesh.triangles.Add(newTriangles[a]);
					bodyMesh.triangles.Add(newTriangles[b]);
					bodyMesh.triangles.Add(newTriangles[c]);
				}
				else
				{
					int newA, newB, newC;
					if (bIsUp == cIsUp && aIsUp != bIsUp)
					{
						newA = a;
						newB = b;
						newC = c;
					}
					else if (cIsUp == aIsUp && bIsUp != cIsUp)
					{
						newA = b;
						newB = c;
						newC = a;
					}
					else
					{
						newA = c;
						newB = a;
						newC = b;
					}
				}
			}
			skinnedMesh.sharedMesh = bodyMesh.GetMesh();
			var newSkinnedMesh = skinnedMesh.transform.parent.GetChild(skinnedMesh.name + "_" + humanBodyBone).GetComponent<SkinnedMeshRenderer>(true);
			newSkinnedMesh.sharedMesh = splitMesh.GetMesh();
			return newSkinnedMesh;
		}
		public static void Split(this MeshFilter meshFilter, Vector3 point, Vector3 normal, bool fill = false)
		{
			point = meshFilter.transform.InverseTransformPoint(point);
			normal = meshFilter.transform.InverseTransformDirection(normal);
			normal.Scale(meshFilter.transform.localScale);
			normal.Normalize();
			var (aMesh, bMesh) = meshFilter.sharedMesh.Split(point, normal, fill);
			UnityEngine.Object.Instantiate(meshFilter.gameObject, meshFilter.transform.parent).GetComponent<MeshFilter>().sharedMesh = aMesh;
			UnityEngine.Object.Instantiate(meshFilter.gameObject, meshFilter.transform.parent).GetComponent<MeshFilter>().sharedMesh = bMesh;
			meshFilter.gameObject.CheckDestory();
		}
		public static (Mesh, Mesh) Split(this Mesh mesh, Vector3 point, Vector3 normal, bool fill = false)
		{
			QMeshData upMesh = new QMeshData();
			QMeshData downMesh = new QMeshData();
			upMesh.bindposes.AddRange(mesh.bindposes);
			downMesh.bindposes.AddRange(mesh.bindposes);
			bool[] isUp = new bool[mesh.vertices.Length];
			int[] newTriangles = new int[mesh.vertices.Length];

			for (int i = 0; i < newTriangles.Length; i++)
			{
				var vert = mesh.vertices[i];
				isUp[i] = Vector3.Dot(vert - point, normal) >= 0f;
				if (isUp[i])
				{
					newTriangles[i] = upMesh.vertices.Count;
					upMesh.AddPoint(mesh, i);
				}
				else
				{
					newTriangles[i] = downMesh.vertices.Count;
					downMesh.AddPoint(mesh, i);
				}
			}
			var fillPoints = new List<Vector3>();
			int triangleCount = mesh.triangles.Length / 3;
			for (int i = 0; i < triangleCount; i++)
			{
				int a = mesh.triangles[i * 3];
				int b = mesh.triangles[i * 3 + 1];
				int c = mesh.triangles[i * 3 + 2];

				bool aIsUp = isUp[a];
				bool bIsUp = isUp[b];
				bool cIsUp = isUp[c];
				if (aIsUp && bIsUp && cIsUp)
				{
					upMesh.triangles.Add(newTriangles[a]);
					upMesh.triangles.Add(newTriangles[b]);
					upMesh.triangles.Add(newTriangles[c]);
				}
				else if (!aIsUp && !bIsUp && !cIsUp)
				{
					downMesh.triangles.Add(newTriangles[a]);
					downMesh.triangles.Add(newTriangles[b]);
					downMesh.triangles.Add(newTriangles[c]);
				}
				else
				{
					int newA, newB, newC;
					if (bIsUp == cIsUp && aIsUp != bIsUp)
					{
						newA = a;
						newB = b;
						newC = c;
					}
					else if (cIsUp == aIsUp && bIsUp != cIsUp)
					{
						newA = b;
						newB = c;
						newC = a;
					}
					else
					{
						newA = c;
						newB = a;
						newC = b;
					}
					Vector3 pos0, pos1;
					if (isUp[newA])
						(pos0, pos1) = mesh.SplitTriangle(upMesh, downMesh, point, normal, newTriangles, newA, newB, newC);
					else
						(pos1, pos0) = mesh.SplitTriangle(downMesh, upMesh, point, normal, newTriangles, newA, newB, newC);

					fillPoints.Add(pos0);
					fillPoints.Add(pos1);
				}
			}
			upMesh.CombineVertices(0.001f);
			downMesh.CombineVertices(0.001f);
			if (fill && fillPoints.Count > 2)
			{
				var donwFillMesh = Fill(fillPoints, normal);
				var upFillMesh = donwFillMesh.GetReverseMesh();
				return (upMesh.GetMesh().CombineMeshes(upFillMesh), downMesh.GetMesh().CombineMeshes(donwFillMesh));
			}
			else
			{
				return (upMesh.GetMesh(), downMesh.GetMesh());
			}
		}

		private static (Vector3,Vector3) SplitTriangle(this Mesh mesh,QMeshData upMesh, QMeshData downMesh, Vector3 point, Vector3 normal, int[] newTriangles, int a, int b, int c)
		{
			Vector3 v0 = mesh.vertices[a];
			Vector3 v1 = mesh.vertices[b];
			Vector3 v2 = mesh.vertices[c];
			float topDot = Vector3.Dot(point - v0, normal);
			float aScale = Mathf.Clamp01(topDot / Vector3.Dot(v1 - v0, normal));
			float bScale = Mathf.Clamp01(topDot / Vector3.Dot(v2 - v0, normal));
			Vector3 pos_a = v0 + (v1 - v0) * aScale;
			Vector3 pos_b = v0 + (v2 - v0) * bScale;

			Vector2 u0 = mesh.uv[a];
			Vector2 u1 = mesh.uv[b];
			Vector2 u2 = mesh.uv[c];
			Vector3 uv_a = (u0 + (u1 - u0) * aScale);
			Vector3 uv_b = (u0 + (u2 - u0) * bScale);

			Vector3 n0 = mesh.normals[a];
			Vector3 n1 = mesh.normals[b];
			Vector3 n2 = mesh.normals[c];
			Vector3 normal_a = (n0 + (n1 - n0) * aScale).normalized;
			Vector3 normal_b = (n0 + (n2 - n0) * bScale).normalized;

			Vector4 t0 = mesh.tangents[a];
			Vector4 t1 = mesh.tangents[b];
			Vector4 t2 = mesh.tangents[c];
			Vector4 tangent_a = (t0 + (t1 - t0) * aScale).normalized;
			Vector4 tangent_b = (t0 + (t2 - t0) * bScale).normalized;
			tangent_a.w = t1.w;
			tangent_b.w = t2.w;

			int top_a = upMesh.vertices.Count;
			upMesh.Add(pos_a, uv_a, normal_a, tangent_a,mesh.boneWeights[a]);
			int top_b = upMesh.vertices.Count;
			upMesh.Add(pos_b, uv_b, normal_b, tangent_b, mesh.boneWeights[a]);
			upMesh.triangles.Add(newTriangles[a]);
			upMesh.triangles.Add(top_a);
			upMesh.triangles.Add(top_b);

			int down_a = downMesh.vertices.Count;
			downMesh.Add(pos_a, uv_a, normal_a, tangent_a, mesh.boneWeights[b]);
			int down_b = downMesh.vertices.Count;
			downMesh.Add(pos_b, uv_b, normal_b, tangent_b, mesh.boneWeights[b]);

			downMesh.triangles.Add(newTriangles[b]);
			downMesh.triangles.Add(newTriangles[c]);
			downMesh.triangles.Add(down_b);

			downMesh.triangles.Add(newTriangles[b]);
			downMesh.triangles.Add(down_b);
			downMesh.triangles.Add(down_a);

			return (pos_a, pos_b);
		}
		public static Mesh Copy(this Mesh mesh)
		{
			var newMesh = new Mesh();
			newMesh.vertices = mesh.vertices;
			newMesh.uv = mesh.uv;
			newMesh.colors32 = mesh.colors32;
			newMesh.triangles = mesh.triangles;
			newMesh.normals = mesh.normals;
			newMesh.tangents = mesh.tangents;
			return newMesh;
		}
		public static Mesh GetReverseMesh(this Mesh mesh)
		{
			mesh = mesh.Copy();
			var triangles = mesh.triangles;
			for (int i = 0; i < mesh.triangles.Length; i += 3)
			{
				var temp = triangles[i];
				triangles[i] = triangles[i + 2];
				triangles[i + 2] = temp;
			}
			mesh.triangles = triangles;
			var normals = mesh.normals;
			var tangents = mesh.tangents;
			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				normals[i] *= -1;
				mesh.tangents[i].w *= -1;
			}
			mesh.normals = normals;
			mesh.tangents = tangents;
			return mesh;
		}
	
		public static Mesh Fill(this List<Vector3> edges, Vector3 normal)
		{
			if (edges.Count < 3)
				throw new Exception("edges point less 3!");

			for (int i = 0; i < edges.Count - 3; i++)
			{
				Vector3 t = edges[i + 1];
				Vector3 temp = edges[i + 3];
				for (int j = i + 2; j < edges.Count - 1; j += 2)
				{
					if ((edges[j] - t).sqrMagnitude < 1e-6)
					{
						edges[j] = edges[i + 2];
						edges[i + 3] = edges[j + 1];
						edges[j + 1] = temp;
						break;
					}
					if ((edges[j + 1] - t).sqrMagnitude < 1e-6)
					{
						edges[j + 1] = edges[i + 2];
						edges[i + 3] = edges[j];
						edges[j] = temp;
						break;
					}
				}
				edges.RemoveAt(i + 2);
			}
			edges.RemoveAt(edges.Count - 1);

			Vector4 tangent = normal.CalculateTangent();

			var cutEdges = new QMeshData();
			for (int i = 0; i < edges.Count; i++)
				cutEdges.Add(edges[i], Vector2.zero, normal, tangent,default);
			int count = edges.Count - 1;
			for (int i = 1; i < count; i++)
			{
				cutEdges.triangles.Add(0);
				cutEdges.triangles.Add(i);
				cutEdges.triangles.Add(i + 1);
			}
			return cutEdges.GetMesh();
		}
	}
}
