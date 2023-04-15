using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
namespace QTool
{
	[ExecuteInEditMode]
	public class QWeaponTrail : MonoBehaviour
	{
		public struct TrailShot
		{
			public readonly float timeStamp;
			public readonly Vector3 top;
			public readonly Vector3 bottom;

			public Vector3 Center => (top + bottom) * 0.5f;
			public Vector3 Radius => (top - bottom) * 0.5f;

			public TrailShot(float timeStamp, Vector3 top, Vector3 bottom)
			{
				this.timeStamp = timeStamp;
				this.top = top;
				this.bottom = bottom;
			}
		}

		private const int CountOfPointOnStick = 3;

		private List<TrailShot> stickshotBuffer = new List<TrailShot>();
		[Min(0)]

		public float duration = 0.1f;

		[Range(1f, 30f)]
		[SerializeField] private float m_DegreeResolution = 1f;
		public float degreeResolution
		{
			get => m_DegreeResolution;
			set => m_DegreeResolution = Mathf.Max(1f, value);
		}

		public Transform Start => transform.GetChild(nameof(Start), true);
		public Transform End => transform.GetChild(nameof(End), true);
		public Material material;
		private Mesh m_Mesh = null;
		public Mesh mesh
		{
			get
			{
				if (m_Mesh == null)
				{
					m_Mesh = new Mesh { name = "Trail Effect" };
					m_Mesh.MarkDynamic();
				}
				return m_Mesh;
			}
		}

		private void LateUpdate()
		{
			Profiler.BeginSample(nameof(LateUpdate));

			UpdateFrameBuffer();
			if (stickshotBuffer.Count > 1)
			{
				var verticesMaxCount = stickshotBuffer.Count * Mathf.CeilToInt(180f / degreeResolution);
				var vertices = new NativeArray<Vector3>(verticesMaxCount * CountOfPointOnStick, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var verticesCount = UpdateSegments(vertices);
				UpdateMesh(vertices, verticesCount);
				Graphics.DrawMesh(mesh, Matrix4x4.identity, material, gameObject.layer, null, 0, null, false, false, false);
				vertices.Dispose();
			}
			else
			{
				mesh.Clear(true);
			}

			Profiler.EndSample();
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var color = Gizmos.color;
			var vertices = mesh.vertices;
			var interpolatedCount = vertices.Length / CountOfPointOnStick;
			if (interpolatedCount > stickshotBuffer.Count)
			{
				Gizmos.color = Color.green;
				for (int i = 0; i < interpolatedCount; ++i)
				{
					int baseIndex = i * CountOfPointOnStick;
					Gizmos.DrawLine(vertices[baseIndex], vertices[baseIndex + 1]);
					Gizmos.DrawLine(vertices[baseIndex + 1], vertices[baseIndex + 2]);
				}
			}
			Gizmos.color = Color.black;
			for (int i = 0; i < stickshotBuffer.Count; ++i)
			{
				var stickshot = stickshotBuffer[i];
				Gizmos.DrawLine(stickshot.bottom, stickshot.top);
			}
			Gizmos.color = color;
		}
#endif

		private void OnDestroy()
		{
			if (m_Mesh != null)
			{
				m_Mesh.CheckDestory();
			}
		}

		private void OnDisable()
		{
			stickshotBuffer.Clear();
		}

		private void UpdateFrameBuffer()
		{
			Profiler.BeginSample(nameof(UpdateFrameBuffer));
			var time = Time.time;

			while (stickshotBuffer.Count != 0)
			{
				if (time < stickshotBuffer[0].timeStamp + duration)
					break;

				stickshotBuffer.Dequeue();
			}
			stickshotBuffer.Add(new TrailShot(time, Start.position, End.position));
			Profiler.EndSample();
		}

		private int UpdateSegments(NativeArray<Vector3> vertices)
		{
			Profiler.BeginSample(nameof(UpdateSegments));
			int count = stickshotBuffer.Count;
			int verticesCount = 0;
			for (int i = 0, j = 1; j < count; ++i, ++j)
			{
				var stickshotP0 = stickshotBuffer[i];
				var stickshotP1 = stickshotBuffer[j];
				var stickshotC0 = i > 0 ? stickshotBuffer[i - 1] : stickshotP0;
				var stickshotC1 = j + 1 < count ? stickshotBuffer[j + 1] : stickshotP1;
				Vector3 centerP0 = stickshotP0.Center, radiusP0 = stickshotP0.Radius;
				Vector3 centerP1 = stickshotP1.Center, radiusP1 = stickshotP1.Radius;
				Vector3 centerC0 = stickshotC0.Center, radiusC0 = stickshotC0.Radius;
				Vector3 centerC1 = stickshotC1.Center, radiusC1 = stickshotC1.Radius;
				var deltaDegrees = Math.Max(
					Vector3.Angle(centerP1 - centerC0, centerC1 - centerP0),
					Vector3.Angle(radiusP0, radiusP1)
				);
				var interpolations = Mathf.CeilToInt(deltaDegrees / degreeResolution) + 1;
				if (interpolations > 1)
				{
					for (int k = 0; k < interpolations; ++k)
					{
						var t = (float)k / interpolations;
						Vector3 center = QCatmullRom.CatmullRom(centerC0, centerP0, centerP1, centerC1, t);
						Vector3 radius = QCatmullRom.CatmullRom(radiusC0, radiusP0, radiusP1, radiusC1, t);
						vertices[verticesCount++] = center - radius; // bottom
						vertices[verticesCount++] = center; // center
						vertices[verticesCount++] = center + radius; // top
					}
				}
				else
				{
					var center = stickshotP0.Center;
					var radius = stickshotP0.Radius;
					vertices[verticesCount++] = center - radius; // bottom
					vertices[verticesCount++] = center; // center
					vertices[verticesCount++] = center + radius; // top
				}
			}

			{
				var stickshot = stickshotBuffer[count - 1];
				var center = stickshot.Center;
				var radius = stickshot.Radius;
				vertices[verticesCount++] = center - radius; // bottom
				vertices[verticesCount++] = center; // center
				vertices[verticesCount++] = center + radius; // top
			}
			Profiler.EndSample();

			return verticesCount;
		}

		private void UpdateMesh(NativeArray<Vector3> vertices, int verticesCount)
		{
			int stickshotsCount = verticesCount / CountOfPointOnStick;
			int segmentsCount = stickshotsCount - 1;
			int indicesCountPerSegment = (CountOfPointOnStick - 1) * 6;
			var indices = new NativeArray<int>(segmentsCount * indicesCountPerSegment, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var uv0 = new NativeArray<Vector2>(verticesCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

			Profiler.BeginSample(nameof(UpdateMesh));
			for (int i = 0; i < segmentsCount; ++i)
			{
				int leftBottom = i * CountOfPointOnStick;
				int leftCenter = i * CountOfPointOnStick + 1;
				int leftTop = i * CountOfPointOnStick + 2;
				int rightBottom = (i + 1) * CountOfPointOnStick;
				int rightCenter = (i + 1) * CountOfPointOnStick + 1;
				int rightTop = (i + 1) * CountOfPointOnStick + 2;

				int baseIndex = i * indicesCountPerSegment;
				indices[baseIndex++] = leftBottom;
				indices[baseIndex++] = leftCenter;
				indices[baseIndex++] = rightCenter;
				indices[baseIndex++] = rightCenter;
				indices[baseIndex++] = rightBottom;
				indices[baseIndex++] = leftBottom;
				indices[baseIndex++] = leftCenter;
				indices[baseIndex++] = leftTop;
				indices[baseIndex++] = rightTop;
				indices[baseIndex++] = rightTop;
				indices[baseIndex++] = rightCenter;
				indices[baseIndex] = leftCenter;
			}

			for (int i = 0; i < stickshotsCount; ++i)
			{
				float u = 1f - (float)i / segmentsCount;
				uv0[i * CountOfPointOnStick] = new Vector2(u, 0f);
				uv0[i * CountOfPointOnStick + 1] = new Vector2(u, 0.5f);
				uv0[i * CountOfPointOnStick + 2] = new Vector2(u, 1f);
			}

			mesh.Clear(true);
			mesh.SetVertices(vertices, 0, verticesCount);
			mesh.SetIndices(indices, 0, segmentsCount * indicesCountPerSegment, MeshTopology.Triangles, 0);
			mesh.SetUVs(0, uv0, 0, verticesCount);
			Profiler.EndSample();

			indices.Dispose();
			uv0.Dispose();
		}
	}
}
