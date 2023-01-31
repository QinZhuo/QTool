using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[ExecuteInEditMode, DisallowMultipleComponent]
	public class QLightmapPrefab : MonoBehaviour
	{
		[SerializeField]
		private List<QLightmapData> lightmapDatas = new List<QLightmapData>();
		[SerializeField]
		private List<QLightmapInfo> lightmapInfos = new List<QLightmapInfo>();
		void Awake()
		{
			var lightmapList =new List<LightmapData>(LightmapSettings.lightmaps);
			foreach (var lightmap in lightmapDatas)
			{
				if (lightmap.lightmapDir == null || lightmap.lightmapColor == null) continue;
				lightmapList.Add(lightmap);
			}
			foreach (var info in lightmapInfos)
			{
				if (lightmapList.Get(info.lightmapIndex) == null) continue;
				if (info.renderer != null)
				{
					info.renderer.lightmapIndex = info.lightmapIndex + LightmapSettings.lightmaps.Length;
					info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;
				}
				else if (info.terrain != null) 
				{
					info.terrain.lightmapIndex = info.lightmapIndex + LightmapSettings.lightmaps.Length;
					info.terrain.lightmapScaleOffset = info.lightmapOffsetScale;
				}
			}
			LightmapSettings.lightmaps = lightmapList.ToArray();
		}

		[System.Serializable]
		struct QLightmapInfo
		{
			public Terrain terrain;
			public Renderer renderer;
			public int lightmapIndex;
			public Vector4 lightmapOffsetScale;
		}
		[System.Serializable]
		struct QLightmapData
		{
			public Texture2D lightmapColor;
			public Texture2D lightmapDir;
		
			public static implicit operator LightmapData(QLightmapData lightmap)
			{
				return new LightmapData
				{
					lightmapColor = lightmap.lightmapColor,
					lightmapDir = lightmap.lightmapDir,
				};
			}
			public override int GetHashCode()
			{
				return lightmapColor.name.GetHashCode() + lightmapDir.GetHashCode();
			}
		}
#if UNITY_EDITOR
	
		private bool IsPrefab => gameObject.IsPrefabInstance(out var prefab)&&!Application.isPlaying&&
					!UnityEditor.Lightmapping.isRunning;
		[QName("生成光照贴图信息",nameof(IsPrefab))]
		private void GenerateLightmap()
		{
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
				{
					Debug.LogError("生成光照信息数据要求您删除已烘焙光照信息并禁用自动模式");
					return;
				}
				if (!gameObject.isStatic)
				{
					Debug.LogError(gameObject +"."+ nameof(gameObject.isStatic)+" 为false 只有静态物体才能被烘培光照");
					return;
				}
				if (!gameObject.activeSelf)
				{
					gameObject.SetActive(true);
				}
				foreach (var lightObj in GameObject.FindObjectsOfType<QLightmapPrefab>())
				{
					if (lightObj == this) continue;
					if (lightObj.gameObject.activeInHierarchy)
					{
						lightObj.gameObject.SetActive(false);
					}
				}
				lightmapDatas.Clear();
				lightmapInfos.Clear();
				if (UnityEditor.Lightmapping.BakeAsync())
				{
					UnityEditor.Lightmapping.bakeCompleted += BakeOver;
				}
			}
			else
			{
				Debug.LogError(gameObject + " 不是预制体实例 无法烘培光照贴图");
			}
		}
		private void BakeOver()
		{
			UnityEditor.Lightmapping.bakeCompleted -= BakeOver;
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
				{
					var lightmap = LightmapSettings.lightmaps[i];
					var colorPath = path.Replace(".prefab", "/" + nameof(lightmap) + "_" + i + "_color.exr");
					colorPath.CheckDirectoryPath();
					UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(lightmap.lightmapColor), colorPath);
					var dirPath = path.Replace(".prefab", "/" + nameof(lightmap) + "_" + i + "_dir.png");
					UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(lightmap.lightmapDir), dirPath);
					lightmapDatas.Add(new QLightmapData
					{
						lightmapColor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath),
						lightmapDir = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(dirPath),
					}); ;
				}
				var renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer renderer in renderers)
				{
					if (renderer.lightmapIndex < 0) continue;
					lightmapInfos.Add(new QLightmapInfo
					{
						lightmapIndex = renderer.lightmapIndex,
						renderer = renderer,
						lightmapOffsetScale = renderer.lightmapScaleOffset,
					});
				}
				var Terrainrenderers = gameObject.GetComponentsInChildren<Terrain>();
				foreach (var terrain in Terrainrenderers)
				{
					if (terrain.lightmapIndex < 0) continue;
					lightmapInfos.Add(new QLightmapInfo
					{
						lightmapIndex = terrain.lightmapIndex,
						terrain = terrain,
						lightmapOffsetScale = terrain.lightmapScaleOffset,
					});
				}
				UnityEditor.PrefabUtility.ApplyPrefabInstance(gameObject, UnityEditor.InteractionMode.AutomatedAction);
			}

		}
#endif
	}
}
