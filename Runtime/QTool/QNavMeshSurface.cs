using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
#if Navigation
using Unity.AI.Navigation;
#endif
using UnityEngine;
using UnityEngine.AI;

namespace QTool
{
	[ExecuteInEditMode]
	public class QNavMeshSurface : MonoBehaviour
	{
		public LayerMask Layer = NavMesh.AllAreas;
		public NavMeshCollectGeometry CollectGeometry = NavMeshCollectGeometry.PhysicsColliders;

		private List<NavMeshBuildSource> SourceList = new List<NavMeshBuildSource>();
		private List<NavMeshBuildMarkup> Markups = new List<NavMeshBuildMarkup>();
		private NavMeshDataInstance navMeshInstance = default;
		private NavMeshData navMesh = null;
		private void OnEnable()
		{
			UpdateNavMesh();
		}
		private void OnValidate()
		{
			UpdateNavMesh();
		}
		private void OnTransformChildrenChanged()
		{
			UpdateNavMesh();
		}
		private void OnDisable()
		{
			Clear();
		}
		public void UpdateNavMesh()
		{
			Clear();
			if (!enabled)
			{
				return;
			}
			var bounds = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse.MultiplyBounds(transform.GetBounds());
			bounds.Expand(0.1f);
			CollectSources(transform, Layer, CollectGeometry, Markups, SourceList);
			var setting = NavMesh.GetSettingsByID(0);
			navMesh = NavMeshBuilder.BuildNavMeshData(setting, SourceList, bounds, transform.position, transform.rotation);
			navMeshInstance = NavMesh.AddNavMeshData(navMesh, transform.position, transform.rotation);
			navMeshInstance.owner = this;
			NavMeshBuilder.UpdateNavMeshData(navMesh, NavMesh.GetSettingsByID(0), SourceList, bounds);
		}
		public void Clear()
		{
			Markups.Clear();
			SourceList.Clear();
			navMeshInstance.Remove();
		}

		private bool CanCollect(Component root, out int area, int includedLayerMask)
		{
			area = 0;
			if (root is Renderer renderer)
			{
				if (!renderer.enabled)
				{
					return false;
				}
			}
			else if (root is Behaviour behaviour)
			{
				if (!behaviour.enabled)
				{
					return false;
				}
			}
			if (((0x1 << root.gameObject.layer) & includedLayerMask) == 0)
			{
				return false;
			}
#if Navigation
			var modifier = root.GetComponent<NavMeshModifier>();
			if (modifier != null)
			{
				if (modifier.overrideArea)
				{
					area = modifier.area;
				}
				if (modifier.ignoreFromBuild)
				{
					return false;
				}
			}
#endif
			return true;
		}

		public void CollectSources(Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
		{
			markups.Clear();
			results.Clear();
			var sourceList = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(root, includedLayerMask, geometry, 0, markups, sourceList);
			switch (CollectGeometry)
			{
				case NavMeshCollectGeometry.RenderMeshes:
					{
						var sprites = root.GetComponentsInChildren<SpriteRenderer>();
						foreach (var sprite in sprites)
						{
							if (sprite?.sprite == null) continue;
							var src = new NavMeshBuildSource();
							src.shape = NavMeshBuildSourceShape.Mesh;
							src.area = 0;
							src.component = sprite;
							src.sourceObject = sprite.sprite.GetMesh();
							src.transform = Matrix4x4.TRS(sprite.transform.position, sprite.transform.rotation, sprite.transform.lossyScale);
							sourceList.Add(src);
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
							src.area = 0;
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
							sourceList.Add(src);
						}
					}
					break;
				default:
					break;
			}
			foreach (var source in sourceList)
			{
				if(CanCollect(source.component,out var newArea, includedLayerMask))
				{
					var newSource = source;
					newSource.area = newArea;
					results.Add(newSource);
				}
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

