using System.Collections;
using System.Collections.Generic;
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
		public static void Clear()
		{
			Bounds =default;
			navMesh = null;
			navMeshInstance.Remove();
		}
		public static void AddRoot(Transform root)
		{
			Bounds.Encapsulate(root.GetBounds());
			Roots.AddCheckExist(root);
		}
		public static void UpdateNavMesh()
		{
			Markups.Clear();
			SourceList.Clear();
			foreach (var root in Roots)
			{
				NavMeshBuilder.CollectSources(root, -1, NavMeshCollectGeometry.RenderMeshes, 0, Markups, SourceList);
			}
			navMesh = new NavMeshData();
			navMeshInstance = NavMesh.AddNavMeshData(navMesh);
			NavMeshBuilder.UpdateNavMeshData(navMesh, NavMesh.GetSettingsByID(0), SourceList, Bounds);
		}

	}
}

