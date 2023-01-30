using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[ExecuteInEditMode, DisallowMultipleComponent]
	public class QPrefabLigthmap : MonoBehaviour
	{
		[SerializeField]
		private List<QLightmapInfo> rendererInfos = new List<QLightmapInfo>();
		[SerializeField]
		private List<QLightmapData> lightmapDatas = new List<QLightmapData>();
	
		void Awake()
		{
			if (rendererInfos.Count == 0)
				return;

			var lightmaps = LightmapSettings.lightmaps;
			var combinedLightmaps = new LightmapData[lightmaps.Length + lightmapDatas.Count];
			lightmaps.CopyTo(combinedLightmaps, 0);
			for (int i = 0; i < lightmapDatas.Count; i++)
			{
				combinedLightmaps[i + lightmaps.Length] = lightmapDatas[i];
			}
			foreach (var info in rendererInfos)
			{
				if (info.renderer != null)
				{
					info.renderer.lightmapIndex = info.lightmapIndex + lightmaps.Length;
					info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;
				}
				else if (info.terrain != null)
				{
					info.terrain.lightmapIndex = info.lightmapIndex + lightmaps.Length;
					info.terrain.lightmapScaleOffset = info.lightmapOffsetScale;
				}
			}
			LightmapSettings.lightmaps = combinedLightmaps;
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
	
		public bool IsPrefab => gameObject.IsPrefabInstance(out var prefab);
		[QName("生成光照贴图信息",nameof(IsPrefab))]
		public void GenerateLightmap()
		{
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
				{
					Debug.LogError("生成光照信息数据要求您删除已烘焙光照信息并禁用自动模式");
					return;
				}
				UnityEditor.Lightmapping.Bake();
				rendererInfos.Clear();
				lightmapDatas.Clear();
				var renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer renderer in renderers)
				{
					if (renderer.lightmapIndex != -1)
					{
						QLightmapInfo info = new QLightmapInfo();
						info.renderer = renderer;
						if (renderer.lightmapScaleOffset != Vector4.zero)
						{
							info.lightmapOffsetScale = renderer.lightmapScaleOffset;
							var data = LightmapSettings.lightmaps[renderer.lightmapIndex]; 
							//info.lightmapIndex = lightmapDatas.IndexOf(data);
							//if (info.lightmapIndex == -1)
							//{
							//	info.lightmapIndex = lightmapDatas.Count;
							//	//UnityEditor.AssetDatabase.GetAssetPath(data.lightmapColor).FileCount
							//	UnityEditor.AssetDatabase.CreateAsset(data.lightmapColor, path.Replace(".prefab",".exr"));
							//	lightmapDatas.Add(data);
							//}
							rendererInfos.Add(info);
						}
					}
				}
				var Terrainrenderers = gameObject.GetComponentsInChildren<Terrain>();
				foreach (var terrain in Terrainrenderers)
				{
					if (terrain.lightmapIndex != -1)
					{
						QLightmapInfo info = new QLightmapInfo();
						info.terrain = terrain;
						if (terrain.lightmapScaleOffset != Vector4.zero)
						{
							info.lightmapOffsetScale = terrain.lightmapScaleOffset;
							var data = LightmapSettings.lightmaps[terrain.lightmapIndex];
							//info.lightmapIndex = lightmapDatas.IndexOf(data);
							//if (info.lightmapIndex == -1)
							//{
							//	info.lightmapIndex = lightmapDatas.Count;
							//	lightmapDatas.Add(data);
							//}
							rendererInfos.Add(info);
						}
					}
				}
				UnityEditor.PrefabUtility.ApplyPrefabInstance(gameObject, UnityEditor.InteractionMode.AutomatedAction);
			}
			else
			{
				Debug.LogError(gameObject + "不是预制体实例 无法烘培光照贴图");
			}
		}
#endif
	}
}
