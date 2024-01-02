using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QBillboardPrefab : MonoBehaviour
	{
#if UNITY_EDITOR
		[QName("单位像素尺寸")]
		public int pixel = 100;
		[QName("遮罩")]
		public LayerMask layerMask = new LayerMask { value = -1 };
		[QName("仅生成Sprite")]
		public bool onlySprite = false;
		public bool AroudMode => !onlySprite;
		[QName("单方向时拍摄方向","!"+nameof(AroudMode))]
		public Vector3 fromDirection = Vector3.back;
		[QName("方向数",nameof(AroudMode)), Range(1, 64)]
		public int count = 1;
		[QName("仅Y轴旋转", nameof(AroudMode))]
		public bool onlyY = true;
		
		[QName("烘培QBillboard资源")]
		void Bake()
		{
			string path = gameObject.IsPrefabInstance(out var prefab) ? UnityEditor.AssetDatabase.GetAssetPath(prefab) : "Assets/"+nameof(QBillboardPrefab) + ".prefab";
			var QBillboard = transform.GetChild("QBillboard", true);
			if (QBillboard.GetComponent<MeshRenderer>() != null)
			{
				QBillboard.GetComponent<MeshRenderer>().enabled = false;
			}
			if (QBillboard.GetComponent<SpriteRenderer>() != null)
			{
				QBillboard.GetComponent<SpriteRenderer>().enabled = false;
			}
			QBillboard.gameObject.SetActive(false);
			var bounds = gameObject.GetBounds();
			if (onlySprite)
			{
				count = 1;
			}
			if (bounds.size.magnitude * pixel * count > 16384)
			{
				Debug.LogError(gameObject + " 生成QBillboard图片过大 " + (bounds.size.magnitude * pixel * count));
				return;
			}
			var texture = onlySprite? gameObject.CaptureFrom(fromDirection, pixel, layerMask) : gameObject.CaptureAround(pixel, count, false, layerMask);
			var pngPath = path.Replace(".prefab", "/" + name + "_" + nameof(Texture) + ".png");
			pngPath.CheckDirectoryPath();
			if (onlySprite)
			{
				var sprite = QBillboard.GetComponent<SpriteRenderer>(true);
				sprite.sprite = texture.SaveSprite(pngPath,pixel); 
				sprite.enabled = true;
				QBillboard.transform.localScale = Vector3.one ;
				QBillboard.transform.LookAt(QBillboard.transform.position - fromDirection);
			}
			else
			{
				QFileTool.SavePNG(texture, pngPath);
				UnityEditor.AssetDatabase.Refresh();
				texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
				QBillboard.GetComponent<MeshFilter>(true).sharedMesh = QMeshTool.GetMesh(PrimitiveType.Quad);
				QBillboard.transform.SetParent(transform);
				QBillboard.name = nameof(QBillboard);
				QBillboard.transform.position = bounds.center;
				var material = new Material(Shader.Find("QShaderGraph/QBillboard"));
				QBillboard.GetComponent<MeshRenderer>(true).sharedMaterial = material;
				QBillboard.GetComponent<MeshRenderer>(true).enabled = true;
				var matPath = path.Replace(".prefab", "/" + name + "_" + nameof(Material) + ".mat");
				UnityEditor.AssetDatabase.CreateAsset(material, matPath);
				UnityEditor.AssetDatabase.Refresh();
				material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
				material.DisableKeyword("BILLBOARDMODE_NORMAL");
				material.DisableKeyword("BILLBOARDMODE_HORIZONTAL");
				material.SetTexture("_MainTex", texture);
				material.SetInt("_Count", count);
				if (count == 1)
				{
					QBillboard.transform.localScale = bounds.size.magnitude * new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1); ;
					material.EnableKeyword("BILLBOARDMODE_NORMAL");
				}
				else
				{
					QBillboard.transform.localScale = new Vector3(new Vector2(bounds.size.x, bounds.size.z).magnitude / transform.lossyScale.x, bounds.size.y / transform.lossyScale.y, 1);
					material.EnableKeyword("BILLBOARDMODE_HORIZONTAL");
				}
				if (onlyY)
				{
					material.EnableKeyword("ONLYY");
				}
				else
				{
					material.DisableKeyword("ONLYY");
				}
			}
			QBillboard.gameObject.SetActive(true);
			gameObject.ApplyPrefab();
			QDebug.Log(nameof(QBillboardPrefab) + " " + gameObject + "烘培QBillboard资源成功 " + path);
		}
#endif
	}
}
