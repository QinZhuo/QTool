using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QBillboardPrefab : MonoBehaviour
	{
#if UNITY_EDITOR
		private bool IsPrefab => gameObject.IsPrefabInstance(out var prefab) && !Application.isPlaying;
		[QName("材质")]
		public Material material;
		public int size = 256;
		public int count = 8;
		[QName("烘培广告牌资源",nameof(IsPrefab))]
		void Bake()
		{
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				BillboardAsset billboard = new BillboardAsset();
				var renderer = transform.GetChild(nameof(BillboardRenderer), true).GetComponent<BillboardRenderer>(true);
				string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				if (material == null)
				{
					material = new Material(Shader.Find("Universal Render Pipeline/Nature/SpeedTree7 Billboard"));
					material.SetColor("_HueVariation", Color.clear);
					material.shaderKeywords = new string[] { "EFFECT_BUMP", "EFFECT_HUE_VARIATION", "_SAMPLES_LOW" };
					var matPath = path.Replace(".prefab", "/" + name + "_" + nameof(Material) + ".mat");
					UnityEditor.AssetDatabase.CreateAsset(material, matPath);
					UnityEditor.AssetDatabase.Refresh();
					material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
				}
				billboard.material = material;
				var texture = gameObject.CaptureAround(size, count);
				var pngPath= path.Replace(".prefab", "/" + name + "_"+nameof(Texture) + ".png");
				pngPath.CheckDirectoryPath();
				QFileManager.SavePNG(texture, pngPath);
				UnityEditor.AssetDatabase.Refresh();
				texture= UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
				billboard.material.SetTexture("_MainTex", texture);
				Vector4[] texCoords = new Vector4[count];
				ushort[] indices = new ushort[6];
				Vector2[] vertices = new Vector2[4];
				var width = 1f / count;
				for (int i = 0; i < count; i++)
				{
					texCoords[i].Set(width * i, 0, width, 1);
				}
				indices[0] = 0;
				indices[1] = 3;
				indices[2] = 1;
				indices[3] = 2;
				indices[4] = 3;
				indices[5] = 0;
				vertices[0].Set(0, 1);
				vertices[1].Set(0, 0);
				vertices[2].Set(1, 1);
				vertices[3].Set(1, 0);
				billboard.SetImageTexCoords(texCoords);
				billboard.SetIndices(indices);
				billboard.SetVertices(vertices);
				billboard.width = 1;
				billboard.height = 1;
				billboard.bottom = -0.5f;
				UnityEditor.AssetDatabase.CreateAsset(billboard, path.Replace(".prefab", "/" +name+"_"+ nameof(BillboardAsset) + ".asset"));
				renderer.billboard = billboard;
				gameObject.ApplyPrefab();
			
			}
		}
#endif
	}
}
