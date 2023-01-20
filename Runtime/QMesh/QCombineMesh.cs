using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.Mesh
{
	[RequireComponent(typeof(Animator))]
	public class QCombineMesh : MonoBehaviour
	{
		[QOnChange(nameof(FreshRenderers))]
		public List<GameObject> modelRoot;
		[QEnum("get_" + nameof(Renderers))]
		public List<string> skinnedMeshKeys;
		[HideInInspector]
		public List<Renderer> renderers;
		List<Renderer> Renderers => renderers;
		[HideInInspector]
		public Transform rootBone;

		private void Awake()
		{
			FreshMesh();
		}
		void FreshRenderers()
		{
			renderers?.Clear();
			foreach (var model in modelRoot)
			{
				if (model == null) continue;
				var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
				if (modelRenderers.Length > 0)
				{
					if (rootBone == null)
					{
						foreach (var renderer in modelRenderers)
						{
							if (renderer is SkinnedMeshRenderer skinnedMesh)
							{
								CheckBone(skinnedMesh);
								break;
							}
						}
					}
					renderers.AddRange(modelRenderers);
				}
			}
		}
		[ContextMenu("刷新模型")]
		public void FreshMesh()
		{
			FreshRenderers();
			var combineMeshs = new List<SkinnedMeshRenderer>();
			var meshRnderers= GetComponentsInChildren<MeshRenderer>();
			foreach (var r in meshRnderers)
			{
				r.gameObject.SetActive(false);
			}
			foreach (var meshName in skinnedMeshKeys)
			{
				if (meshName.IsNull()) continue;
				var mesh = renderers.Get(meshName, (mesh) => mesh.name);
				if (mesh == null)
				{
					Debug.LogError("找不到网格[" + meshName + "]");
				}
				else if( mesh is SkinnedMeshRenderer skinnedMesh)
				{
					combineMeshs.Add(skinnedMesh);
				}
				else if(mesh is MeshRenderer renderer)
				{
					var meshFilter = mesh.GetComponent<MeshFilter>(true);
					if (meshFilter != null)
					{
						if(meshFilter.transform.GetPath().SplitTowString(rootBone.name+".",out var start,out var end))
						{
							var child= rootBone.GetChild(end,true);
							if (child != null)
							{
								child.GetComponent<MeshRenderer>(true).sharedMaterials = renderer.sharedMaterials;
								child.GetComponent<MeshFilter>(true).sharedMesh = meshFilter.sharedMesh;
								if (!child.gameObject.activeSelf)
								{
									child.gameObject.SetActive(true);
								}
							}
						}
					}
				}
			}
			if (combineMeshs.Count > 0)
			{
				gameObject.GetComponent<SkinnedMeshRenderer>(true).CombineMeshs(combineMeshs.ToArray());
			}
		}
		public void CheckBone(SkinnedMeshRenderer skineedMeshRenderer)
		{
			if (rootBone == null)
			{
				if (skineedMeshRenderer.rootBone!=null)
				{
					var bone = skineedMeshRenderer.rootBone;
					while (bone.parent!=null&&bone.parent.parent!=null)
					{
						bone = bone.parent;
					}
					rootBone = Instantiate(bone, transform);
					rootBone.name = bone.name;
					rootBone.SetAsFirstSibling();
				}
			}
			var meshRnderers = GetComponentsInChildren<MeshRenderer>();
			for (int i = 0; i < meshRnderers.Length; i++)
			{
				var meshRenderer = meshRnderers[i];
				meshRenderer.gameObject.CheckDestory();
			}
		}
	}
	
}

