using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace QTool
{
	public class QBillboardPrefab : MonoBehaviour
	{
#if UNITY_EDITOR
		private bool IsPrefab => gameObject.IsPrefabInstance(out var prefab) && !Application.isPlaying;
		[QName("单个图像尺寸")]
		public int size = 128;
		[QName("方向"),Range(1,64)]
		public int count = 1;
		[QName("烘培QBillboard资源", nameof(IsPrefab))]
		void Bake()
		{
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				MeshRenderer QBillboard = transform.GetChild(nameof(QBillboard))?.GetComponent<MeshRenderer>();
				if (QBillboard != null)
				{
					QBillboard.gameObject.SetActive(false);
				}
				var bounds = gameObject.GetBounds();
			
				var texture = gameObject.CaptureAround(size, count);
				var pngPath = path.Replace(".prefab", "/" + name + "_" + nameof(Texture) + ".png");
				pngPath.CheckDirectoryPath();
				QFileManager.SavePNG(texture, pngPath);
				UnityEditor.AssetDatabase.Refresh();
				if (QBillboard== null)
				{
					QBillboard = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
					QBillboard.transform.SetParent(transform);
					QBillboard.name = nameof(QBillboard);
					QBillboard.sharedMaterial = new Material(Shader.Find("QShaderGraph/QBillboard"));
					var matPath = path.Replace(".prefab", "/" + name + "_" + nameof(Material) + ".mat");
					UnityEditor.AssetDatabase.CreateAsset(QBillboard.sharedMaterial, matPath);
					UnityEditor.AssetDatabase.Refresh();
					QBillboard.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
				}
				else
				{
					QBillboard.gameObject.SetActive(true);
				}
				QBillboard.transform.localScale =Vector3.one* bounds.size.magnitude * 1.1f;
				texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
				QBillboard.sharedMaterial.SetTexture("_MainTex", texture);
				QBillboard.sharedMaterial.SetInt("_Count", count);
				QBillboard.sharedMaterial.DisableKeyword("BILLBOARDMODE_Normal");
				QBillboard.sharedMaterial.DisableKeyword("BILLBOARDMODE_HORIZONTAL");
				if (count == 1)
				{
					QBillboard.sharedMaterial.EnableKeyword("BILLBOARDMODE_Normal");
				}
				else
				{
					QBillboard.sharedMaterial.EnableKeyword("BILLBOARDMODE_HORIZONTAL");
				}
				gameObject.ApplyPrefab();
				QDebug.Log(nameof(QBillboardPrefab) + " " + gameObject + "烘培QBillboard资源成功 "+ path);
			}
			else
			{
				Debug.LogError(nameof(QBillboardPrefab) + " " + gameObject + " 非预制体实例 无法烘培QBillboard资源");
			}
		}
#endif
	}
}
