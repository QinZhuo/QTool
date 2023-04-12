using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QTool
{
	public static class QNavMesh
	{
		private static Bounds Bounds = default;
		private static NavMeshDataInstance navMeshInstance = default;
		private static NavMeshData navMesh = null;
		private static List<NavMeshBuildSource> SourceList = new List<NavMeshBuildSource>();
		private static List<Transform> Roots = new List<Transform>();
		private static List<NavMeshBuildMarkup> Markups = new List<NavMeshBuildMarkup>();
		public static bool IsUpdateOver = true;
		public static void Clear()
		{
			Bounds =default;
			navMesh = null;
			Roots.Clear();
			navMeshInstance.Remove();
		}
		public static async void AddRoot(Transform root)
		{
			if (!Roots.Contains(root))
			{
				Bounds.Encapsulate(root.GetBounds());
				Roots.Add(root);
				await UpdateNavMeshAsync();
			}
		}
		public static async void RemoveRoot(Transform root)
		{
			if (Roots.Contains(root))
			{
				Roots.Remove(root);
				await UpdateNavMeshAsync();
			}
		}
		public async static Task UpdateNavMeshAsync()
		{
			navMeshInstance.Remove();
			Markups.Clear();
			SourceList.Clear();
			foreach (var root in Roots)
			{
				NavMeshBuilder.CollectSources(root, -1, NavMeshCollectGeometry.RenderMeshes, 0, Markups, SourceList);
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
	}
}

