using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
namespace QTool
{
	[ExecuteInEditMode]
	public class QNavMeshSurface : MonoBehaviour
	{
		private static List<NavMeshBuildSource> SourceList = new List<NavMeshBuildSource>();
		private static List<NavMeshBuildMarkup> Markups = new List<NavMeshBuildMarkup>();
		private NavMeshDataInstance navMeshInstance = default;
		private NavMeshData navMesh = null;
		public NavMeshCollectGeometry CollectGeometry = NavMeshCollectGeometry.PhysicsColliders;
		private void OnEnable()
		{
			_ = UpdateNavMeshAsync();
		}
		private void OnDisable()
		{
			Clear();
		}
		private void OnValidate()
		{
			_ = UpdateNavMeshAsync();
		}
		private void OnTransformChildrenChanged()
		{
			_ = UpdateNavMeshAsync();
		}
		public async Task UpdateNavMeshAsync()
		{
			Clear();
			if (!enabled)
			{
				return;
			}
			var bounds = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse.MultiplyBounds(transform.GetBounds());
			bounds.Expand(0.1f);
			CollectSources(transform, -1, CollectGeometry, 0, Markups, SourceList);
			var setting = NavMesh.GetSettingsByID(0);
			navMesh = NavMeshBuilder.BuildNavMeshData(setting, SourceList, bounds, transform.position, transform.rotation);
			navMeshInstance = NavMesh.AddNavMeshData(navMesh, transform.position, transform.rotation);
			navMeshInstance.owner = this;
			if (Application.isPlaying)
			{
				await NavMeshBuilder.UpdateNavMeshDataAsync(navMesh, NavMesh.GetSettingsByID(0), SourceList, bounds);
			}
			else
			{
				NavMeshBuilder.UpdateNavMeshData(navMesh, NavMesh.GetSettingsByID(0), SourceList, bounds);
			}
		}
		public void Clear()
		{
			Markups.Clear();
			SourceList.Clear();
			navMeshInstance.Remove();
		}
		public void CollectSources(Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
		{
			NavMeshBuilder.CollectSources(root, includedLayerMask, geometry, defaultArea, markups, results);
			switch (CollectGeometry)
			{
				case NavMeshCollectGeometry.RenderMeshes:
					{
						var sprites = root.GetComponentsInChildren<SpriteRenderer>();
						foreach (var sprite in sprites)
						{
							var src = new NavMeshBuildSource();
							src.shape = NavMeshBuildSourceShape.Mesh;
							src.area = defaultArea;
							src.component = sprite;
							src.sourceObject = sprite.sprite.GetMesh();
							src.transform= Matrix4x4.TRS(sprite.transform.position, sprite.transform.rotation, sprite.transform.lossyScale);
							results.Add(src);
						}
					}
					break;
				case NavMeshCollectGeometry.PhysicsColliders:
					{
						var colliders = root.GetComponentsInChildren<Collider2D>();
						foreach (var collider in colliders)
						{
							var src = new NavMeshBuildSource();
							src.shape = NavMeshBuildSourceShape.Mesh;
							src.area = defaultArea;
							src.component = collider;
							src.sourceObject = collider.GetMesh();
							if (collider.attachedRigidbody)
							{
								src.transform = Matrix4x4.TRS(collider.attachedRigidbody.transform.position, collider.attachedRigidbody.transform.rotation, Vector3.one);
							}
							else
							{
								src.transform = Matrix4x4.identity;
							}
							results.Add(src);
						}
					}
					break;
				default:
					break;
			}
		}
		private void OnDrawGizmosSelected()
		{
			if (navMesh != null)
			{
				QGizmos.StartMatrix(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one));
				Gizmos.color = Color.red;
				Gizmos.DrawWireCube(navMesh.sourceBounds.center, navMesh.sourceBounds.size);
				QGizmos.EndMatrix();
			}
		}
	}
}

