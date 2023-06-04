using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QCombineMesh : MonoBehaviour
	{
		[QName("模型"),QOnChange(nameof(FreshRenderers))]
		public List<GameObject> modelRoot;
		[QName("合并模块"),QEnum("get_" + nameof(Renderers))]
		public List<string> skinnedMeshKeys;
		[QName("合并贴图")]
		public string[] combineTextures;
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
				var avatar= model.GetComponent<Animator>()?.avatar;
				if (avatar != null)
				{
					GetComponent<Animator>().avatar = avatar;
				}
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
				if (renderers.ContainsKey(r.name, (obj) => obj.name))
				{
					r.gameObject.SetActive(false);
				}
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
				transform.GetChild(nameof(QCombineMesh), true).GetComponent<SkinnedMeshRenderer>(true).CombineMeshs(combineMeshs.ToArray(), combineTextures);
			}
		}
		public void CheckBone(SkinnedMeshRenderer skinedMeshRenderer)
		{
			if (rootBone == null)
			{
				if (skinedMeshRenderer.rootBone!=null)
				{
					var bone = skinedMeshRenderer.rootBone;
					while (bone.parent!=null&&bone.parent.parent!=null)
					{
						bone = bone.parent;
					}
					if(transform.GetChild(bone.name) == null)
					{
						QDebug.Log(gameObject + " 创建骨骼 " + bone);
						rootBone = Instantiate(bone, transform);
						rootBone.name = bone.name;
						rootBone.SetAsFirstSibling();
						var meshRnderers = GetComponentsInChildren<MeshRenderer>();
						for (int i = 0; i < meshRnderers.Length; i++)
						{
							var meshRenderer = meshRnderers[i];
							meshRenderer.gameObject.CheckDestory();
						}
					}
				}
			}
		}
	}
	
}

