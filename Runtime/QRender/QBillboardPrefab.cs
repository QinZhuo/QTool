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
		[QName("单位像素尺寸")]
		public int pixel = 100;
		[QName("方向"), Range(1, 64)]
		public int count = 1;
		[QName("仅Y轴旋转")]
		public bool onlyY = true;
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

				var texture = gameObject.CaptureAround(pixel, count, false);
				var pngPath = path.Replace(".prefab", "/" + name + "_" + nameof(Texture) + ".png");
				pngPath.CheckDirectoryPath();
				QFileManager.SavePNG(texture, pngPath);
				UnityEditor.AssetDatabase.Refresh();
				if (QBillboard == null)
				{
					QBillboard = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
					QBillboard.transform.SetParent(transform);
					QBillboard.name = nameof(QBillboard);
					QBillboard.sharedMaterial = null;
				}
				else
				{
					QBillboard.gameObject.SetActive(true);
				}

				QBillboard.transform.position = bounds.center;
				texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
				if (QBillboard.sharedMaterial == null)
				{
					QBillboard.sharedMaterial = new Material(Shader.Find("QShaderGraph/QBillboard"));
					var matPath = path.Replace(".prefab", "/" + name + "_" + nameof(Material) + ".mat");
					UnityEditor.AssetDatabase.CreateAsset(QBillboard.sharedMaterial, matPath);
					UnityEditor.AssetDatabase.Refresh();
					QBillboard.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
				}
				QBillboard.sharedMaterial.DisableKeyword("BILLBOARDMODE_NORMAL");
				QBillboard.sharedMaterial.DisableKeyword("BILLBOARDMODE_HORIZONTAL");
				QBillboard.sharedMaterial.SetTexture("_MainTex", texture);
				QBillboard.sharedMaterial.SetInt("_Count", count);
				if (count == 1)
				{
					QBillboard.transform.localScale = bounds.size.magnitude * new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1); ;
					QBillboard.sharedMaterial.EnableKeyword("BILLBOARDMODE_NORMAL");
				}
				else
				{
					QBillboard.transform.localScale = new Vector3(new Vector2(bounds.size.x, bounds.size.z).magnitude / transform.lossyScale.x, bounds.size.y / transform.lossyScale.y, 1);
					QBillboard.sharedMaterial.EnableKeyword("BILLBOARDMODE_HORIZONTAL");
				}
				if (onlyY)
				{
					QBillboard.sharedMaterial.EnableKeyword("ONLYY");
				}
				else
				{
					QBillboard.sharedMaterial.DisableKeyword("ONLYY");
				}
				gameObject.ApplyPrefab();
				QDebug.Log(nameof(QBillboardPrefab) + " " + gameObject + "烘培QBillboard资源成功 " + path);
			}
			else
			{
				Debug.LogError(nameof(QBillboardPrefab) + " " + gameObject + " 非预制体实例 无法烘培QBillboard资源");
			}
		}
#endif
	}
}