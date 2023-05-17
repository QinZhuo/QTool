using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QTool
{
	public static class QNavMesh
	{
		public static Bounds Bounds = default;
		private static NavMeshDataInstance navMeshInstance = default;
		private static NavMeshData navMesh = null;
		private static List<NavMeshBuildSource> SourceList = new List<NavMeshBuildSource>();
		private static List<Transform> Roots = new List<Transform>();
		private static List<NavMeshBuildMarkup> Markups = new List<NavMeshBuildMarkup>();
		public static NavMeshCollectGeometry CollectGeometry { get; set; } = NavMeshCollectGeometry.RenderMeshes;
		public static bool IsUpdateOver = true;
		public static void Clear()
		{
			Bounds =default;
			navMesh = null;
			Roots.Clear();
			navMeshInstance.Remove();
		}
		public static async void UpdateNavMesh()
		{
			await UpdateNavMeshAsync();
		}
		public static void AddRoot(Transform root)
		{
			if (!Roots.Contains(root))
			{
				Bounds.Encapsulate(root.GetBounds());
				Roots.Add(root);
				UpdateNavMesh();
			}
		}
		public static void RemoveRoot(Transform root)
		{
			if (Roots.Contains(root))
			{
				Roots.Remove(root);
				UpdateNavMesh();
			}
		}
		public async static Task UpdateNavMeshAsync()
		{
			navMeshInstance.Remove();
			Markups.Clear();
			SourceList.Clear();
			foreach (var root in Roots)
			{
				NavMeshBuilder.CollectSources(root, -1, CollectGeometry, 0, Markups, SourceList);
			}
			navMesh = new NavMeshData();
			navMeshInstance = NavMesh.AddNavMeshData(navMesh);
			if (Application.isPlaying)
			{
				await NavMeshBuilder.UpdateNavMeshDataAsync(navMesh, NavMesh.GetSettingsByID(0), SourceList, Bounds);
			}
			else
			{
				NavMeshBuilder.UpdateNavMeshData(navMesh, NavMesh.GetSettingsByID(0), SourceList, Bounds);
			}
		}
		public static bool CalculatePath(this NavMeshPath path, Vector3 sourcePosition, Vector3 targetPosition, int areaMask = NavMesh.AllAreas)
		{
			return NavMesh.CalculatePath(sourcePosition, targetPosition, areaMask, path);
		}
	}
}

